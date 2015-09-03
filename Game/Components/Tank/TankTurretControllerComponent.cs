using System;
using System.Reflection;
using GeneticTanks.Extensions;
using GeneticTanks.Game.Components.Messages;
using log4net;
using Microsoft.Xna.Framework;

namespace GeneticTanks.Game.Components.Tank
{
  sealed class TankTurretControllerComponent
    : Component
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    private const float UpdateInterval = 1f / 30f;
    private const float TargetAlignmentThreshold = 0.1f;

    #region Private Fields
    private PhysicsTransformComponent m_physics;
    private TankStateComponent m_state;

    private float m_timeSinceLastUpdate = 0f;
    
    private bool m_enabled = false;
    private Vector2 m_targetDirection;
    private float m_rotationTarget = 0f;
    
    private Entity m_target = null;
    private bool m_targetAligned = false;

    private bool m_firing;
    private bool m_reloading = false;
    private float m_reloadTime = 0f;

    #endregion

    /// <summary>
    /// Create the component.
    /// </summary>
    /// <param name="parent"></param>
    public TankTurretControllerComponent(Entity parent) 
      : base(parent)
    {
      NeedsUpdate = true;
    }

    #region Component Implmentation

    public override bool Initialize()
    {
      if (!RetrieveBaseSibling(out m_physics))
      {
        return false;
      }
      if (!RetrieveSibling(out m_state))
      {
        return false;
      }

      Parent.AddListener<SetTargetMessage>(HandleSetTarget);
      Parent.AddListener<ShootingStateMessage>(HandleShootingStateChange);
      Parent.AddListener<TankKilledMessage>(HandleTankKilled);

      Initialized = true;
      return true;
    }

    public override void Update(float deltaTime)
    {
      if (!m_enabled)
      {
        return;
      }

      m_timeSinceLastUpdate += deltaTime;
      if (m_timeSinceLastUpdate < UpdateInterval)
      {
        return;
      }

      var elapsed = UpdateInterval * 
        (float)Math.Floor(m_timeSinceLastUpdate / UpdateInterval);
      m_timeSinceLastUpdate = m_timeSinceLastUpdate % UpdateInterval;
      
      UpdateRotation(elapsed);
      UpdateFiring(elapsed);
    }

    #endregion
    #region Private Methods

    private void UpdateFiring(float deltaTime)
    {
      if (m_reloading)
      {
        m_reloadTime -= deltaTime;
        m_reloading = m_reloadTime > 0f;
      }

      if (m_reloading || !m_firing || !m_targetAligned)
      {
        return;
      }
      
      m_targetDirection.Normalize();
      // find the end of the barrel where the bullet will originate
      var totalBarrelLen = (m_state.TurretWidth / 2f) + 
        m_state.BarrelDimensions.X;
      var position = Parent.Transform.Position + 
        (m_targetDirection * totalBarrelLen);
      var velocity = m_targetDirection * m_state.GunSpeed;

      var bullet = BulletFactory.CreateBullet(Parent.Id, m_state.GunDamage, 
        position, velocity, m_state.BarrelDimensions.Y * 0.9f);
      if (bullet == null)
      {
        Log.ErrorFmt("{0} tried to fire a shot but failed", Parent.FullName);
      }
      else
      {
        Parent.QueueMessage(new ShotFiredMessage(bullet.Id));
      }

      m_reloading = true;
      m_reloadTime = m_state.ReloadTime;
    }

    private void UpdateRotation(float deltaTime)
    {
      if (m_target != null)
      {
        UpdateRotationTarget();
      }

      var remaining = m_rotationTarget - m_state.TurretRotation;
      if (remaining <= -180f)
      {
        remaining += 360f;
      }
      else if (remaining >= 180f)
      {
        remaining -= 360f;
      }
      m_targetAligned = Math.Abs(remaining) <= TargetAlignmentThreshold;

      if (!m_targetAligned)
      {
        var rotationDelta = m_state.MaxTurretRotationRate * deltaTime;
        rotationDelta = Math.Min(Math.Abs(remaining), rotationDelta);
        rotationDelta = remaining < 0 ? -rotationDelta : rotationDelta;
        m_state.TurretRotation += rotationDelta;
      }
      else if (m_target == null)
      {
        m_enabled = false;
      }
    }

    private void UpdateRotationTarget()
    {
      var forwardDirection = m_physics.Body.GetWorldVector(
        PhysicsTransformComponent.ForwardVector);
      m_targetDirection =
        m_target.Transform.Position - Parent.Transform.Position;
      var angle = Math.Atan2(m_targetDirection.Y, m_targetDirection.X) -
                  Math.Atan2(forwardDirection.Y, forwardDirection.X);
      m_rotationTarget = MathHelper.ToDegrees((float)angle);
    }

    #endregion
    #region Callbacks

    private void HandleSetTarget(Message m)
    {
      var msg = (SetTargetMessage) m;

      m_target = msg.Target;
      if (m_target == null)
      {
        m_rotationTarget = 0f;
      }
      else
      {
        m_enabled = true;
      }
    }

    private void HandleShootingStateChange(Message m)
    {
      var msg = (ShootingStateMessage) m;
      m_firing = msg.Shooting;
    }

    private void HandleTankKilled(Message msg)
    {
      m_target = null;
      m_enabled = false;
    }
    
    #endregion
    #region IDisposable Implementation

    private bool m_disposed = false;

    protected override void Dispose(bool disposing)
    {
      if (!Initialized || m_disposed)
      {
        return;
      }

      Parent.RemoveListener<SetTargetMessage>(HandleSetTarget);
      Parent.RemoveListener<ShootingStateMessage>(HandleShootingStateChange);

      base.Dispose(disposing);
      m_disposed = true;
    }

    #endregion
  }
}
