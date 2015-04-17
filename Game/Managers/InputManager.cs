using System;
using System.Reflection;
using GeneticTanks.Game.Components.Tank;
using GeneticTanks.Game.Events;
using log4net;
using Microsoft.Xna.Framework;
using SFML.Window;

namespace GeneticTanks.Game.Managers
{
  /// <summary>
  /// Captures input from the window and translates it to game events.
  /// </summary>
  sealed class InputManager
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    #region Private Fields
    private readonly Window m_window;
    private readonly EventManager m_eventManager;
    private bool m_dragging = false;
    private Vector2i m_mousePos;
    #endregion

    /// <summary>
    /// Create the input manager.
    /// </summary>
    /// <param name="window"></param>
    /// <param name="em"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public InputManager(Window window, EventManager em)
    {
      if (window == null)
      {
        throw new ArgumentNullException("window");
      }
      if (em == null)
      {
        throw new ArgumentNullException("em");
      }

      m_window = window;
      m_eventManager = em;

      m_window.Resized += HandleWindowResized;
      m_window.MouseWheelMoved += HandleMouseWheelMoved;
      m_window.MouseButtonPressed += HandleMouseButtonPressed;
      m_window.MouseButtonReleased += HandleMouseButtonReleased;
      m_window.MouseMoved += HandleMouseMoved;
      m_window.KeyPressed += HandleKeyPressed;
      m_window.KeyReleased += HandleKeyReleased;
    }
    
    /// <summary>
    /// Dispatches events for the controlled window.
    /// </summary>
    /// <param name="deltaTime"></param>
    public void Update(float deltaTime)
    {
      m_window.DispatchEvents();
    }

    #region Callbacks

    // Forwards window resize events
    private void HandleWindowResized(object sender, SizeEventArgs sizeEventArgs)
    {
      var size = new Vector2u(sizeEventArgs.Width, sizeEventArgs.Height);
      m_eventManager.QueueEvent(new WindowResizeEvent(size));
    }

    // Translates mouse wheel movement to zoom events
    private void HandleMouseWheelMoved(object sender,
      MouseWheelEventArgs mouseWheelEventArgs)
    {
      m_eventManager.QueueEvent(new MapZoomEvent(mouseWheelEventArgs.Delta));
    }

    // Begins dragging the map
    private void HandleMouseButtonPressed(object sender,
      MouseButtonEventArgs mouseButtonEventArgs)
    {
      if (mouseButtonEventArgs.Button != Mouse.Button.Left)
      {
        return;
      }

      m_dragging = true;
      m_mousePos = new Vector2i(mouseButtonEventArgs.X, mouseButtonEventArgs.Y);
    }

    // Ends dragging the map
    private void HandleMouseButtonReleased(object sender,
      MouseButtonEventArgs mouseButtonEventArgs)
    {
      if (mouseButtonEventArgs.Button != Mouse.Button.Left)
      {
        return;
      }

      m_dragging = false;
    }

    // Translates mouse movement to a dragging event
    private void HandleMouseMoved(object sender,
      MouseMoveEventArgs mouseMoveEventArgs)
    {
      if (!m_dragging)
      {
        return;
      }

      var oldPos = m_mousePos;
      m_mousePos = new Vector2i(mouseMoveEventArgs.X, mouseMoveEventArgs.Y);
      var deltaPos = oldPos - m_mousePos;

      var size = m_window.Size;
      var deltaPercent = new Vector2(
        ((float)deltaPos.X / size.X),
        ((float)deltaPos.Y / size.Y)
        );

      m_eventManager.QueueEvent(new MapDragEvent(deltaPercent));
    }

    private void HandleKeyPressed(object sender, KeyEventArgs keyEventArgs)
    {
      switch (keyEventArgs.Code)
      {
        case Keyboard.Key.Left:
          m_eventManager.QueueEvent(
            new UserMoveEvent(MoveState.Begin, MoveDirection.Left));
          break;

        case Keyboard.Key.Right:
          m_eventManager.QueueEvent(
            new UserMoveEvent(MoveState.Begin, MoveDirection.Right));
          break;

        case Keyboard.Key.Up:
          m_eventManager.QueueEvent(
            new UserMoveEvent(MoveState.Begin, MoveDirection.Forward));
          break;

        case Keyboard.Key.Down:
          m_eventManager.QueueEvent(
            new UserMoveEvent(MoveState.Begin, MoveDirection.Back));
          break;
      }
    }

    private void HandleKeyReleased(object sender, KeyEventArgs keyEventArgs)
    {
      switch (keyEventArgs.Code)
      {
        case Keyboard.Key.Left:
          m_eventManager.QueueEvent(
            new UserMoveEvent(MoveState.End, MoveDirection.Left));
          break;

        case Keyboard.Key.Right:
          m_eventManager.QueueEvent(
            new UserMoveEvent(MoveState.End, MoveDirection.Right));
          break;

        case Keyboard.Key.Up:
          m_eventManager.QueueEvent(
            new UserMoveEvent(MoveState.End, MoveDirection.Forward));
          break;

        case Keyboard.Key.Down:
          m_eventManager.QueueEvent(
            new UserMoveEvent(MoveState.End, MoveDirection.Back));
          break;
      }
    }

    #endregion

    ~InputManager()
    {
      m_window.Resized -= HandleWindowResized;
      m_window.MouseWheelMoved -= HandleMouseWheelMoved;
      m_window.MouseButtonPressed -= HandleMouseButtonPressed;
      m_window.MouseButtonReleased -= HandleMouseButtonReleased;
      m_window.MouseMoved -= HandleMouseMoved;
    }
  }
}
