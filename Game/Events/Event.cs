using System;

namespace GeneticTanks.Game.Events
{
  /// <summary>
  /// The base class for all events in the game.
  /// </summary>
  abstract class Event
    : EventArgs
  {
    /// <summary>
    /// Creates the event.
    /// </summary>
    protected Event()
    {
      TimeStamp = DateTime.Now.Ticks;
    }

    /// <summary>
    /// The creation timestamp of the event in system ticks.
    /// </summary>
    public long TimeStamp { get; private set; }
  }
}
