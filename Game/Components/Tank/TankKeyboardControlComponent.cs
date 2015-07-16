using System;
using System.Reflection;
using GeneticTanks.Game.Components.Messages;
using GeneticTanks.Game.Events;
using GeneticTanks.Game.Managers;
using log4net;

namespace GeneticTanks.Game.Components.Tank
{
  public enum MoveState
  {
    Begin,
    End
  }

  public enum MoveDirection
  {
    None,
    Left,
    Right,
    Forward,
    Back
  }

  /// <summary>
  /// Allows a tank to be controlled by user input.  Should only be used one 
  /// tank, because otherwise all tanks with this component will receive
  /// identical commands.
  /// </summary>
  sealed class TankKeyboardControlComponent
    : Component
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    #region Private Fields
    private readonly EventManager m_eventManager;
    #endregion

    /// <summary>
    /// Create the component.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="em"></param>
    public TankKeyboardControlComponent(Entity parent, EventManager em) 
      : base(parent)
    {
      if (em == null)
      {
        throw new ArgumentNullException("em");
      }

      m_eventManager = em;
      NeedsUpdate = false;
    }

    #region Component Implementation

    public override bool Initialize()
    {
      m_eventManager.AddListener<UserMoveEvent>(HandleUserMove);

      Initialized = true;
      return true;
    }

    public override void Update(float deltaTime)
    {
    }

    #endregion
    #region Callbacks

    private void HandleUserMove(Event e)
    {
      var evt = (UserMoveEvent) e;
      MoveCommand moveCommand = MoveCommand.AllStop;

      switch (evt.Direction)
      {
        case MoveDirection.Forward:
          moveCommand = evt.State == MoveState.Begin
            ? MoveCommand.SpeedForwardFull
            : MoveCommand.SpeedStop;
          break;

        case MoveDirection.Back:
          moveCommand = evt.State == MoveState.Begin
            ? MoveCommand.SpeedReverseFull
            : MoveCommand.SpeedStop;
          break;

        case MoveDirection.Left:
          moveCommand = evt.State == MoveState.Begin
            ? MoveCommand.TurnLeftFull
            : MoveCommand.TurnStop;
          break;

        case MoveDirection.Right:
          moveCommand = evt.State == MoveState.Begin
            ? MoveCommand.TurnRightFull
            : MoveCommand.TurnStop;
          break;
      }

      if (moveCommand != MoveCommand.AllStop)
      {
        Parent.QueueMessage(new MoveMessage(moveCommand));
      }
    }

    #endregion
    #region IDisposable Implementation

    private bool m_disposed = false;

    protected override void Dispose(bool disposing)
    {
      if (m_disposed || !Initialized)
      {
        return;
      }

      m_eventManager.RemoveListener<UserMoveEvent>(HandleUserMove);
      base.Dispose(disposing);
      m_disposed = true;
    }

    #endregion
  }
}
