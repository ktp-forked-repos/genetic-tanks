using System;
using System.Diagnostics;
using System.Reflection;
using GeneticTanks.Extensions;
using GeneticTanks.Game.Events;
using log4net;
using SFML.Graphics;
using SFML.Window;
using Event = GeneticTanks.Game.Events.Event;

namespace GeneticTanks.Game
{
  sealed class ViewManager
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    private const float BaseViewWidth = 100;
    private const float MinViewWidth = 10;
    private const float ZoomIncrement = 10;

    private readonly EventManager m_eventManager;
    private Vector2u m_windowSize;
    private float m_viewWidth = BaseViewWidth;

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

    public View View { get; private set; }

    private void UpdateViewSize()
    {
      var ratio = (float)m_windowSize.Y / m_windowSize.X;
      View.Size = new Vector2f(m_viewWidth, m_viewWidth * ratio);
    }

    #region Callbacks

    private void HandleWindowResize(Event e)
    {
      var evt = e as WindowResizeEvent;
      Debug.Assert(evt != null);

      m_windowSize = evt.Size;
      UpdateViewSize();
    }

    private void HandleMapDrag(Event e)
    {
      var evt = e as MapDragEvent;
      Debug.Assert(evt != null);

      var delta = evt.Delta;
      var size = View.Size;
      View.Center += new Vector2f(delta.X * size.X, delta.Y * size.Y);
    }

    private void HandleMapZoom(Event e)
    {
      var evt = e as MapZoomEvent;
      Debug.Assert(evt != null);

      m_viewWidth += -evt.Amount * (m_viewWidth / ZoomIncrement);
      m_viewWidth = Math.Max(m_viewWidth, MinViewWidth);
      UpdateViewSize();
    }

    #endregion

    ~ViewManager()
    {
      m_eventManager.RemoveListener<WindowResizeEvent>(HandleWindowResize);
      m_eventManager.RemoveListener<MapDragEvent>(HandleMapDrag);
      m_eventManager.RemoveListener<MapZoomEvent>(HandleMapZoom);
    }
  }
}
