using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GeneticTanks.Extensions;
using GeneticTanks.Game.Components;
using GeneticTanks.Game.Components.Messages;
using log4net;

namespace GeneticTanks.Game
{
  /// <summary>
  /// All objects in the game are entities, and an entity is really just a 
  /// collection of components.
  /// </summary>
  sealed class Entity 
    : IDisposable
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    /// The only id number that it's not valid to use.
    /// </summary>
    public const uint InvalidId = 0;

    /// <summary>
    /// The base type for all message handlers.
    /// </summary>
    /// <param name="msg">
    /// The message to be handled.  This is guaranteed to be of the type that 
    /// was used to register the called listener.
    /// </param>
    public delegate void MessageListener(Message msg);

    #region Private Fields
    private bool m_disabled = false;

    private readonly Dictionary<Type, Component> m_components =
      new Dictionary<Type, Component>();
    // Only the components that require update calls
    private readonly List<Component> m_updateComponents = new List<Component>();

    private readonly Dictionary<Type, MessageListener> m_listeners =
      new Dictionary<Type, MessageListener>();

    private readonly List<Message>[] m_queue =
    {
      new List<Message>(),
      new List<Message>(),
    };

    private int m_readIndex = 0;
    private int m_writeIndex = 1;

    #endregion

    private List<Message> ReadQueue { get { return m_queue[m_readIndex]; } }
    private List<Message> WriteQueue { get { return m_queue[m_writeIndex]; } }

    /// <summary>
    /// Creates a new entity.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="name"></param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// id is invalid.
    /// </exception>
    public Entity(uint id, string name = "<unnamed>")
    {
      if (id == InvalidId) throw new ArgumentOutOfRangeException("id");

      Id = id;
      Name = name;
      FullName = string.Format("{0} ({1})", 
        string.IsNullOrEmpty(name) ? "<unnamed>" : name, Id);
      NeedsUpdate = false;
    }

    #region Properties

    /// <summary>
    /// The id number of the entity.  Entities should typically be referenced 
    /// by id rather than holding a reference directly to the object.
    /// </summary>
    public uint Id { get; private set; }

    public bool Enabled
    {
      get { return !m_disabled; }
      set
      {
        // enable when enabled or disable when disabled, aka nops
        if ((value && m_disabled) || (!value && !m_disabled)) return;

        // enable when disabled (reset)
        if (value && m_disabled)
        {
          m_updateComponents.Clear();
          foreach (var component in m_components.Values)
          {
            component.Enable();
            if (component.NeedsUpdate)
            {
              m_updateComponents.Add(component);
              NeedsUpdate = true;
            }
          }
        }
        // disable when enabled
        else
        {
          foreach (var component in m_components.Values)
          {
            component.Disable();
          }
        }

        m_disabled = !value;
      }
    }

    /// <summary>
    /// The optional name of the entity.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// The full name of the entity combining its Id and Name.
    /// </summary>
    public string FullName { get; private set; }

    /// <summary>
    /// Signifies that the entity has components that require logic updates.  
    /// Should only be considered valid after all components are added to the 
    /// entity.
    /// </summary>
    public bool NeedsUpdate { get; private set; }

    /// <summary>
    /// The object's transform, for easy access since most (all?) entities will 
    /// have a transform.
    /// </summary>
    public TransformComponent Transform { get; private set; }

    #endregion

    /// <summary>
    /// Initializes all the components in the entity.
    /// </summary>
    /// <returns>
    /// Initialization success or failure.  After failed initialization an 
    /// entity should be considered unusable and immediately disposed.
    /// </returns>
    public bool Initialize()
    {
      var transforms = GetComponentsByBase<TransformComponent>();
      switch (transforms.Count)
      {
        case 1:
          Transform = transforms.First();
          break;

        case 0:
          Log.WarnFmt("{0} initialized with no TransformComponent...",
            FullName);
          break;

        default:
          Log.ErrorFmt("{0} initialized with multiple TransformComponents",
            FullName);
          return false;
      }

      foreach (var component in m_components.Values)
      {
        if (!component.Initialize())
        {
          Log.ErrorFmt("Failed to initialize {0} in {1}",
            component.GetType().Name, FullName);
          return false;
        }
      }

      Log.VerboseFmt("{0} initialized {1} components", FullName, 
        m_components.Count);
      return true;
    }

