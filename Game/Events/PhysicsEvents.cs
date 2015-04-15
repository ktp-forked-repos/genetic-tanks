using System;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;

namespace GeneticTanks.Game.Events
{
  /// <summary>
  /// Signals that a new physics world has been created.  The old world is 
  /// still accessible through the physics manager, but it will be replaced as
  /// soon is this event is completed.
  /// </summary>
  sealed class NewPhysicsWorld
    : Event
  {
    public NewPhysicsWorld(World world)
    {
      if (world == null)
      {
        throw new ArgumentNullException("world");
      }

      World = world;
    }

    /// <summary>
    /// The newly created world.
    /// </summary>
    public World World { get; private set; }
  }

  /// <summary>
  /// Event signaling that one tank has shot another tank.
  /// </summary>
  sealed class TankHitEvent
    : Event
  {
    public TankHitEvent(uint shooter, uint target, float damage)
    {
      Shooter = shooter;
      Target = target;
      Damage = damage;
    }

    /// <summary>
    /// The tank that fired.
    /// </summary>
    public uint Shooter { get; private set;  }

    /// <summary>
    /// The tank that was hit.
    /// </summary>
    public uint Target { get; private set; }

    /// <summary>
    /// The amount of damage done.
    /// </summary>
    public float Damage { get; private set; }
  }

  /// <summary>
  /// Signals that a shot was fired.
  /// </summary>
  sealed class ShotFiredEvent
    : Event
  {
    public ShotFiredEvent(uint shooter, uint bullet)
    {
      Shooter = shooter;
      Bullet = bullet;
    }

    /// <summary>
    /// The tank that fired the shot.
    /// </summary>
    public uint Shooter { get; private set; }

    /// <summary>
    /// The id of the bullet.
    /// </summary>
    public uint Bullet { get; private set; }
  }
}
