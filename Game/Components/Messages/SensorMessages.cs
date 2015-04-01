using System;

namespace GeneticTanks.Game.Components.Messages
{
  /// <summary>
  /// The base for all sensor related messages.
  /// </summary>
  abstract class SensorMessage
    : Message
  {
    protected SensorMessage(int id)
    {
      if (id == Entity.InvalidId)
      {
        throw new ArgumentOutOfRangeException("id");
      }

      ContactId = id;
    }

    /// <summary>
    /// The id of the target entity involved in the message.
    /// </summary>
    public int ContactId { get; private set; }
  }

  /// <summary>
  /// Signals that the sensor has contact with a new entity.
  /// </summary>
  sealed class SensorNewContactMessage
    : SensorMessage
  {
    public SensorNewContactMessage(int id) 
      : base(id)
    {
    }
  }

  /// <summary>
  /// Signals that the sensor has lost contact with an entity.
  /// </summary>
  sealed class SensorLostContactMessage
    : SensorMessage
  {
    public SensorLostContactMessage(int id)
      : base(id)
    {
    }
  }
}
