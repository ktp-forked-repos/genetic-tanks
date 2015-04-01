using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Factories;
using GeneticTanks.Game.Components.Messages;
using log4net;
using Microsoft.Xna.Framework;

namespace GeneticTanks.Game.Components
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
    
    // the local vector pointing towards the front of the tank
    private static readonly Vector2 ForwardVector = new Vector2(1, 0);

    #region Private Fields
    private readonly PhysicsManager m_physicsManager;
    private TankStateComponent m_state;
    private MessageComponent m_messenger;

    private Fixture m_chassis;
    private Fixture m_sensor;

    private float m_desiredRotationRate = 0f;
    private float m_desiredSpeed = 0f;

    private readonly HashSet<int> m_sensorContacts = new HashSet<int>(); 
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

    /// <summary>
    /// The list of tank contacts that the sensor has detected.
    /// </summary>
    public List<int> SensorContacts
    {
      get { return m_sensorContacts.ToList(); }
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

      var size = m_state.Dimensions;
      size.Y += m_state.TrackWidth * 2;

      Body = BodyFactory.CreateBody(m_physicsManager.World,
        Position, Rotation, Parent.Id);
      
      m_chassis = FixtureFactory.AttachRectangle(
        size.X, size.Y, 1, Vector2.Zero, Body, Parent.Id);
      m_chassis.CollisionCategories = PhysicsManager.TankCategory;
      m_chassis.CollidesWith = Category.All;

      m_sensor = FixtureFactory.AttachCircle(
        m_state.SensorRadius, 0, Body, Parent.Id);
      m_sensor.CollisionCategories = PhysicsManager.SensorCategory;
      m_sensor.CollidesWith = PhysicsManager.TankCategory;
      m_sensor.IsSensor = true;

      Body.BodyType = BodyType.Dynamic;

      m_chassis.OnCollision += HandleChassisCollision;
      m_sensor.OnCollision += HandleSensorCollision;
      m_sensor.OnSeparation += HandleSensorSeparation;
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

    private bool HandleSensorCollision(Fixture fixtureA, Fixture fixtureB,
      Contact contact)
    {
      if ((fixtureB.CollisionCategories & PhysicsManager.TankCategory) > 0)
      {
        var id = Convert.ToInt32(fixtureB.UserData);
        if (!m_sensorContacts.Contains(id))
        {
          m_sensorContacts.Add(id);
          m_messenger.QueueMessage(new SensorNewContactMessage(id));
        }
      }

      return true;
    }

    private void HandleSensorSeparation(Fixture fixtureA, Fixture fixtureB)
    {
      var id = Convert.ToInt32(fixtureB.UserData);
      if (m_sensorContacts.Remove(id))
      {
        m_messenger.QueueMessage(new SensorLostContactMessage(id));
      }
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

      switch (msg.Move)
      {
        case Messages.Move.AllStop:
          DesiredRotationRate = 0;
          DesiredSpeed = 0;
          break;
        case Messages.Move.SpeedForwardIncrease:
          DesiredSpeed += m_state.MaxSpeed / 10f;
          break;
        case Messages.Move.SpeedReverseIncrease:
          DesiredSpeed -= m_state.MaxSpeed / 10f;
          break;
        case Messages.Move.TurnLeftIncrease:
          DesiredRotationRate += m_state.MaxRotationRate / 10f;
          break;
        case Messages.Move.TurnRightIncrease:
          DesiredRotationRate -= m_state.MaxRotationRate / 10f;
          break;
        case Messages.Move.SpeedForwardSlow:
          DesiredSpeed = m_state.MaxSpeed / 4f;
          break;
        case Messages.Move.SpeedForwardHalf:
          DesiredSpeed = m_state.MaxSpeed / 2f;
          break;
        case Messages.Move.SpeedForwardFull:
          DesiredSpeed = m_state.MaxSpeed;
          break;
        case Messages.Move.SpeedStop:
          DesiredSpeed = 0;
          break;
        case Messages.Move.SpeedReverseSlow:
          DesiredSpeed = -(m_state.MaxSpeed / 4f);
          break;
        case Messages.Move.SpeedReverseHalf:
          DesiredSpeed = -(m_state.MaxSpeed / 2f);
          break;
        case Messages.Move.SpeedReverseFull:
          DesiredSpeed = -m_state.MaxSpeed;
          break;
        case Messages.Move.TurnLeftSlow:
          DesiredRotationRate = m_state.MaxRotationRate / 4f;
          break;
        case Messages.Move.TurnLeftHalf:
          DesiredRotationRate = m_state.MaxRotationRate / 2f;
          break;
        case Messages.Move.TurnLeftFull:
          DesiredRotationRate = m_state.MaxRotationRate;
          break;
        case Messages.Move.TurnStop:
          DesiredRotationRate = 0;
          break;
        case Messages.Move.TurnRightSlow:
          DesiredRotationRate = -(m_state.MaxRotationRate / 4f);
          break;
        case Messages.Move.TurnRightHalf:
          DesiredRotationRate = -(m_state.MaxRotationRate / 2f);
          break;
        case Messages.Move.TurnRightFull:
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
      m_sensor.OnCollision -= HandleSensorCollision;
      m_sensor.OnSeparation -= HandleSensorSeparation;
      m_chassis.OnCollision -= HandleChassisCollision;

      m_chassis = null;
      m_sensor = null;
      m_physicsManager.World.RemoveBody(Body);
      Body = null;

      base.Dispose(disposing);
      m_disposed = true;
    }

    #endregion
  }
}
