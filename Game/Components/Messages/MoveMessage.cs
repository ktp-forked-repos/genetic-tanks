namespace GeneticTanks.Game.Components.Messages
{
  enum MoveCommand
  {
    // stop everything
    AllStop,

    // accelerate forward by 10%
    SpeedForwardIncrease,
    // accelerate backwards by 10%
    SpeedReverseIncrease,
    // accelerate turning to the left by 10%
    TurnLeftIncrease,
    // accelerate turning to the right by 10%
    TurnRightIncrease,

    // absolute forward/back commands
    SpeedForwardSlow,
    SpeedForwardHalf,
    SpeedForwardFull,
    SpeedStop,
    SpeedReverseSlow,
    SpeedReverseHalf,
    SpeedReverseFull,

    // absolute turn speed commands
    TurnLeftSlow,
    TurnLeftHalf,
    TurnLeftFull,
    TurnStop,
    TurnRightSlow,
    TurnRightHalf,
    TurnRightFull
  }

  sealed class MoveMessage
    : Message
  {
    public MoveMessage(MoveCommand cmd)
    {
      MoveCommand = cmd;
    }

    public MoveCommand MoveCommand { get; private set; }
  }
}
