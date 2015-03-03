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
    /// <param name="id"></param>
    protected EntityEvent(uint id)
    {
      Id = id;
    }

    /// <summary>
    /// The entity id tied to this event.
    /// </summary>
    public uint Id { get; private set; }
  }

  /// <summary>
  /// Signals that a new entity was added to the game.  The entity is valid 
  /// for retrieval when this event fires.
  /// </summary>
  sealed class EntityAdded
    : EntityEvent
  {
    /// <summary>
    /// Create the event
    /// </summary>
    /// <param name="id"></param>
    public EntityAdded(uint id) 
      : base(id)
    {
    }
  }

  /// <summary>
  /// Signals that an entity is queued for removal in the next frame.  The 
  /// entity is valid for retrieval when this event fires.
  /// </summary>
  sealed class EntityRemoved
    : EntityEvent
  {
    /// <summary>
    /// Create the event
    /// </summary>
    /// <param name="id"></param>
    public EntityRemoved(uint id) 
      : base(id)
    {
    }
  }

  /// <summary>
  /// Requests that the entity manager remove an entity.
  /// </summary>
  sealed class RequestEntityRemoval
    : EntityEvent
  {
    /// <summary>
    /// Create the event
    /// </summary>
    /// <param name="id"></param>
    public RequestEntityRemoval(uint id) 
      : base(id)
    {
    }
  }
}