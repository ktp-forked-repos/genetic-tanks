using System;

namespace GeneticTanks.Game.Components.Messages
{
  /// <summary>
  /// The base for all inter-component messages.
  /// </summary>
  abstract class Message
    : EventArgs
  {
    protected Message()
    {
      TimeStamp = DateTime.Now.Ticks;
    }

    /// <summary>
    /// THe creation time of the message in system ticks.
    /// </summary>
    public long TimeStamp { get; private set; }
  }
}
