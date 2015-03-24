using System;
using System.Reflection;
using GeneticTanks.Game.Components;
using log4net;
using log4net.Repository.Hierarchy;
using Microsoft.Xna.Framework;

namespace GeneticTanks.Game
{
  class TankFactory
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    private readonly EntityManager m_entityManager;

    public TankFactory(EntityManager em)
    {
      if (em == null)
      {
        throw new ArgumentNullException("em");
      }

      m_entityManager = em;
    }

    public Entity NewTestTank()
    {
      var id = EntityManager.NextId;
      var name = string.Format("Tank {0}", id);
      var entity = new Entity(id, name);

      entity.AddComponent(new TransformComponent(entity));

      var state = new TankStateComponent(entity)
      {
        Dimensions = new Vector2(10, 6),
        TrackWidth = 1,
        TurretWidth = 4,
        BarrelDimensions = new Vector2(4, 0.5f),
        HealthPercent = 1
      };
      entity.AddComponent(state);
      entity.AddComponent(new TankRenderComponent(entity));

      if (!entity.Initialize())
      {
        Log.ErrorFormat("Could not initialize {0}, discarding", name);
        entity.Dispose();
        return null;
      }

      m_entityManager.AddEntity(entity);
      return entity;
    }
  }
}
