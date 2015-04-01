using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using GeneticTanks.Game.Events;
using log4net;

namespace GeneticTanks.Game
{
  /// <summary>
  /// Manages the queueing and dispatching of events in the game.
  /// </summary>
  sealed class EventManager
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);
    
    /// <summary>
    /// The base delegate type for all event listeners.
    /// </summary>
    /// <param name="evt">
    /// The arguments for the event.  This is always guaranteed to be of the 
    /// type that was used when you registered the listener using AddListener.
    /// </param>
    public delegate void EventListener(Event evt);

    #region Private Fields
    private readonly Stopwatch m_eventTimer = new Stopwatch();
    private readonly Dictionary<Type, EventListener> m_listeners = 
      new Dictionary<Type, EventListener>();
    // each list acts as a queue for unprocessed events
    private readonly List<Event>[] m_queue = {
      new List<Event>(25),
      new List<Event>(25)
    };
    private int m_readIndex = 0;
    private int m_writeIndex = 1;
    #endregion

    /// <summary>
    /// Updates the event queue, processing any pending events.
    /// </summary>
    /// <param name="maxEventTime">
    /// The max time in seconds that can spent on event processing.  If 
    /// processing takes longer than this time, it will be aborted.
    /// </param>
    public void Update(float maxEventTime)
    {
      // queues are swapped so that any new events added in response to an 
      // event firing are processed in the next frame
      SwapQueues();
      DispatchEvents(maxEventTime);
    }

    #region Listener Access Methods

    /// <summary>
    /// Adds a listener for event type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="listener"></param>
    /// <exception cref="ArgumentNullException">
    /// listener is null.
    /// </exception>
    public void AddListener<T>(EventListener listener) 
      where T : Event
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
      Log.DebugFormat("Added listener for {0}", type.Name);
    }

    /// <summary>
    /// Removes a listener for event type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="listener"></param>
    /// <exception cref="ArgumentNullException">
    /// listener is null.
    /// </exception>
    public void RemoveListener<T>(EventListener listener)
      where T : Event
    {
      if (listener == null)
      {
        throw new ArgumentNullException("listener");
      }

      var type = typeof (T);
      if (m_listeners.ContainsKey(type))
      {
        m_listeners[type] -= listener;
        Log.DebugFormat("Removed listener for {0}", type.Name);
      }
    }

    #endregion
    #region Event Access Methods
    
    /// <summary>
    /// Immediately triggers an event, ignoring the queue.
    /// </summary>
    /// <param name="evt"></param>
    /// <exception cref="ArgumentNullException">
    /// evt is null.
    /// </exception>
    public void TriggerEvent(Event evt)
    {
      if (evt == null)
      {
        throw new ArgumentNullException("evt");
      }

      var type = evt.GetType();
      EventListener listener;
      if (m_listeners.TryGetValue(type, out listener) && listener != null)
      {
        Log.DebugFormat("Dispatching {0}", type.Name);
        listener(evt);
      }
      else
      {
        Log.DebugFormat("Discarding {0}, no listeners", type.Name);
      }
    }

    /// <summary>
    /// Adds an event to the queue.
    /// </summary>
    /// <param name="evt"></param>
    /// <exception cref="ArgumentNullException">
    /// evt is null.
    /// </exception>
    public void QueueEvent(Event evt)
    {
      if (evt == null)
      {
        throw new ArgumentNullException("evt");
      }

      m_queue[m_writeIndex].Add(evt);
      Log.DebugFormat("Queued {0}", evt.GetType().Name);
    }

    /// <summary>
    /// Removes the oldest event of type T.  Events cannot be aborted after 
    /// Update() has been called and event processing begun.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns>
    /// True if an event was aborted.
    /// </returns>
    public bool AbortFirstEvent<T>() 
      where T : Event
    {
      var type = typeof (T);
      var toRemove = m_queue[m_writeIndex].First(e => e.GetType() == type);
      if (toRemove == null)
      {
        return false;
      }

      m_queue[m_writeIndex].Remove(toRemove);
      Log.DebugFormat("Aborted event {0}", type.Name);
      return true;
    }

    /// <summary>
    /// Removes all pending events of type T.  Events cannot be aborted after 
    /// Update() has been called and event processing begun.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns>
    /// The number of events removed.
    /// </returns>
    public int AbortEvents<T>() 
      where T : Event
    {
      var type = typeof (T);
      var result = m_queue[m_writeIndex].RemoveAll(e => e.GetType() == type);
      if (result > 0)
      {
        Log.DebugFormat("Aborted {0} events of type {1}", result, type.Name);
      }
      return result;
    }

    /// <summary>
    /// Clears all events from the queue.  Events cannot be aborted after 
    /// Update() has been called and event processing begun.
    /// </summary>
    /// <returns>
    /// The number of events cleared.
    /// </returns>
    public int AbortAllEvents()
    {
      var result = m_queue[m_writeIndex].Count;
      m_queue[m_writeIndex].Clear();
      if (result > 0)
      {
        Log.DebugFormat("Cleared {0} events from queue", result);
      }
      return result;
    }

    #endregion
    #region Private Methods

    // swap the read and write queues
    private void SwapQueues()
    {
      // both queue indices are toggled between 0 and 1
      m_readIndex = (m_readIndex + 1) & 1;
      m_writeIndex = (m_writeIndex + 1) & 1;
    }

    // Attempts to dispatch all messages from the read queue.  Any events that 
    // can't be processed are moved to the head of the write queue.
    private void DispatchEvents(float maxEventTime)
    {
      if (m_queue[m_readIndex].Count == 0)
      {
        return;
      }

      var count = 0;
      m_eventTimer.Restart();
      while (m_queue[m_readIndex].Count > 0)
      {
        if (m_eventTimer.Elapsed.TotalSeconds >= maxEventTime)
        {
          Log.DebugFormat(
            "Queue processing aborted with {0} events in the queue",
            m_queue[m_readIndex].Count);
          
          m_queue[m_writeIndex].InsertRange(0, m_queue[m_readIndex]);
          m_queue[m_readIndex].Clear();
          break;
        }

        var evt = m_queue[m_readIndex].First();
        m_queue[m_readIndex].RemoveAt(0);
        TriggerEvent(evt);
        count++;
      }

      Log.DebugFormat("Processed {0} events in {1:F4} s", count,
        m_eventTimer.Elapsed.TotalSeconds);
    }

    #endregion
  }
}
