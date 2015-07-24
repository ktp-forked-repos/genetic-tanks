using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    
    /// <summary>
    /// The base type for all message handlers.
    /// </summary>
    /// <param name="msg">
    /// The message to be handled.  This is guaranteed to be of the type that 
    /// was used to register the called listener.
    /// </param>
    public delegate void MessageListener(Message msg);

    #region Private Fields
    
    private readonly Dictionary<Type, MessageListener> m_listeners = 
      new Dictionary<Type, MessageListener>();

    private readonly List<Message>[] m_queue =
    {
      new List<Message>(),
      new List<Message>(),
    };

    private int m_readIndex = 0;
    private int m_writeIndex = 1;
    private bool m_enabled = false;
    #endregion

    private List<Message> ReadQueue { get { return m_queue[m_readIndex]; } }
    private List<Message> WriteQueue { get { return m_queue[m_writeIndex]; } }

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

    /// <summary>
    /// Number of message waiting to dispatch.
    /// </summary>
    public int PendingMessages { get { return WriteQueue.Count; } }

    #region Component Implementation

    public override bool Initialize()
    {
      m_enabled = true;
      return true;
    }

    public override void Enable()
    {
      m_enabled = true;
    }

    public override void Disable()
    {
      RemoveAllMessages();
      m_enabled = false;
    }

    public override void Deactivate()
    {
    }

    public override void Update(float deltaTime)
    {
      if (WriteQueue.Count == 0)
      {
        return;
      }

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
        // ReSharper disable once DelegateSubtraction
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
      
      Debug.Assert(m_enabled);

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

      Debug.Assert(m_enabled);

      WriteQueue.Add(msg);
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
      Debug.Assert(m_enabled);

      var type = typeof (T);
      var toRemove = WriteQueue.First(m => m.GetType() == type);
      if (toRemove == null)
      {
        return false;
      }

      WriteQueue.Remove(toRemove);
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
      Debug.Assert(m_enabled);

      var type = typeof (T);
      var result = WriteQueue.RemoveAll(m => m.GetType() == type);
      Log.VerboseFmtIf(result > 0,
        "{0} aborted {1} {2} messages", Parent.FullName, result, type.Name);
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
      Debug.Assert(m_enabled);

      var result = WriteQueue.Count;
      WriteQueue.Clear();
      Log.VerboseFmtIf(result > 0,
        "{0} cleared {1} messages from queue",Parent.FullName, result);
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
      var count = 0;
      while (ReadQueue.Count > 0)
      {
        var msg = ReadQueue.First();
        ReadQueue.RemoveAt(0);
        TriggerMessage(msg);
        count++;
      }

      Log.VerboseFmtIf(count > 0, 
        "{0} dispatched {1} messages", Parent.FullName, count);
    }

    #endregion
  }
}
