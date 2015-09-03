using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using GeneticTanks.Extensions;
using GeneticTanks.Game.Components;
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

    #region Private Fields
    private readonly Dictionary<Type, Component> m_components =
      new Dictionary<Type, Component>();
    // Only the components that require update calls
    private readonly List<Component> m_updateComponents = new List<Component>();
    #endregion

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
      if (id == InvalidId)
      {
        throw new ArgumentOutOfRangeException("id");
      }

      Id = id;
      Name = name;
      FullName = string.Format("{0} ({1})", 
        string.IsNullOrEmpty(name) ? "<unnamed>" : name, Id);
      NeedsUpdate = false;
    }

    /// <summary>
    /// The id number of the entity.  Entities should typically be referenced 
    /// by id rather than holding a reference directly to the object.
    /// </summary>
    public uint Id { get; private set; }

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
    }

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
        // HACK: Components can't remove listeners from MessageComponent after 
        // it has been disposed
        var messenger = GetComponent<MessageComponent>();
        var nonMessenger = m_components.Values
          .Where(c => c.GetType() != typeof (MessageComponent));
        foreach (var component in nonMessenger)
        {
          component.Dispose();
        }

        if (messenger != null)
        {
          messenger.Dispose();
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
