using System;
using System.Reflection;
using GeneticTanks.Game.Events;
using log4net;

namespace GeneticTanks.Game.Components
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
    private TankPhysicsComponent m_tankPhysics;
    private TankStateComponent m_state;
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
      if (!RetrieveSibling(out m_tankPhysics))
      {
        return false;
      }
      if (!RetrieveSibling(out m_state))
      {
        return false;
      }

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

      var speed = m_state.MaxSpeed;
      var rotation = m_state.MaxRotationRate;
      
      switch (evt.Direction)
      {
        case MoveDirection.Forward:
          m_tankPhysics.DesiredSpeed = 
            evt.State == MoveState.Begin ? speed : 0;
          break;

        case MoveDirection.Back:
          m_tankPhysics.DesiredSpeed =
            evt.State == MoveState.Begin ? -speed : 0;
          break;

        case MoveDirection.Left:
          m_tankPhysics.DesiredRotationRate =
            evt.State == MoveState.Begin ? rotation : 0;
          break;

        case MoveDirection.Right:
          m_tankPhysics.DesiredRotationRate =
            evt.State == MoveState.Begin ? -rotation : 0;
          break;
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
