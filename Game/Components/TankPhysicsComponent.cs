using System;
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
    private Vector2 m_desiredVelocity = Vector2.Zero;
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

      m_body = BodyFactory.CreateRectangle(m_physicsManager.World,
        size.X, size.Y, 1, pos, Parent.Id);
      m_body.BodyType = BodyType.Dynamic;
      m_body.CollisionCategories = PhysicsManager.TankCategory;
      m_body.CollidesWith = Category.All;
      m_body.Rotation = rot;

      m_body.OnCollision += HandleCollision;
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
    private bool HandleCollision(Fixture fixtureA, Fixture fixtureB, 
      Contact contact)
    {
      // TODO: implement me
      return true;
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

      m_physicsManager.World.RemoveBody(m_body);
      m_body = null;

      base.Dispose(disposing);
      m_disposed = true;
    }

    #endregion
  }
}
