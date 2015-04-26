using System;
using System.Reflection;
using GeneticTanks.Extensions;
using GeneticTanks.Game.Components;
using GeneticTanks.Game.Components.Tank;
using GeneticTanks.Game.Managers;
using GeneticTanks.GeneticAlgorithm;
using log4net;
using Microsoft.Xna.Framework;
using Attribute = GeneticTanks.GeneticAlgorithm.Attribute;

namespace GeneticTanks.Game
{
  static class TankFactory
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

//     public static Entity CreateControlledTestTank(Vector2 position, 
//       float rotation)
//     {
//       var entity = CreateTankBase();
// 
//       var state = new TankStateComponent(entity)
//       {
//         Dimensions = new Vector2(10, 6),
//         TrackWidth = 1,
//         TurretWidth = 4,
//         BarrelDimensions = new Vector2(4, 0.5f),
//         MaxHealth = 1,
//         MaxSpeed = 30,
//         MaxTurnSpeed = 90,
//         SensorRadius = 100
//       };
//       entity.AddComponent(state);
//       entity.AddComponent(
//         new TankKeyboardControlComponent(entity, EventManager));
// 
//       if (!entity.Initialize())
//       {
//         Log.ErrorFormat("Could not initialize {0}, discarding", entity.Name);
//         entity.Dispose();
//         return null;
//       }
// 
//       entity.Transform.Position = position;
//       entity.Transform.Rotation = rotation;
//       EntityManager.AddEntity(entity);
//       return entity;
//     }

    public static Entity CreateTestTank(Vector2 position, float rotation)
    {
      var entity = CreateTankBase();

      var state = new TankStateComponent(entity, Globals.EventManager)
      {
        Dimensions = new Vector2(10, 6),
        TrackWidth = 1,
        TurretWidth = 4,
        BarrelDimensions = new Vector2(4, 0.5f),
        TurretRangeOfMotion = 180,

        SensorRadius = 100,
        GunRange = 75,
        GunSpeed = 100,
        GunDamage = 1,
        ReloadTime = 2,

        MaxHealth = 20,
        MaxSpeed = 20,
        MaxTurnSpeed = 30,
        MaxTurretRotationRate = 30
      };
      entity.AddComponent(state);
      entity.AddComponent(new TankAiComponent(entity, Globals.EntityManager,
        Globals.EventManager, Globals.PhysicsManager));
      

      if (!entity.Initialize())
      {
        Log.ErrorFmt("Could not initialize {0}, discarding", entity.Name);
        entity.Dispose();
        return null;
      }

      entity.Transform.Position = position;
      entity.Transform.Rotation = rotation;
      Globals.EntityManager.AddEntity(entity);
      return entity;
    }

    public static Entity CreateFromGenome(TankGenome genome)
    {
      var tank = CreateTankBase();

      tank.AddComponent(new TankAiComponent(tank, Globals.EntityManager,
        Globals.EventManager, Globals.PhysicsManager));

      if (!tank.Initialize())
      {
        Log.ErrorFmt("Could not initialize {0}, discarding", tank.Name);
        tank.Dispose();
        return null;
      }

      Globals.EntityManager.AddEntity(tank);
      return tank;
    }

    #region Private Methods

    // Creates and returns a common tank object that includes 
    // MessageComponent, TankRenderComponent, and TankPhysicsTransformComponent.  
    // Entity is NOT initialized.
    private static Entity CreateTankBase()
    {
      var id = EntityManager.NextId;
      var entity = new Entity(id, "tank");

      entity.AddComponent(new MessageComponent(entity));
      entity.AddComponent(new TankRenderComponent(entity));
      entity.AddComponent(
        new TankPhysicsTransformComponent(entity, Globals.PhysicsManager));
      entity.AddComponent(new TankSensorComponent(entity, Globals.PhysicsManager));
      entity.AddComponent(new TankTurretControllerComponent(entity));

      return entity;
    }

    private static void BuildTankState(Entity tank, 
      TankGenome genome)
    {
      var state = new TankStateComponent(tank, Globals.EventManager)
      {
        TrackWidth = 1f
      };

      foreach (var a in TankGenome.Attributes)
      {
        var percent = (float)genome.GetAttribute(a) / 
          TankGenome.MaxAttributeValue;
        float min;
        float max;
        float value;

        switch (a)
        {
          case Attribute.Health:
            min = Properties.Settings.Default.TankMinHealth;
            max = Properties.Settings.Default.TankMaxHealth;
            value = ((max - min) * percent) + min;
            state.MaxHealth = value;
            break;

          // size is backwards, more points means a smaller size
          case Attribute.Size:
            percent = 1f - (percent / 2f);
            var length = percent * Properties.Settings.Default.TankBaseLength;
            var width = percent * Properties.Settings.Default.TankBaseWidth;
            state.Dimensions = new Vector2(length, width);
            state.TurretWidth = width * 0.75f;
            break;

          case Attribute.Speed:
            min = Properties.Settings.Default.TankMinSpeed;
            max = Properties.Settings.Default.TankMaxSpeed;
            value = ((max - min) * percent) + min;
            state.MaxSpeed = value;
            break;

          case Attribute.TurnSpeed:
            min = Properties.Settings.Default.TankMinTurnSpeed;
            max = Properties.Settings.Default.TankMaxTurnSpeed;
            value = ((max - min) * percent) + min;
            state.MaxTurnSpeed = value;
            break;

          case Attribute.TurretRotationSpeed:
            min = Properties.Settings.Default.TankMinTurretSpeed;
            max = Properties.Settings.Default.TankMaxTurretSpeed;
            value = ((max - min) * percent) + min;
            state.MaxTurretRotationRate = value;
            break;

//           case Attribute.TurretRangeOfMotion:
//             value = ((max - min) * percent) + min;
//             break;
// 
//           case Attribute.SensorRange:
//             value = ((max - min) * percent) + min;
//             break;
//             
//           case Attribute.GunRange:
//             value = ((max - min) * percent) + min;
//             break;
// 
//           case Attribute.GunDamage:
//             value = ((max - min) * percent) + min;
//             break;
// 
//           case Attribute.GunSpeed:
//             value = ((max - min) * percent) + min;
//             break;
// 
//           case Attribute.GunReloadTime:
//             value = ((max - min) * percent) + min;
//             break;
        }

        tank.AddComponent(state);
      }
    }

    #endregion
  }
}
