using System.Reflection.Emit;
using GeneticTanks.Game.Components;
using GeneticTanks.Game.Components.Tank;
using Microsoft.Xna.Framework;
using SFML.Window;

// All events triggered by user input go here.

namespace GeneticTanks.Game.Events
{
  /// <summary>
  /// The base for all input events.
  /// </summary>
  abstract class InputEvent
    : Event
  {
  }

  /// <summary>
  /// Signals that the user has dragged the map view.
  /// </summary>
  sealed class MapDragEvent
    : InputEvent
  {
    public MapDragEvent(Vector2 delta)
    {
      Delta = delta;
    }

    /// <summary>
    /// The amount that the user dragged as a percentage of the window size.
    /// </summary>
    public Vector2 Delta { get; private set; }
  }

  /// <summary>
  /// Signals that the user has zoomed the map in or out.
  /// </summary>
  sealed class MapZoomEvent
    : InputEvent
  {
    public MapZoomEvent(int amount)
    {
      Amount = amount;
    }

    /// <summary>
    /// The number of steps to zoom.  Positive value is zoom in, negative 
    /// zoom out.
    /// </summary>
    public int Amount { get; private set; }
  }

  /// <summary>
  /// Signals user input to move an object.
  /// </summary>
  sealed class UserMoveEvent
    : InputEvent
  {
    public UserMoveEvent(MoveState state,
      MoveDirection dir = MoveDirection.None)
    {
      State = state;
      Direction = dir;
    }

    public MoveState State { get; private set; }
    public MoveDirection Direction { get; private set; }
  }

  /// <summary>
  /// Signals that the render window has been resized.
  /// </summary>
  /// <remarks>
  /// Should probably go somewhere else?  But it's technically an input thing.
  /// </remarks>
  sealed class WindowResizeEvent
    : InputEvent
  {
    public WindowResizeEvent(Vector2u size)
    {
      Size = size;
    }

    /// <summary>
    /// The new size of the window.
    /// </summary>
    public Vector2u Size { get; private set; }
  }
}
