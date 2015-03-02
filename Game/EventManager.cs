using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;

namespace GeneticTanks.Game
{
  sealed class EventManager
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    public delegate void EventListener(EventArgs args);

    private readonly Dictionary<Type, EventListener> m_eventListeners = 
      new Dictionary<Type, EventListener>();
    private readonly List<EventArgs> m_pendingEvents = new List<EventArgs>();

    /// <summary>
    /// Adds a listener for event type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="listener"></param>
    /// <exception cref="ArgumentNullException">
    /// listener is null.
    /// </exception>
    public void AddListener<T>(EventListener listener) where T : EventArgs
    {
      if (listener == null)
      {
        throw new ArgumentNullException("listener");
      }

      var type = typeof (T);
      m_eventListeners[type] += listener;
      Log.DebugFormat("Added listener {0} for type {1}", listener, type);
    }

    /// <summary>
    /// Removes a listener for event type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="listener"></param>
    /// <exception cref="ArgumentNullException">
    /// listener is null.
    /// </exception>
    public void RemoveListener<T>(EventListener listener) where T : EventArgs
    {
      if (listener == null)
      {
        throw new ArgumentNullException("listener");
      }

      var type = typeof (T);
      m_eventListeners[type] -= listener;
      Log.DebugFormat("Removed listener {0} for type {1}", listener, type);
    }

    /// <summary>
    /// Immediately triggers an event, ignoring the queue.
    /// </summary>
    /// <param name="evt"></param>
    /// <exception cref="ArgumentNullException">evt is null</exception>
    public void TriggerEvent(EventArgs evt)
    {
      if (evt == null)
      {
        throw new ArgumentNullException("evt");
      }

      var type = evt.GetType();
      var listener = m_eventListeners[type];
      if (listener != null)
      {
        Log.DebugFormat("Firing event {0}", type);
        listener(evt);
      }
    }

    /// <summary>
    /// Adds an event to the queue.
    /// </summary>
    /// <param name="evt"></param>
    /// <exception cref="ArgumentNullException">evt is null</exception>
    public void QueueEvent(EventArgs evt)
    {
      if (evt == null)
      {
        throw new ArgumentNullException("evt");
      }
      m_pendingEvents.Add(evt);
    }

    /// <summary>
    /// Aborts the oldest event of type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns>True if an event was aborted.</returns>
    public bool AbortFirstEvent<T>() 
      where T : EventArgs
    {
      var type = typeof (T);
      var toRemove = m_pendingEvents.First(evt => evt.GetType() == type);
      if (toRemove != null)
      {
        m_pendingEvents.Remove(toRemove);
        Log.DebugFormat("Aborted event of type {0}", type);
      }
    }

    /// <summary>
    /// Removes all pending events of type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns>The number of events removed.</returns>
    public int AbortEvents<T>() 
      where T : EventArgs
    {
      var type = typeof (T);
      var result = m_pendingEvents.RemoveAll(evt => evt.GetType() == type);
      Log.DebugFormat("Aborted {0} events of type {1}", result, type);
      return result;
    }

    /// <summary>
    /// Clears all events from the queue.
    /// </summary>
    /// <returns>The number of events cleared.</returns>
    public int AbortAllEvents()
    {
      var result = m_pendingEvents.Count();
      m_pendingEvents.Clear();
      Log.DebugFormat("Cleared {0} events from queue", result);
      return result;
    }
  }
}
