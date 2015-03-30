using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using GeneticTanks.Game.Components;
using GeneticTanks.Game.Events;
using log4net;
using SFML.Graphics;
using Event = GeneticTanks.Game.Events.Event;

namespace GeneticTanks.Game
{
  /// <summary>
  /// Manages the rendering of all graphical components.
  /// </summary>
  sealed class RenderManager
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    /// The framerate that the manager attempts to maintain.
    /// </summary>
    public const int TargetFrameRate = 60;

    // Update time to achieve the framerate
    private const float UpdateInterval = 1f / TargetFrameRate;

    #region Private Fields
    private readonly EventManager m_eventManager;
    private bool m_dirtyState = false;
    private float m_timeSinceLastRender = 0;
    private readonly List<RenderComponent> m_renderComponents = 
      new List<RenderComponent>();
    #endregion

    /// <summary>
    /// Create the render manager.
    /// </summary>
    /// <param name="em"></param>
    /// <exception cref="ArgumentNullException">
    /// em is null
    /// </exception>
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
    /// <returns>
    /// True if rendering occured, and Display() needs to be called on target.
    /// </returns>
    public bool Update(float deltaTime, RenderTarget target)
    {
      m_timeSinceLastRender += deltaTime;
      if (m_timeSinceLastRender < UpdateInterval)
      {
        return false;
      }

      m_timeSinceLastRender -= UpdateInterval;
      Draw(target);
      return true;
    }

    /// <summary>
    /// Draws everything to the target, ignoring the framerate timer.
    /// </summary>
    /// <param name="target"></param>
    public void Draw(RenderTarget target)
    {
      if (target == null)
      {
        return;
      }

      if (m_dirtyState)
      {
        m_renderComponents.Sort();
        m_dirtyState = false;
      }

      target.Clear(Color.White);

      foreach (var component in m_renderComponents)
      {
        component.Draw(target);
      }
    }
    
    #region Callbacks
    
    // Grabs the render components from a new entity.
    private void HandleEntityAdded(Event e)
    {
      var evt = (EntityAddedEvent) e;

      var components = evt.Entity.GetComponentsByBase<RenderComponent>();
      if (components.Count > 0)
      {
        m_dirtyState = true;
        m_renderComponents.AddRange(components);
        Log.DebugFormat("Added {0} components from entity {1}",
          components.Count, evt.Entity.Id);
      }
    }

    // Removes an entity from the render list.
    private void HandleEntityRemoved(Event e)
    {
      var evt = (EntityRemovedEvent) e;

      var count = m_renderComponents.RemoveAll(
        component => component.Parent.Id == evt.Entity.Id);
      if (count > 0)
      {
        m_dirtyState = true;
        Log.DebugFormat("Removed {0} components from entity {1}",
          count, evt.Entity.Id);
      }
    }
    
    #endregion

    ~RenderManager()
    {
      m_eventManager.RemoveListener<EntityAddedEvent>(HandleEntityAdded);
      m_eventManager.RemoveListener<EntityRemovedEvent>(HandleEntityRemoved);
    }
  }
}
