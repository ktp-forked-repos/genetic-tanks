using System;
using System.Reflection;
using GeneticTanks.Game.Components.Messages;
using GeneticTanks.Game.Events;
using GeneticTanks.Game.Managers;
using log4net;
using Microsoft.Xna.Framework;

namespace GeneticTanks.Game.Components.Tank
{
  /// <summary>
  /// Is a data container for various information about the tank state, and 
  /// updates the health when the tank is hit.
  /// </summary>
  sealed class TankStateComponent
    : Component
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    #region Private Fields
    private readonly EventManager m_eventManager;
    private MessageComponent m_messenger;

    private float m_turretRotation = 0f;
    private float m_leftTurretLimit = 0f;
    private float m_rightTurretLimit = 360f;
    private float m_health;
    #endregion

    public TankStateComponent(Entity parent, EventManager eventManager) 
      : base(parent)
    {
      if (eventManager == null)
      {
        throw new ArgumentNullException("eventManager");
      }
      m_eventManager = eventManager;

      NeedsUpdate = false;
    }

    // stuff that doesn't change
    #region Base Definition

    /// <summary>
    /// The length and width of the tank body, in meters.
    /// </summary>
    public Vector2 Dimensions { get; set; }

    /// <summary>
    /// The width of the tank tracks, in meters.
    /// </summary>
    public float TrackWidth { get; set; }

    /// <summary>
    /// The diameter of the turret, in meters.
    /// </summary>
    public float TurretWidth { get; set; }

    /// <summary>
    /// The length and width of the gun barrel, in meters.
    /// </summary>
    public Vector2 BarrelDimensions { get; set; }

    /// <summary>
    /// The arc the turret can traverse through, in degrees.  Centered facing 
    /// forward.
    /// </summary>
    public float TurretRangeOfMotion { get; set; }

    /// <summary>
    /// The radius of the tank's sensor, in meters.
    /// </summary>
    public float SensorRadius { get; set; }

    /// <summary>
    /// The gun range in meters.
    /// </summary>
    public float GunRange { get; set; }

    /// <summary>
    /// The velocity of shells in m/s.
    /// </summary>
    public float GunSpeed { get; set; }

    /// <summary>
    /// The gun's damage per shot.
    /// </summary>
    public float GunDamage { get; set; }

    /// <summary>
    /// The gun's reload time in seconds.
    /// </summary>
    public float ReloadTime { get; set; }

    /// <summary>
    /// The maximum tank health.
    /// </summary>
    public float MaxHealth { get; set; }

    /// <summary>
    /// The maximum speed of the tank in m/s.
    /// </summary>
    public float MaxSpeed { get; set; }

    /// <summary>
    /// The maximum speed that the tank can rotate in degrees/s.
    /// </summary>
    public float MaxTurnSpeed { get; set; }

    /// <summary>
    /// The maximum speed that the turret can rotate in degrees/s.
    /// </summary>
    public float MaxTurretRotationRate { get; set; }

    #endregion

    #region Dynamic State
    /// <summary>
    /// The rotation of the turret relative to the body.  0 degrees is facing 
    /// forward, positive rotation is counter clockwise.
    /// </summary>
    public float TurretRotation
    {
      get { return m_turretRotation; }
      set
      {
        m_turretRotation = value % 360f;
        while (m_turretRotation < 0)
        {
          m_turretRotation += 360f;
        }

        if (TurretRangeOfMotion < 360f)
        {
          if (m_turretRotation > m_leftTurretLimit &&
              m_turretRotation < m_rightTurretLimit)
          {
            m_turretRotation = m_turretRotation < 180f
              ? m_leftTurretLimit
              : m_rightTurretLimit;
          }
        }
      }
    }

    /// <summary>
    /// The current health of the tank.
    /// </summary>
    public float Health
    {
      get { return m_health; }
      set
      {
        value = Math.Max(value, 0f);
        value = Math.Min(value, MaxHealth);
        m_health = value;
      }
    }

    /// <summary>
    /// The tank's health as a percent.
    /// </summary>
    public float HealthPercent
    {
      get { return Health / MaxHealth; }
    }

    #endregion

    #region Component Implementation

    public override bool Initialize()
    {
      if (!RetrieveSibling(out m_messenger))
      {
        return false;
      }

      m_eventManager.AddListener<TankHitEvent>(HandleTankHit);

      Health = MaxHealth;

      if (TurretRangeOfMotion < 360f)
      {
        m_leftTurretLimit = TurretRangeOfMotion / 2f;
        m_rightTurretLimit = 360f - m_leftTurretLimit;
      }

      Initialized = true;
      return true;
    }
    
    public override void Update(float deltaTime)
    {
    }

    #endregion

    #region Callbacks

    private void HandleTankHit(Event e)
    {
      var evt = (TankHitEvent) e;
      if (evt.Target != Parent.Id)
      {
        return;
      }

      m_messenger.QueueMessage(new TankHitMessage(evt.Shooter, evt.Damage));
      m_health -= evt.Damage;
      m_health = Math.Max(m_health, 0f);

      if (HealthPercent <= 0f)
      {
        m_messenger.QueueMessage(new TankKilledMessage(evt.Shooter));
        m_eventManager.QueueEvent(new TankKilledEvent(Parent.Id, evt.Shooter));
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

      m_eventManager.RemoveListener<TankHitEvent>(HandleTankHit);

      base.Dispose(disposing);
      m_disposed = true;
    }

    #endregion
  }
}
