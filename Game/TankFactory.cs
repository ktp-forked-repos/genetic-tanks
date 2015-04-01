using System;
using System.Reflection;
using GeneticTanks.Game.Components;
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

    public Entity CreateControlledTestTank(Vector2 position)
    {
      var id = EntityManager.NextId;
      var name = string.Format("Tank {0}", id);
      var entity = new Entity(id, name);

      entity.AddComponent(
        new TransformComponent(entity){ Position = position });

      var state = new TankStateComponent(entity)
      {
        Dimensions = new Vector2(10, 6),
        TrackWidth = 1,
        TurretWidth = 4,
        BarrelDimensions = new Vector2(4, 0.5f),
        HealthPercent = 1,
        MaxSpeed = 10,
        MaxRotationRate = 90,
        SensorRadius = 100
      };
      entity.AddComponent(state);
      entity.AddComponent(new TankRenderComponent(entity));
      entity.AddComponent(new TankPhysicsComponent(entity, PhysicsManager));
      entity.AddComponent(
        new TankKeyboardControlComponent(entity, EventManager));

      if (!entity.Initialize())
      {
        Log.ErrorFormat("Could not initialize {0}, discarding", name);
        entity.Dispose();
        return null;
      }

      EntityManager.AddEntity(entity);
      return entity;
    }

    public Entity CreateTestTank(Vector2 position)
    {
      var id = EntityManager.NextId;
      var name = string.Format("Tank {0}", id);
      var entity = new Entity(id, name);

      entity.AddComponent(
        new TransformComponent(entity) { Position = position });

      var state = new TankStateComponent(entity)
      {
        Dimensions = new Vector2(10, 6),
        TrackWidth = 1,
        TurretWidth = 4,
        BarrelDimensions = new Vector2(4, 0.5f),
        HealthPercent = 1,
        MaxSpeed = 10,
        MaxRotationRate = 90,
        SensorRadius = 100
      };
      entity.AddComponent(state);
      entity.AddComponent(new TankRenderComponent(entity));
      entity.AddComponent(new TankPhysicsComponent(entity, PhysicsManager));

      if (!entity.Initialize())
      {
        Log.ErrorFormat("Could not initialize {0}, discarding", name);
        entity.Dispose();
        return null;
      }

      EntityManager.AddEntity(entity);
      return entity;
    }
  }
}
