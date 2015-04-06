namespace GeneticTanks.Game.Components.Messages
{
  class SetTargetMessage
    : Message
  {
    public SetTargetMessage(Entity target)
    {
      Target = target;
    }

    public Entity Target { get; private set; }
  }

}
