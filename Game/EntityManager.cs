using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using GeneticTanks.Game.Events;
using log4net;

namespace GeneticTanks.Game
{
  /// <summary>
  /// Owns and manages all entities in the game.
  /// </summary>
  sealed class EntityManager
    : IDisposable
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    #region Private Fields
    // the game event manager
    private readonly EventManager m_eventManager;
    // the main entity map
    private readonly Dictionary<uint, Entity> m_entities = 
      new Dictionary<uint, Entity>();
    // entities that require per frame updates
    private readonly List<Entity> m_updateEntities = new List<Entity>(50);
    // entities that will be removed in the next frame
    private readonly Queue<Entity> m_pendingRemovalQueue = new Queue<Entity>();
    #endregion

    /// <summary>
    /// Create the entity manager.
    /// </summary>
    /// <param name="em"></param>
    /// <exception cref="ArgumentNullException">em is null.</exception>
    public EntityManager(EventManager em)
    {
      if (em == null)
      {
        throw new ArgumentNullException("em");
      }

      m_eventManager = em;
      m_eventManager.AddListener<RequestEntityRemoval>(HandleRemovalRequest);
    }

    /// <summary>
    /// Perform an update on all entities.
    /// </summary>
    /// <param name="deltaTime">
    /// The time in seconds since the Update was last called.
    /// </param>
    public void Update(float deltaTime)
    {
      while (m_pendingRemovalQueue.Count > 0)
      {
        var e = m_pendingRemovalQueue.Dequeue();
        RemoveEntity(e);
      }

      foreach (var entity in m_updateEntities)
      {
        entity.Update(deltaTime);
      }
    }

    /// <summary>
    /// Adds an entity to the manager.
    /// </summary>
    /// <param name="e"></param>
    /// <exception cref="ArgumentNullException">e is null.</exception>
    public void AddEntity(Entity e)
    {
      if (e == null)
      {
        throw new ArgumentNullException("e");
      }
      Debug.Assert(!m_entities.ContainsKey(e.Id));

      m_entities[e.Id] = e;
      if (e.NeedsUpdate)
      {
        m_updateEntities.Add(e);
      }
      m_eventManager.QueueEvent(new EntityAdded(e.Id));
      Log.DebugFormat("Added entity {0}", e.Id);
    }

    /// <summary>
    /// Queues an entity to be removed during the next frame.
    /// </summary>
    /// <param name="id"></param>
    public void RemoveEntity(uint id)
    {
      var e = GetEntity(id);
      if (e != null)
      {
        m_pendingRemovalQueue.Enqueue(e);
        m_eventManager.QueueEvent(new EntityRemoved(e.Id));
        Log.DebugFormat("Entity {0} queued for removal", e.Id);
      }
    }

    /// <summary>
    /// Retrieve an entity.
    /// </summary>
    /// <param name="id"></param>
    /// <returns>The entity, or null if it doesn't exist.</returns>
    public Entity GetEntity(uint id)
    {
      Entity e;
      m_entities.TryGetValue(id, out e);
      return e;
    }

    #region Private Methods

    /// <summary>
    /// Handles the event request to remove an entity.
    /// </summary>
    /// <param name="evt"></param>
    private void HandleRemovalRequest(Event evt)
    {
      Debug.Assert(evt != null);
      var e = evt as RequestEntityRemoval;
      Debug.Assert(e != null);

      var entity = GetEntity(e.Id);
      if (entity != null)
      {
        m_pendingRemovalQueue.Enqueue(entity);
        Log.DebugFormat("Entity {0} queued for removal", e.Id);
        // event must be manually triggered so the entity can be removed
        // in the next frame
        m_eventManager.TriggerEvent(new EntityRemoved(e.Id));
      }
    }

    /// <summary>
    /// Finalizes the removal of an entity and disposes of it.
    /// </summary>
    /// <param name="e"></param>
    private void RemoveEntity(Entity e)
    {
      Debug.Assert(e != null);

      var id = e.Id;
      m_updateEntities.Remove(e);
      m_entities.Remove(e.Id);
      e.Dispose();
      Log.DebugFormat("Removed entity {0}", id);
    }

    #endregion

    #region IDisposable Implementation

    private bool m_disposed = false;

    /// <summary>
    /// Clean up the manager and all active entities.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
      if (m_disposed)
      {
        return;
      }

      if (disposing)
      {
        foreach (var entity in m_entities.Values)
        {
          entity.Dispose();
        }
      }

      m_entities.Clear();
      m_updateEntities.Clear();
      m_eventManager.RemoveListener<RequestEntityRemoval>(HandleRemovalRequest);

      m_disposed = true;
    }

    ~EntityManager()
    {
      Dispose(false);
    }

    #endregion
  }
}
