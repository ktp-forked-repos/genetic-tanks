using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Factories;
using log4net;
using Microsoft.Xna.Framework;

namespace GeneticTanks.Game.Components
{
  /// <summary>
  /// Handles tank collision and movement physics.
  /// </summary>
  sealed class TankPhysicsComponent
    : Component
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);
    
    // the local vector pointing towards the front of the tank
    private static readonly Vector2 ForwardVector = new Vector2(1, 0);

    #region Private Fields
    private readonly PhysicsManager m_physicsManager;
    private TransformComponent m_transform;
    private TankStateComponent m_state;

    private Body m_body;
    private Fixture m_chassis;
    private Fixture m_sensor;
    private Vector2 m_desiredVelocity = Vector2.Zero;

    private readonly HashSet<int> m_sensorContacts = new HashSet<int>(); 
    #endregion

    /// <summary>
    /// Create the component.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="pm"></param>
    public TankPhysicsComponent(Entity parent, PhysicsManager pm) 
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
    /// The desired rotation in degrees per second.  Positive rotation is CCW,
    /// negative is CW.
    /// </summary>
    public float DesiredRotationRate { get; set; }

    /// <summary>
    /// The desired speed of the tank in m/s.  Positive is forward, negative is
    /// reverse.
    /// </summary>
    public float DesiredSpeed { get; set; }

    /// <summary>
    /// The list of tank contacts that the sensor has detected.
    /// </summary>
    public List<int> SensorContacts
    {
      get { return m_sensorContacts.ToList(); }
    }

    #region Component Implementation

    public override bool Initialize()
    {
      if (!RetrieveSibling(out m_transform))
      {
        return false;
      }
      if (!RetrieveSibling(out m_state))
      {
        return false;
      }

      var size = m_state.Dimensions;
      size.Y += m_state.TrackWidth * 2;
      var pos = m_transform.Position;
      var rot = MathHelper.ToRadians(m_transform.Rotation);

      m_body = BodyFactory.CreateBody(m_physicsManager.World,
        pos, rot, Parent.Id);
      
      m_chassis = FixtureFactory.AttachRectangle(
        size.X, size.Y, 1, Vector2.Zero, m_body, Parent.Id);
      m_chassis.CollisionCategories = PhysicsManager.TankCategory;
      m_chassis.CollidesWith = Category.All;

      m_sensor = FixtureFactory.AttachCircle(
        m_state.SensorRadius, 0, m_body, Parent.Id);
      m_sensor.CollisionCategories = PhysicsManager.SensorCategory;
      m_sensor.CollidesWith = PhysicsManager.TankCategory;
      m_sensor.IsSensor = true;

      m_body.BodyType = BodyType.Dynamic;

      m_chassis.OnCollision += HandleChassisCollision;
      m_sensor.OnCollision += HandleSensorCollision;
      m_sensor.OnSeparation += HandleSensorSeparation;
      m_physicsManager.PreStep += HandlePreStep;
      m_physicsManager.PostStep += HandlePostStep;

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
        m_sensorContacts.Add(Convert.ToInt32(fixtureB.UserData));
        Log.DebugFormat("{0} contacted {1}", fixtureA.UserData, fixtureB.UserData);
      }

      return true;
    }

    private void HandleSensorSeparation(Fixture fixtureA, Fixture fixtureB)
    {
      m_sensorContacts.Remove(Convert.ToInt32(fixtureB.UserData));
    }
    
    // applies impulses to make the tank move and turn
    private void HandlePreStep(float deltaTime)
    {
      var deltaRot = 
        MathHelper.ToRadians(DesiredRotationRate) - m_body.AngularVelocity;
      var impulseRot = deltaRot * m_body.Mass;
      m_body.ApplyAngularImpulse(impulseRot);

      var velocity = m_body.LinearVelocity;
      var deltaVel = m_desiredVelocity - velocity;
      var impulseVel = deltaVel * m_body.Mass;
      m_body.ApplyLinearImpulse(impulseVel);
    }

    // sync the physics position back to the transform
    private void HandlePostStep(float deltaTime)
    {
      m_transform.Rotation = MathHelper.ToDegrees(m_body.Rotation);
      m_transform.Position = m_body.Position;

      // update the velocity vector in case the tank turned
      m_desiredVelocity = m_body.GetWorldVector(ForwardVector) * DesiredSpeed;
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
      m_physicsManager.PostStep -= HandlePostStep;
      m_sensor.OnCollision -= HandleSensorCollision;
      m_sensor.OnSeparation -= HandleSensorSeparation;
      m_chassis.OnCollision -= HandleChassisCollision;

      m_chassis = null;
      m_sensor = null;
      m_physicsManager.World.RemoveBody(m_body);
      m_body = null;

      base.Dispose(disposing);
      m_disposed = true;
    }

    #endregion
  }
}
