using System;

namespace GeneticTanks.Game.Events
{
  /// <summary>
  /// The base class for all events involving entities directly.
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
  /// The base class for all events that reference entities by id.
  /// </summary>
  abstract class EntityIdEvent
    : Event
  {
    /// <summary>
    /// Create the event.
    /// </summary>
    /// <param name="id"></param>
    protected EntityIdEvent(uint id)
    {
      Id = id;
    }

    /// <summary>
    /// The id of the entity referenced by the event.
    /// </summary>
    public uint Id { get; private set; }
  }

  /// <summary>
  /// Signals that a new entity was added to the game.  The entity is valid 
  /// for retrieval when this event fires.
  /// </summary>
  sealed class EntityAddedEvent
    : EntityEvent
  {
    /// <summary>
    /// Create the event
    /// </summary>
    /// <param name="e"></param>
    /// <exception cref="ArgumentNullException">e is null</exception>
    public EntityAddedEvent(Entity e) 
      : base(e)
    {
    }
  }

  /// <summary>
  /// Signals that an entity is queued for removal in the next frame.  The 
  /// entity is valid for retrieval when this event fires.
  /// </summary>
  sealed class EntityRemovedEvent
    : EntityEvent
  {
    /// <summary>
    /// Create the event
    /// </summary>
    /// <param name="e"></param>
    /// <exception cref="ArgumentNullException">e is null</exception>
    public EntityRemovedEvent(Entity e) 
      : base(e)
    {
    }
  }

  /// <summary>
  /// Requests that the entity manager remove an entity.
  /// </summary>
  sealed class RequestEntityRemovalEvent
    : EntityIdEvent
  {
    /// <summary>
    /// Create the event
    /// </summary>
    /// <param name="id"></param>
    public RequestEntityRemovalEvent(uint id) 
      : base(id)
    {
    }
  }

  sealed class TankKilledEvent
      : EntityIdEvent
  {
    public TankKilledEvent(uint id, uint killer)
      : base(id)
    {
      Killer = killer;
    }

    public uint Killer { get; private set; }
  }
}