    /// <summary>
    /// Performs a logic update on the entity.
    /// </summary>
    /// <param name="deltaTime">
    /// The seconds elapsed since the last update.
    /// </param>
    public void Update(float deltaTime)
    {
      foreach (var component in m_updateComponents)
      {
        component.Update(deltaTime);
      }

      if (WriteQueue.Count == 0)
      {
        return;
      }

      SwapQueues();
      DispatchMessages();
    }

    #region Listener Access Methods

    /// <summary>
    /// Add a listener for message type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="listener"></param>
    /// <exception cref="ArgumentNullException">
    /// listener is null.
    /// </exception>
    public void AddListener<T>(MessageListener listener)
      where T : Message
    {
      if (listener == null)
      {
        throw new ArgumentNullException("listener");
      }

      var type = typeof(T);
      if (!m_listeners.ContainsKey(type))
      {
        m_listeners[type] = listener;
      }
      else
      {
        m_listeners[type] += listener;
      }
      Log.VerboseFmt("{0} registered listener for {1}",
        Name, type.Name);
    }

    /// <summary>
    /// Removes a listener for message type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="listener"></param>
    /// <exception cref="ArgumentNullException">
    /// listener is null.
    /// </exception>
    public void RemoveListener<T>(MessageListener listener)
      where T : Message
    {
      if (listener == null)
      {
        throw new ArgumentNullException("listener");
      }

      var type = typeof(T);
      if (m_listeners.ContainsKey(type))
      {
        m_listeners[type] -= listener;
        Log.VerboseFmt("{0} removed listener for {1}",
          Name, type.Name);
      }
    }

    #endregion
    #region Message Access Methods

    /// <summary>
    /// Immediately dispatches a message, ignoring the queue.
    /// </summary>
    /// <param name="msg"></param>
    /// <exception cref="ArgumentNullException">
    /// msg is null.
    /// </exception>
    public void TriggerMessage(Message msg)
    {
      if (msg == null)
      {
        throw new ArgumentNullException("msg");
      }

      var type = msg.GetType();
      MessageListener listener;
      if (m_listeners.TryGetValue(type, out listener) && listener != null)
      {
        Log.VerboseFmt("{0} dispatching {1}", Name, type.Name);
        listener(msg);
      }
      else
      {
        Log.VerboseFmt("{0} discarding {1}, no listeners",
          Name, type.Name);
      }
    }

    /// <summary>
    /// Adds a message to the queue.
    /// </summary>
    /// <param name="msg"></param>
    /// <exception cref="ArgumentNullException">
    /// msg is null.
    /// </exception>
    public void QueueMessage(Message msg)
    {
      if (msg == null)
      {
        throw new ArgumentNullException("msg");
      }

      WriteQueue.Add(msg);
      Log.VerboseFmt("{0} queued {1}", Name, msg.GetType().Name);
    }

    /// <summary>
    /// Removes the oldest message of type T.  Messages cannot be removed after 
    /// the queue has begun to process.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns>
    /// True if an event was aborted.
    /// </returns>
    public bool RemoveFirstMessage<T>()
      where T : Message
    {
      var type = typeof(T);
      var toRemove = WriteQueue.First(m => m.GetType() == type);
      if (toRemove == null)
      {
        return false;
      }

      WriteQueue.Remove(toRemove);
      Log.VerboseFmt("{0} aborted message {1}", Name, type.Name);
      return true;
    }

    /// <summary>
    /// Removes all pending messages of type T.  Messages cannot be removed 
    /// after the queue has begun to process.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns>
    /// The number of messages removed.
    /// </returns>
    public int RemoveMessages<T>()
      where T : Message
    {
      var type = typeof(T);
      var result = WriteQueue.RemoveAll(m => m.GetType() == type);
      Log.VerboseFmtIf(result > 0,
        "{0} aborted {1} {2} messages", Name, result, type.Name);
      return result;
    }

