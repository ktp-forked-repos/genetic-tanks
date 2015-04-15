namespace GeneticTanks.Game.Components.Messages
{
  sealed class ShotFiredMessage
    : Message
  {
    public ShotFiredMessage(uint bullet)
    {
      Bullet = bullet;
    }

    /// <summary>
    /// The entity id of the bullet.
    /// </summary>
    public uint Bullet { get; private set; }
  }

  /// <summary>
  /// Changes the firing state of the tank.
  /// </summary>
  sealed class ShootingStateMessage
    : Message
  {
    public ShootingStateMessage(bool shooting)
    {
      Shooting = shooting;
    }

    public bool Shooting { get; private set; }
  }

  /// <summary>
  /// Broadcasts that the tank was hit by a shot.
  /// </summary>
  sealed class TankHitMessage
    : Message
  {
    public TankHitMessage(uint shooter, float damage)
    {
      Shooter = shooter;
      Damage = damage;
    }

    /// <summary>
    /// The tank that fired the shot.
    /// </summary>
    public uint Shooter { get; private set; }

    /// <summary>
    /// Damage done by the shot.
    /// </summary>
    public float Damage { get; private set; }
  }

  sealed class TankKilledMessage
      : Message
  {
    public TankKilledMessage(uint killer)
    {
      Killer = killer;
    }

    public uint Killer { get; private set; }
  }
}
