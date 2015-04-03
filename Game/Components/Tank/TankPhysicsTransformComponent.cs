using System;
using System.Reflection;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Factories;
using GeneticTanks.Game.Components.Messages;
using log4net;
using Microsoft.Xna.Framework;

namespace GeneticTanks.Game.Components.Tank
{
  /// <summary>
  /// Handles tank collision and movement physics, uses the physics object to
  /// provide the tank's transform.
  /// </summary>
  sealed class TankPhysicsTransformComponent
    : PhysicsTransformComponent
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);
    
    #region Private Fields
    private readonly PhysicsManager m_physicsManager;
    private TankStateComponent m_state;
    private MessageComponent m_messenger;

    private float m_desiredRotationRate = 0f;
    private float m_desiredSpeed = 0f;
    #endregion

    /// <summary>
    /// Create the component.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="pm"></param>
    public TankPhysicsTransformComponent(Entity parent, PhysicsManager pm) 
      : base(parent)
    {
      if (pm == null)
      {
        throw new ArgumentNullException("pm");
      }

      m_physicsManager = pm;
      NeedsUpdate = false;
    }

    /// <summary>
    /// The rate at which the tank is actually rotating.
    /// </summary>
    public float RotationRate
    {
      get { return MathHelper.ToDegrees(Body.AngularVelocity); }
    }

    /// <summary>
    /// The actual speed of the tank.
    /// </summary>
    public float Speed
    {
      get { return Body.LinearVelocity.Length(); }
    }

    /// <summary>
    /// The rate at which the tank wants to be rotating.  Positive is CCW, 
    /// negative is CW.
    /// </summary>
    public float DesiredRotationRate
    {
      get { return m_desiredRotationRate; }
      private set {
        m_desiredRotationRate = value >= 0 
          ? Math.Min(value, m_state.MaxRotationRate) 
          : Math.Max(value, -m_state.MaxRotationRate);
      }
    }

    /// <summary>
    /// The speed that the tank wants to be going.  Positive is forward, 
    /// negative is backwards.
    /// </summary>
    public float DesiredSpeed
    {
      get { return m_desiredSpeed; }
      private set
      {
        m_desiredSpeed = value >= 0
          ? Math.Min(value, m_state.MaxSpeed)
          : Math.Max(value, -m_state.MaxSpeed);
      }
    }

    #region TransformComponent Implementation
    
    public override bool Initialize()
    {
      if (Parent.Transform == null)
      {
        return false;
      }
      if (!RetrieveSibling(out m_state))
      {
        return false;
      }
      if (!RetrieveSibling(out m_messenger))
      {
        return false;
      }

      World = m_physicsManager.World;

      var size = m_state.Dimensions;
      size.Y += m_state.TrackWidth * 2;

      Body = BodyFactory.CreateBody(World, Parent.Id);
      FixtureFactory.AttachRectangle(size.X, size.Y, 1, Vector2.Zero, Body, 
        Parent.Id);
      Body.BodyType = BodyType.Dynamic;
      Body.CollisionCategories = PhysicsManager.TankCategory;
      Body.CollidesWith = Category.All;
      
      Body.OnCollision += HandleChassisCollision;
      m_physicsManager.PreStep += HandlePreStep;

      m_messenger.AddListener<MoveMessage>(HandleMoveMessage);

      Initialized = true;
      return true;
    }
    
    public override void Update(float deltaTime)
    {
    }

    #endregion
    #region Callbacks

    // detect when a bullet hits this tank, and prevent this tank's bullets
    // from hitting itself
    private bool HandleChassisCollision(Fixture fixtureA, Fixture fixtureB, 
      Contact contact)
    {
      // TODO: implement me
      return true;
    }
    
    // applies impulses to make the tank move and turn
    private void HandlePreStep(float deltaTime)
    {
      var deltaRot = 
        MathHelper.ToRadians(m_desiredRotationRate) - Body.AngularVelocity;
      var impulseRot = deltaRot * Body.Mass;
      Body.ApplyAngularImpulse(impulseRot);

      var velocity = Body.LinearVelocity;
      var desiredVelocity = Body.GetWorldVector(ForwardVector) * m_desiredSpeed;
      var deltaVel = desiredVelocity - velocity;
      var impulseVel = deltaVel * Body.Mass;
      Body.ApplyLinearImpulse(impulseVel);
    }

    private void HandleMoveMessage(Message m)
    {
      var msg = (MoveMessage) m;

      switch (msg.MoveCommand)
      {
        case Messages.MoveCommand.AllStop:
          DesiredRotationRate = 0;
          DesiredSpeed = 0;
          break;
        case Messages.MoveCommand.SpeedForwardIncrease:
          DesiredSpeed += m_state.MaxSpeed / 10f;
          break;
        case Messages.MoveCommand.SpeedReverseIncrease:
          DesiredSpeed -= m_state.MaxSpeed / 10f;
          break;
        case Messages.MoveCommand.TurnLeftIncrease:
          DesiredRotationRate += m_state.MaxRotationRate / 10f;
          break;
        case Messages.MoveCommand.TurnRightIncrease:
          DesiredRotationRate -= m_state.MaxRotationRate / 10f;
          break;
        case Messages.MoveCommand.SpeedForwardSlow:
          DesiredSpeed = m_state.MaxSpeed / 4f;
          break;
        case Messages.MoveCommand.SpeedForwardHalf:
          DesiredSpeed = m_state.MaxSpeed / 2f;
          break;
        case Messages.MoveCommand.SpeedForwardFull:
          DesiredSpeed = m_state.MaxSpeed;
          break;
        case Messages.MoveCommand.SpeedStop:
          DesiredSpeed = 0;
          break;
        case Messages.MoveCommand.SpeedReverseSlow:
          DesiredSpeed = -(m_state.MaxSpeed / 4f);
          break;
        case Messages.MoveCommand.SpeedReverseHalf:
          DesiredSpeed = -(m_state.MaxSpeed / 2f);
          break;
        case Messages.MoveCommand.SpeedReverseFull:
          DesiredSpeed = -m_state.MaxSpeed;
          break;
        case Messages.MoveCommand.TurnLeftSlow:
          DesiredRotationRate = m_state.MaxRotationRate / 4f;
          break;
        case Messages.MoveCommand.TurnLeftHalf:
          DesiredRotationRate = m_state.MaxRotationRate / 2f;
          break;
        case Messages.MoveCommand.TurnLeftFull:
          DesiredRotationRate = m_state.MaxRotationRate;
          break;
        case Messages.MoveCommand.TurnStop:
          DesiredRotationRate = 0;
          break;
        case Messages.MoveCommand.TurnRightSlow:
          DesiredRotationRate = -(m_state.MaxRotationRate / 4f);
          break;
        case Messages.MoveCommand.TurnRightHalf:
          DesiredRotationRate = -(m_state.MaxRotationRate / 2f);
          break;
        case Messages.MoveCommand.TurnRightFull:
          DesiredRotationRate = -m_state.MaxRotationRate;
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

      m_physicsManager.PreStep -= HandlePreStep;
      Body.OnCollision -= HandleChassisCollision;

      m_physicsManager.World.RemoveBody(Body);
      Body = null;

      base.Dispose(disposing);
      m_disposed = true;
    }

    #endregion
  }
}
