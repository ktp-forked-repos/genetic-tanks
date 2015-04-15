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

  sealed class ShootingStateMessage
    : Message
  {
    public ShootingStateMessage(bool shooting)
    {
      Shooting = shooting;
    }

    public bool Shooting { get; private set; }
  }
}
