﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using GeneticTanks.Game.Components;
using GeneticTanks.Game.Events;
using log4net;
using SFML.Graphics;

namespace GeneticTanks.Game
{
  /// <summary>
  /// Manages the rendering of all graphical components.
  /// </summary>
  sealed class RenderManager
    : IDisposable
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    // Update time to achieve 60 fps
    private const float UpdateInterval = 1f / 60f;

    #region Private Fields
    // event manager dependency
    private readonly EventManager m_eventManager;
    // signals that something in the render state has changed and needs updating
    private bool m_dirtyState = false;
    // accumulates time since the last render
    private float m_timeSinceLastRender = 0;
    // holds all components that require rendering
    private readonly List<RenderComponent> m_renderComponents = 
      new List<RenderComponent>();
    #endregion

    /// <summary>
    /// Create the render manager.
    /// </summary>
    /// <param name="em"></param>
    public RenderManager(EventManager em)
    {
      if (em == null)
      {
        throw new ArgumentNullException("em");
      }
      
      m_eventManager = em;
      m_eventManager.AddListener<EntityAddedEvent>(HandleEntityAdded);
      m_eventManager.AddListener<EntityRemovedEvent>(HandleEntityRemoved);
    }

    /// <summary>
    /// Updates the renderer and draws to the target.
    /// </summary>
    /// <param name="deltaTime">
    /// The time since Update was last called, in seconds.
    /// </param>
    /// <param name="target"></param>
    public void Update(float deltaTime, RenderTarget target)
    {
      Debug.Assert(target != null);

      m_timeSinceLastRender += deltaTime;
      if (m_timeSinceLastRender < UpdateInterval)
      {
        return;
      }
      m_timeSinceLastRender = 0;

      if (m_dirtyState)
      {
        m_renderComponents.Sort();
        m_dirtyState = false;
      }

      foreach (var component in m_renderComponents)
      {
        component.Draw(target);
      }
    }
    
    #region Callbacks
    
    /// <summary>
    /// Grabs the render components from a new entity when it is added.
    /// </summary>
    /// <param name="evt"></param>
    private void HandleEntityAdded(Event evt)
    {
      Debug.Assert(evt != null);
      var e = evt as EntityAddedEvent;
      Debug.Assert(e != null);

      var components = e.Entity.GetComponentsByBase<RenderComponent>();
      if (components.Count > 0)
      {
        m_dirtyState = true;
        m_renderComponents.AddRange(components);
        Log.DebugFormat("Added {0} components from entity {1}",
          components.Count, e.Entity.Id);
      }
    }

    /// <summary>
    /// Removes an entities render components when it is removed.
    /// </summary>
    /// <param name="evt"></param>
    private void HandleEntityRemoved(Event evt)
    {
      Debug.Assert(evt != null);
      var e = evt as EntityRemovedEvent;
      Debug.Assert(e != null);

      var count = m_renderComponents.RemoveAll(
        component => component.Parent.Id == e.Entity.Id);
      if (count > 0)
      {
        m_dirtyState = true;
        Log.DebugFormat("Removed {0} components from entity {1}",
          count, e.Entity.Id);
      }
    }
    
    #endregion
    
    #region IDisposable Implementation

    private bool m_disposed = false;

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
        
      }

      m_eventManager.RemoveListener<EntityAddedEvent>(HandleEntityAdded);
      m_eventManager.RemoveListener<EntityRemovedEvent>(HandleEntityRemoved);

      m_disposed = true;
    }

    ~RenderManager()
    {
      Dispose(false);
    }

    #endregion
  }
}
