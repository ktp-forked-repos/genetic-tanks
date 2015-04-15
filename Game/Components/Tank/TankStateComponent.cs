using System;
using System.Reflection;
using log4net;
using log4net.Repository.Hierarchy;
using Microsoft.Xna.Framework;

namespace GeneticTanks.Game.Components.Tank
{
  /// <summary>
  /// Is a data container for various information about the tank state.
  /// </summary>
  sealed class TankStateComponent
    : Component
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    #region Private Fields
    private float m_turretRotation = 0f;
    private float m_leftTurretLimit = 0f;
    private float m_rightTurretLimit = 360f;
    private float m_health;
    #endregion

    public TankStateComponent(Entity parent) 
      : base(parent)
    {
      NeedsUpdate = false;
      Initialized = true;
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
    

    #region Component Implementation

    public override bool Initialize()
    {
      Health = MaxHealth;

      if (TurretRangeOfMotion < 360f)
      {
        m_leftTurretLimit = TurretRangeOfMotion / 2f;
        m_rightTurretLimit = 360f - m_leftTurretLimit;
      }

      Log.DebugFormat("left limit {0}", m_leftTurretLimit);
      Log.DebugFormat("right limit {0}", m_rightTurretLimit);

      return true;
    }

    public override void Update(float deltaTime)
    {
    }

    #endregion
  }
}
