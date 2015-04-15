using System.Reflection;
using log4net;
using Microsoft.Xna.Framework;

namespace GeneticTanks.Game.Components.Bullet
{
  class BulletDataComponent
    : Component
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    /// Create the component.
    /// </summary>
    /// <param name="parent"></param>
    public BulletDataComponent(Entity parent) 
      : base(parent)
    {
      Initialized = true;
      NeedsUpdate = false;
    }

    /// <summary>
    /// The bullet's radius in meters.
    /// </summary>
    public float Radius { get; set; }

    /// <summary>
    /// The bullet's velocity in m/s.
    /// </summary>
    public Vector2 Velocity { get; set; }

    /// <summary>
    /// The amount of damage that the bullet applies.
    /// </summary>
    public float Damage { get; set; }

    /// <summary>
    /// The tank that fired the bullet.
    /// </summary>
    public uint FiringEntity { get; set; }

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
