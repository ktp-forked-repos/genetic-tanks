using Microsoft.Xna.Framework;

namespace GeneticTanks.Game.Components
{
  /// <summary>
  /// Is a data container for various information about the tank state.
  /// </summary>
  sealed class TankStateComponent
    : Component
  {
    #region Private Fields
    // the rotation state of the tank turret
    private float m_turretRotation = 0f;
    #endregion

    public TankStateComponent(Entity parent) 
      : base(parent)
    {
      NeedsUpdate = false;
      Initialized = true;
    }

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
    /// The rotation of the turret relative to the body.  0 degrees is facing 
    /// forward, positive rotation is counter clockwise.
    /// </summary>
    public float TurretRotation
    {
      get { return m_turretRotation; }
      set
      {
        m_turretRotation = value;
        if (m_turretRotation >= 360f)
        {
          m_turretRotation -= 360f;
        }
        else if (m_turretRotation < 0f)
        {
          m_turretRotation += 360f;
        }
      }
    }

    /// <summary>
    /// The tank's health as a percent.
    /// TODO: tie me to actual health values
    /// </summary>
    public float HealthPercent { get; set; }

    #region Component Implementation

    public override bool Initialize()
    {
      return true;
    }

    public override void Update(float deltaTime)
    {
    }

    #endregion
  }
}