    /// <summary>
    /// Removes all messages from the queue.  Messages cannot be removed after 
    /// the queue has begun to process.
    /// </summary>
    /// <returns>
    /// The number of messages removed.
    /// </returns>
    public int RemoveAllMessages()
    {
      var result = WriteQueue.Count;
      WriteQueue.Clear();
      Log.VerboseFmtIf(result > 0,
        "{0} cleared {1} messages from queue", Name, result);
      return result;
    }

    #endregion
    #region Component Access Methods

    /// <summary>
    /// Checks if the entity contains a particular component.
    /// </summary>
    /// <typeparam name="T">
    /// The component type to query.
    /// </typeparam>
    /// <returns>
    /// True if the entity has that component.
    /// </returns>
    public bool HasComponent<T>() 
      where T : Component
    {
      var type = typeof (T);
      return m_components.ContainsKey(type);
    }

    /// <summary>
    /// Adds a component to the entity.
    /// </summary>
    /// <param name="component"></param>
    /// <exception cref="ArgumentException">
    /// Attempted to add a duplicate component.
    /// </exception>
    public void AddComponent(Component component)
    {
      if (component == null)
      {
        return;
      }
      if (m_components.ContainsKey(component.GetType()))
      {
        throw new ArgumentException(
          "Entity already contains component type " + component.GetType(),
          "component"
          );
      }

      if (component.NeedsUpdate)
      {
        NeedsUpdate = true;
        m_updateComponents.Add(component);
      }

      m_components.Add(component.GetType(), component);
      Log.VerboseFmt("{0} added component {1}", FullName, 
        component.GetType().Name);
    }

    /// <summary>
    /// Attempts to retrieve a component from the entity.
    /// </summary>
    /// <typeparam name="T">
    /// The component type to query.
    /// </typeparam>
    /// <param name="component"></param>
    /// <returns>
    /// True if T was found.
    /// </returns>
    public bool TryGetComponent<T>(out T component) 
      where T : Component
    {
      component = GetComponent<T>();
      return component != null;
    }

    /// <summary>
    /// Gets a component from the entity.
    /// </summary>
    /// <typeparam name="T">
    /// The component type to query.
    /// </typeparam>
    /// <returns>
    /// The component if found, otherwise null.
    /// </returns>
    public T GetComponent<T>() 
      where T : Component
    {
      var type = typeof (T);
      Component c;
      if (m_components.TryGetValue(type, out c))
      {
        return (T) c;
      }

      Log.DebugFmt("GetComponent<T>: {0} does not have requested component {1}",
        FullName, type.Name);
      return null;
    }

    /// <summary>
    /// Searches the entity for all components that can be cast to type T.
    /// </summary>
    /// <typeparam name="T">
    /// The component base type to query.
    /// </typeparam>
    /// <returns>
    /// The matching components.
    /// </returns>
    public List<T> GetComponentsByBase<T>()
      where T : Component
    {
      return m_components.Values.OfType<T>().ToList();
    }

    #endregion
    #region Private Methods

    // swap the read and write queues
    private void SwapQueues()
    {
      m_readIndex = (m_readIndex + 1) & 1;
      m_writeIndex = (m_writeIndex + 1) & 1;
    }

    // dispatches all pending messages in the read queue
    private void DispatchMessages()
    {
      var count = 0;
      while (ReadQueue.Count > 0)
      {
        var msg = ReadQueue.First();
        ReadQueue.RemoveAt(0);
        TriggerMessage(msg);
        count++;
      }

      Log.VerboseFmtIf(count > 0,
        "{0} dispatched {1} messages", Name, count);
    }

    #endregion
    #region IDisposable Implementation

    private bool m_disposed = false;

    /// <summary>
    /// Clean up the resources for this entity.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Cleanup implementation.
    /// </summary>
    /// <param name="disposing">
    /// When true, dispose managed resources.
    /// </param>
    private void Dispose(bool disposing)
    {
      if (m_disposed)
      {
        return;
      }

      Log.VerboseFmt("{0} disposing", FullName);

      if (disposing)
      {
        foreach (var component in m_components.Values)
        {
          component.Dispose();
        }
      }

      Transform = null;
      m_components.Clear();
      m_updateComponents.Clear();

      m_disposed = true;
    }

    #endregion

    ~Entity()
    {
      Dispose(false);
    }
  }
}
