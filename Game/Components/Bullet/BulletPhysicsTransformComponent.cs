using System;
using System.Reflection;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Factories;
using GeneticTanks.Extensions;
using GeneticTanks.Game.Events;
using GeneticTanks.Game.Managers;
using log4net;

namespace GeneticTanks.Game.Components.Bullet
{
  /// <summary>
  /// Handles bullet collision physics.  Triggers a TankHitEvent when then 
  /// bullet impacts a tank, and requests the bullet's removal when it hits a 
  /// tank or piece of terrain.
  /// </summary>
  sealed class BulletPhysicsTransformComponent
    : PhysicsTransformComponent
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    private readonly EventManager m_eventManager;
    private BulletDataComponent m_data;

    /// <summary>
    /// Create the component.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="pm"></param>
    /// <param name="eventManager"></param>
    public BulletPhysicsTransformComponent(Entity parent, PhysicsManager pm,
      EventManager eventManager) 
      : base(parent, pm)
    {
      if (eventManager == null)
      {
        throw new ArgumentNullException("eventManager");
      }

      m_eventManager = eventManager;
      NeedsUpdate = false;
    }

    #region PhysicsTransformComponent Implementation

    public override bool Initialize()
    {
      if (!RetrieveSibling(out m_data))
      {
        return false;
      }

      Body = BodyFactory.CreateCircle(World, m_data.Radius, 0, Parent.Id);
      Body.BodyType = BodyType.Dynamic;
      Body.IsBullet = true;
      Body.FixedRotation = true;
      Body.CollisionCategories = PhysicsManager.BulletCategory;
      Body.CollidesWith =
        PhysicsManager.TankCategory | PhysicsManager.TerrainCategory;
      Body.LinearVelocity = m_data.Velocity;
      
      Enable();
      Initialized = true;
      return true;
    }

    public override void Enable()
    {
      base.Enable();

      Body.OnCollision += HandleCollision;
    }

    public override void Disable()
    {
      Body.OnCollision -= HandleCollision;
      base.Disable();
    }

    public override void Deactivate()
    {
      Disable();
      base.Deactivate();
    }

    #endregion
    #region Callbacks

    private bool HandleCollision(Fixture fixtureA, Fixture fixtureB, 
      Contact contact)
    {
      if ((fixtureB.CollisionCategories & PhysicsManager.TerrainCategory) > 0)
      {
        m_eventManager.QueueEvent(new RequestEntityRemovalEvent(Parent.Id));
        Body.Enabled = false;
      }
      else if ((fixtureB.CollisionCategories & PhysicsManager.TankCategory) > 0)
      {
        uint target = Convert.ToUInt32(fixtureB.UserData);
        if (target != m_data.FiringEntity)
        {
          m_eventManager.QueueEvent(
            new TankHitEvent(m_data.FiringEntity, target, m_data.Damage));
          m_eventManager.QueueEvent(new RequestEntityRemovalEvent(Parent.Id));
          Body.Enabled = false;
        }
      }
      else
      {
        Log.WarnFmt(
        "{0} collided with something it probably shouldn't? Categories {1:X} " +
        "id {2}", Parent.FullName, fixtureB.CollisionCategories,
        Convert.ToUInt32(fixtureB.UserData));
      }
      
      return false;
    }

    #endregion
  }
}
