using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using GeneticTanks.Game.Components;
using GeneticTanks.Game.Events;
using log4net;
using SFML.Graphics;
using SFML.Window;
using Event = GeneticTanks.Game.Events.Event;

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
    // Closest zoom is 10m wide
    private const float MinViewWidth = 10f;

    #region Private Fields

    // event manager dependency
    private readonly EventManager m_eventManager;
    
    // the sfml window
    private readonly RenderWindow m_renderWindow;

    // view control variables
    private bool m_draggingView = false;
    private Vector2i m_mousePos;
    private float m_viewWidth = 100;
    private readonly View m_view = new View
    {
      Center = new Vector2f(0, 0),
      Viewport = new FloatRect(0, 0, 1, 1)
    };

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
    /// <param name="windowHandle"></param>
    public RenderManager(EventManager em, IntPtr windowHandle)
    {
      if (em == null)
      {
        throw new ArgumentNullException("em");
      }
      
      m_eventManager = em;
      m_eventManager.AddListener<EntityAddedEvent>(HandleEntityAdded);
      m_eventManager.AddListener<EntityRemovedEvent>(HandleEntityRemoved);

      m_renderWindow = new RenderWindow(windowHandle,
        new ContextSettings {AntialiasingLevel = 8});
      UpdateViewSize();

      m_renderWindow.Resized += (sender, args) => UpdateViewSize();
      m_renderWindow.MouseWheelMoved += HandleMouseWheelMoved;
      m_renderWindow.MouseButtonPressed += HandleMouseButtonPressed;
      m_renderWindow.MouseButtonReleased += HandleMouseButtonReleased;
      m_renderWindow.MouseMoved += HandleMouseMoved;
    }
    
    /// <summary>
    /// Updates the renderer and draws to the target.
    /// </summary>
    /// <param name="deltaTime">
    /// The time since Update was last called, in seconds.
    /// </param>
    public void Update(float deltaTime)
    {
      m_renderWindow.DispatchEvents();

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

      m_renderWindow.SetView(m_view);
      m_renderWindow.Clear(Color.White);

      foreach (var component in m_renderComponents)
      {
        component.Draw(m_renderWindow);
      }
      m_renderWindow.Display();
    }

    private void UpdateViewSize()
    {
      var size = m_renderWindow.Size;
      var ratio = (float) size.Y / size.X;
      m_view.Size = new Vector2f(m_viewWidth, m_viewWidth * ratio);
    }
    
    #region Callbacks
    
    // Grabs the render components from a new entity.
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

    // Removes an entity from the render list.
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

    // Zooms the view in or out 10% for each mouse wheel click.
    private void HandleMouseWheelMoved(object sender, 
      MouseWheelEventArgs mouseWheelEventArgs)
    {
      // delta is +1/-1
      // but invert it to make forward scrolling zoom in
      m_viewWidth += -mouseWheelEventArgs.Delta * (m_viewWidth / 10f);
      m_viewWidth = Math.Max(m_viewWidth, MinViewWidth);
      UpdateViewSize();
    }

    // Begins dragging the view.
    private void HandleMouseButtonPressed(object sender,
      MouseButtonEventArgs mouseButtonEventArgs)
    {
      if (mouseButtonEventArgs.Button != Mouse.Button.Left)
      {
        return;
      }

      m_draggingView = true;
      m_mousePos = new Vector2i(mouseButtonEventArgs.X, mouseButtonEventArgs.Y);
    }

    // Ends dragging the view.
    private void HandleMouseButtonReleased(object sender,
      MouseButtonEventArgs mouseButtonEventArgs)
    {
      if (mouseButtonEventArgs.Button != Mouse.Button.Left)
      {
        return;
      }

      m_draggingView = false;
    }

    // Moves the view, if dragging.
    private void HandleMouseMoved(object sender,
      MouseMoveEventArgs mouseMoveEventArgs)
    {
      if (!m_draggingView)
      {
        return;
      }

      var oldPos = m_mousePos;
      m_mousePos = new Vector2i(mouseMoveEventArgs.X, mouseMoveEventArgs.Y);
      var delta = oldPos - m_mousePos;

      var windowSize = m_renderWindow.Size;
      var viewSize = m_view.Size;
      var movement = new Vector2f(
        ((float)delta.X / windowSize.X) * viewSize.X,
        ((float)delta.Y / windowSize.Y) * viewSize.Y
        );
      m_view.Center += movement;
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
