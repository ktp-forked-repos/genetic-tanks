using System;
using FarseerPhysics.Dynamics;

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
}
