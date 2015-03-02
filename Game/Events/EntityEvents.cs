using System;

namespace GeneticTanks.Game.Events
{
  /// <summary>
  /// The base class for all events involving entities.
  /// </summary>
  abstract class EntityEvent
    : Event
  {
    /// <summary>
    /// Create an entity event.
    /// </summary>
    /// <param name="e"></param>
    /// <exception cref="ArgumentNullException">e is null</exception>
    protected EntityEvent(Entity e)
    {
      if (e == null)
      {
        throw new ArgumentNullException("e");
      }
      Entity = e;
    }

    /// <summary>
    /// The entity tied to this event.
    /// </summary>
    public Entity Entity { get; private set; }
  }

  /// <summary>
  /// Signals that a new entity was added to the game.
  /// </summary>
  sealed class EntityAdded
    : EntityEvent
  {
    /// <summary>
    /// Create the event
    /// </summary>
    /// <param name="e"></param>
    /// <exception cref="ArgumentNullException">e is null</exception>
    public EntityAdded(Entity e) 
      : base(e)
    {
    }
  }

  /// <summary>
  /// Signals that an entity is queued for removal in the next frame.
  /// </summary>
  sealed class EntityRemoved
    : EntityEvent
  {
    /// <summary>
    /// Create the event
    /// </summary>
    /// <param name="e"></param>
    /// <exception cref="ArgumentNullException">e is null</exception>
    public EntityRemoved(Entity e) 
      : base(e)
    {
    }
  }
}