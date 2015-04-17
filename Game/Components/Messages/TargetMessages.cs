namespace GeneticTanks.Game.Components.Messages
{
  /// <summary>
  /// Commands the tank to change its target.
  /// </summary>
  class SetTargetMessage
    : Message
  {
    public SetTargetMessage(Entity target)
    {
      Target = target;
    }

    /// <summary>
    /// The new target.  If null, the target should be cleared.
    /// </summary>
    public Entity Target { get; private set; }
  }

}
