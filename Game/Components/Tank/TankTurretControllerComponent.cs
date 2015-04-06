using System;
using System.Reflection;
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

    #region Private Fields
    private MessageComponent m_messenger;
    private PhysicsTransformComponent m_physics;
    private TankStateComponent m_state;

    private float m_timeSinceLastUpdate = 0f;
    
    private bool m_enabled = false;
    private float m_rotationTarget = 0f;
    private Entity m_target = null;
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
      if (!RetrieveSibling(out m_messenger))
      {
        return false;
      }
      if (!RetrieveBaseSibling(out m_physics))
      {
        return false;
      }
      if (!RetrieveSibling(out m_state))
      {
        return false;
      }

      m_messenger.AddListener<SetTargetMessage>(HandleSetTarget);

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
    }

    #endregion
    #region Private Methods

    private void UpdateRotation(float deltaTime)
    {
      if (m_target != null)
      {
        UpdateRotationTarget();
      }

      var remaining = m_rotationTarget - m_state.TurretRotation;
      if (Math.Abs(remaining) > 1e-4f)
      {
        if (remaining <= -180f)
        {
          remaining += 360f;
        }
        else if (remaining >= 180f)
        {
          remaining -= 360f;
        }

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
      var targetDirection =
        m_target.Transform.Position - Parent.Transform.Position;
      var angle = Math.Atan2(targetDirection.Y, targetDirection.X) -
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
        Log.DebugFormat("{0} cleared target", Parent.FullName);
        m_rotationTarget = 0f;
      }
      else
      {
        m_enabled = true;
        Log.DebugFormat("{0} set target {1}", Parent.FullName, m_target.FullName);
      }
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

      m_messenger.RemoveListener<SetTargetMessage>(HandleSetTarget);

      base.Dispose(disposing);
      m_disposed = true;
    }

    #endregion
  }
}
