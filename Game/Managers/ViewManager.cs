using System;
using System.Reflection;
using GeneticTanks.Game.Events;
using log4net;
using SFML.Graphics;
using SFML.Window;
using Event = GeneticTanks.Game.Events.Event;

namespace GeneticTanks.Game.Managers
{
  /// <summary>
  /// Controls the user's view into the game map.
  /// </summary>
  sealed class ViewManager
    : IDisposable
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    // initial view width
    private const float BaseViewWidth = 100;
    // smallest allowable view
    private const float MinViewWidth = 10;
    // the amount the zoom changes on every zoom event
    private const float ZoomPercentIncrement = 10;

    #region Private Fields
    private readonly EventManager m_eventManager;

    // tracks the size of the rendering window
    private Vector2u m_windowSize;
    // current width of the view
    private float m_viewWidth = BaseViewWidth;
    #endregion

    /// <summary>
    /// Create a view manager.
    /// </summary>
    /// <param name="em"></param>
    /// <param name="window"></param>
    /// <exception cref="ArgumentNullException">
    /// em or window is null.
    /// </exception>
    public ViewManager(EventManager em, Window window)
    {
      if (em == null)
      {
        throw new ArgumentNullException("em");
      }
      if (window == null)
      {
        throw new ArgumentNullException("window");
      }

      m_eventManager = em;
      m_windowSize = window.Size;

      View = new View
      {
        Center = new Vector2f(0, 0),
        Viewport = new FloatRect(0, 0, 1, 1)
      };
      UpdateViewSize();

      m_eventManager.AddListener<WindowResizeEvent>(HandleWindowResize);
      m_eventManager.AddListener<MapDragEvent>(HandleMapDrag);
      m_eventManager.AddListener<MapZoomEvent>(HandleMapZoom);
    }

    /// <summary>
    /// The current view for rendering.
    /// </summary>
    public View View { get; private set; }

    #region Private Methods

    private void UpdateViewSize()
    {
      var ratio = (float)m_windowSize.Y / m_windowSize.X;
      View.Size = new Vector2f(m_viewWidth, m_viewWidth * ratio);
    }

    #endregion
    #region Callbacks

    private void HandleWindowResize(Event e)
    {
      var evt = (WindowResizeEvent) e;

      m_windowSize = evt.Size;
      UpdateViewSize();
    }

    private void HandleMapDrag(Event e)
    {
      var evt = (MapDragEvent) e;

      var delta = evt.Delta;
      var size = View.Size;
      View.Center += new Vector2f(delta.X * size.X, delta.Y * size.Y);
    }

    private void HandleMapZoom(Event e)
    {
      var evt = (MapZoomEvent) e;

      m_viewWidth += -evt.Amount * (m_viewWidth / ZoomPercentIncrement);
      m_viewWidth = Math.Max(m_viewWidth, MinViewWidth);
      UpdateViewSize();
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

      m_eventManager.RemoveListener<WindowResizeEvent>(HandleWindowResize);
      m_eventManager.RemoveListener<MapDragEvent>(HandleMapDrag);
      m_eventManager.RemoveListener<MapZoomEvent>(HandleMapZoom);

      m_disposed = true;
    }

    ~ViewManager()
    {
      Dispose(false);
    }

    #endregion
  }
}
