using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GeneticTanks.Extensions;
using GeneticTanks.Game.Components.Messages;
using log4net;

namespace GeneticTanks.Game.Components
{
  sealed class MessageComponent
    : Component
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    // messages are only dispatched 30x per second
    private const float UpdateInterval = 1f / 30f;

    /// <summary>
    /// The base type for all message handlers.
    /// </summary>
    /// <param name="msg">
    /// The message to be handled.  This is guaranteed to be of the type that 
    /// was used to register the called listener.
    /// </param>
    public delegate void MessageListener(Message msg);

    #region Private Fields

    private float m_timeSinceLastUpdate = 0f;

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

    /// <summary>
    /// Create the component.
    /// </summary>
    /// <param name="parent"></param>
    /// <exception cref="ArgumentNullException">
    /// parent is null.
    /// </exception>
    public MessageComponent(Entity parent) 
      : base(parent)
    {
      NeedsUpdate = true;
      Initialized = true;
    }

    #region Component Implementation

    public override bool Initialize()
    {
      return true;
    }

    public override void Update(float deltaTime)
    {
      m_timeSinceLastUpdate += deltaTime;
      if (m_timeSinceLastUpdate < UpdateInterval)
      {
        return;
      }
      m_timeSinceLastUpdate = m_timeSinceLastUpdate % UpdateInterval;

      SwapQueues();
      DispatchMessages();
    }

    #endregion
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

      var type = typeof (T);
      if (!m_listeners.ContainsKey(type))
      {
        m_listeners[type] = listener;
      }
      else
      {
        m_listeners[type] += listener;
      }
      Log.VerboseFmt("{0} registered listener for {1}",
        Parent.FullName, type.Name);
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

      var type = typeof (T);
      if (m_listeners.ContainsKey(type))
      {
        m_listeners[type] -= listener;
        Log.VerboseFmt("{0} removed listener for {1}",
          Parent.FullName, type.Name);
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
        Log.VerboseFmt("{0} dispatching {1}", Parent.FullName, type.Name);
        listener(msg);
      }
      else
      {
        Log.VerboseFmt("{0} discarding {1}, no listeners",
          Parent.FullName, type.Name);
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

      m_queue[m_writeIndex].Add(msg);
      Log.VerboseFmt("{0} queued {1}", Parent.FullName, msg.GetType().Name);
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
      var type = typeof (T);
      var toRemove = m_queue[m_writeIndex].First(m => m.GetType() == type);
      if (toRemove == null)
      {
        return false;
      }

      m_queue[m_writeIndex].Remove(toRemove);
      Log.VerboseFmt("{0} aborted message {1}", Parent.FullName, type.Name);
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
      var type = typeof (T);
      var result = m_queue[m_writeIndex].RemoveAll(m => m.GetType() == type);
      if (result > 0)
      {
        Log.VerboseFmt("{0} aborted {1} {2} messages",
          Parent.FullName, result, type.Name);
      }
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
      var result = m_queue[m_writeIndex].Count;
      m_queue[m_writeIndex].Clear();
      if (result > 0)
      {
        Log.VerboseFmt("{0} cleared {1} messages from queue",
          Parent.FullName, result);
      }
      return result;
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
      if (m_queue[m_readIndex].Count == 0)
      {
        return;
      }

      var count = 0;
      while (m_queue[m_readIndex].Count > 0)
      {
        var msg = m_queue[m_readIndex].First();
        m_queue[m_readIndex].RemoveAt(0);
        TriggerMessage(msg);
        count++;
      }

      Log.VerboseFmt("{0} dispatched {1} messages", Parent.FullName, count);
    }

    #endregion
  }
}
