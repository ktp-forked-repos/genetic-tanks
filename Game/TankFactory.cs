using System;
using System.Reflection;
using GeneticTanks.Game.Components;
using GeneticTanks.Game.Components.Tank;
using log4net;
using Microsoft.Xna.Framework;

namespace GeneticTanks.Game
{
  class TankFactory
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    public EntityManager EntityManager { get; set; }
    public PhysicsManager PhysicsManager { get; set; }
    public EventManager EventManager { get; set; }

    public Entity CreateControlledTestTank(Vector2 position, float rotation)
    {
      var entity = CreateTankBase();

      var state = new TankStateComponent(entity)
      {
        Dimensions = new Vector2(10, 6),
        TrackWidth = 1,
        TurretWidth = 4,
        BarrelDimensions = new Vector2(4, 0.5f),
        HealthPercent = 1,
        MaxSpeed = 30,
        MaxRotationRate = 90,
        SensorRadius = 100
      };
      entity.AddComponent(state);
      entity.AddComponent(
        new TankKeyboardControlComponent(entity, EventManager));

      if (!entity.Initialize())
      {
        Log.ErrorFormat("Could not initialize {0}, discarding", entity.Name);
        entity.Dispose();
        return null;
      }

      entity.Transform.Position = position;
      entity.Transform.Rotation = rotation;
      EntityManager.AddEntity(entity);
      return entity;
    }

    public Entity CreateTestTank(Vector2 position, float rotation)
    {
      var entity = CreateTankBase();

      var state = new TankStateComponent(entity)
      {
        Dimensions = new Vector2(10, 6),
        TrackWidth = 1,
        TurretWidth = 4,
        BarrelDimensions = new Vector2(4, 0.5f),
        HealthPercent = 1,
        MaxSpeed = 20,
        MaxRotationRate = 90,
        SensorRadius = 100
      };
      entity.AddComponent(state);
      entity.AddComponent(new TankAiComponent(entity, EntityManager, 
        EventManager));
      

      if (!entity.Initialize())
      {
        Log.ErrorFormat("Could not initialize {0}, discarding", entity.Name);
        entity.Dispose();
        return null;
      }

      entity.Transform.Position = position;
      entity.Transform.Rotation = rotation;
      EntityManager.AddEntity(entity);
      return entity;
    }

    #region Private Methods

    // Creates and returns a common tank object that includes 
    // MessageComponent, TankRenderComponent, and TankPhysicsTransformComponent.  
    // Entity is NOT initialized.
    private Entity CreateTankBase()
    {
      var id = EntityManager.NextId;
      var name = string.Format("Tank {0}", id);
      var entity = new Entity(id, name);

      entity.AddComponent(new MessageComponent(entity));
      entity.AddComponent(new TankRenderComponent(entity));
      entity.AddComponent(
        new TankPhysicsTransformComponent(entity, PhysicsManager));
      entity.AddComponent(new SensorComponent(entity, PhysicsManager));

      return entity;
    }

    #endregion
  }
}
