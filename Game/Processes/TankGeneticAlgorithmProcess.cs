using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using FarseerPhysics.Collision;
using GeneticTanks.Extensions;
using GeneticTanks.Game.Components.Tank;
using GeneticTanks.Game.Events;
using GeneticTanks.Game.Managers;
using GeneticTanks.GeneticAlgorithm;
using GeneticTanks.Properties;
using log4net;
using Microsoft.Xna.Framework;

namespace GeneticTanks.Game.Processes
{
  sealed class TankGeneticAlgorithmProcess
    : Process
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    private static readonly int Size = 
      Settings.Default.PopulationSize;
    private const float DeathRemovalDelay = 5f;
    private const float LastHitTimeout = 10f;
    private static readonly Random Random = new Random();

    #region Private Fields

    private readonly Arena m_arena;
    private readonly EventManager m_eventManager;
    private readonly PhysicsManager m_physicsManager;

    private TankPopulation m_population;
    private bool m_doNextGeneration = false;
    private float m_survivalTime;
    private float m_lastHitTime;
    // maps game entities back to genomes
    private readonly Dictionary<uint, int> m_entityMapping = 
      new Dictionary<uint, int>();
    private readonly Dictionary<uint, float> m_deadList = 
      new Dictionary<uint, float>();
    
    #endregion

    public TankGeneticAlgorithmProcess(Arena arena, EventManager eventManager,
      PhysicsManager physicsManager)
    {
      if (arena == null)
      {
        throw new ArgumentNullException("arena");
      }
      if (eventManager == null)
      {
        throw new ArgumentNullException("eventManager");
      }
      if (physicsManager == null)
      {
        throw new ArgumentNullException("physicsManager");
      }

      m_arena = arena;
      m_eventManager = eventManager;
      m_physicsManager = physicsManager;
    }

    #region Process Implementation

    protected override bool OnInitialize()
    {
      m_eventManager.AddListener<TankHitEvent>(HandleTankHit);
      m_eventManager.AddListener<TankKilledEvent>(HandleTankKilled);
      m_eventManager.AddListener<EntityRemovedEvent>(HandleEntityRemoved);

      m_population = new TankPopulation(Size);
      CreateEntities();

      return true;
    }

    protected override void OnUpdate(float deltaTime)
    {
      if (m_doNextGeneration)
      {
        m_doNextGeneration = false;
        m_population.NextGeneration();
        CreateEntities();
        return;
      }

      m_survivalTime += deltaTime;
      m_lastHitTime += deltaTime;

      foreach (var id in m_deadList.Keys.ToList())
      {
        m_deadList[id] += deltaTime;
        if (m_deadList[id] >= DeathRemovalDelay)
        {
          Log.DebugFmt("Clearing dead entity {0}", id);
          m_eventManager.QueueEvent(new RequestEntityRemovalEvent(id));
        }
      }

      if (m_lastHitTime >= LastHitTimeout &&
          m_entityMapping.Count <= TankPopulation.NumClones)
      {
        foreach (var id in m_entityMapping.Keys)
        {
          m_eventManager.QueueEvent(new RequestEntityRemovalEvent(id));
        }

        m_doNextGeneration = true;
      }
    }

    protected override void OnSucceed()
    {
      ClearEventListeners();
    }

    protected override void OnAbort()
    {
      ClearEventListeners();
    }

    protected override void OnFail()
    {
      ClearEventListeners();
    }

    #endregion

    #region Private Methods

    private void CreateEntities()
    {
      m_entityMapping.Clear();
      m_deadList.Clear();

      var entities = new List<Entity>();
      foreach (var genome in m_population.Genomes)
      {
        var tank = TankFactory.CreateFromGenome(genome);
        if (tank == null)
        {
          Log.ErrorFmt(
            "Failed to create entity for generation {0} genome {1}",
            m_population.Generation, genome.Id);
          continue;
        }

        m_entityMapping[tank.Id] = genome.Id;
        entities.Add(tank);
      }

      m_survivalTime = 0f;
      m_lastHitTime = -5f;
      PositionEntities(entities);
    }

    private void PositionEntities(IEnumerable<Entity> entities)
    {
      var xLimit = (m_arena.Dimensions.X - 50f) / 2f;
      var yLimit = (m_arena.Dimensions.Y - 50f) / 2f;

      foreach (var entity in entities)
      {
        var state = entity.GetComponent<TankStateComponent>();
        Debug.Assert(state != null);
        var size = state.Dimensions;
        
        var position = Vector2.Zero;
        var placed = false;
        while (!placed)
        {
          var x = (float)Random.NextDouble() * xLimit * 
            (Random.NextDouble() < 0.5 ? -1 : 1);
          var y = (float)Random.NextDouble() * yLimit * 
            (Random.NextDouble() < 0.5 ? -1 : 1);
          position = new Vector2(x, y);

          var aabb = new AABB(position, size.X * 1.5f, size.Y * 1.5f);
          placed = true;
          m_physicsManager.World.QueryAABB(fixture =>
          {
            if ((fixture.CollisionCategories & PhysicsManager.TankCategory) != 0)
            {
              placed = false;
              return false;
            }
            return true;
          }, ref aabb);
        }

        entity.Transform.Position = position;
        entity.Transform.Rotation = (float)Random.NextDouble() * 360f;
      }
    }

    private void ClearEventListeners()
    {
      m_eventManager.RemoveListener<TankHitEvent>(HandleTankHit);
      m_eventManager.RemoveListener<TankKilledEvent>(HandleTankKilled);
      m_eventManager.RemoveListener<EntityRemovedEvent>(HandleEntityRemoved);
    }

    private TankGenome LookupGenome(uint entityId)
    {
      int genomeId;
      if (!m_entityMapping.TryGetValue(entityId, out genomeId))
      {
        Log.ErrorFmt("Tried to lookup unknown genome {0}", entityId);
        return null;
      }

      TankGenome genome;
      if (!m_population.TryGetGenome(genomeId, out genome))
      {
        Log.ErrorFmt(
          "Failed to retrieve genome {0} that is mapped to entity {1}",
          genomeId, entityId);
        return null;
      }

      return genome;
    }

    #endregion

    #region Callbacks

    private void HandleTankHit(Event e)
    {
      var evt = (TankHitEvent) e;
      var genome = LookupGenome(evt.Shooter);
      if (genome == null)
      {
        return;
      }

      genome.DamageDealt += evt.Damage;
      m_lastHitTime = 0f;
    }

    private void HandleTankKilled(Event e)
    {
      var evt = (TankKilledEvent) e;
      var victim = LookupGenome(evt.Id);
      var killer = LookupGenome(evt.Killer);
      if (victim == null || killer == null)
      {
        return;
      }

      m_deadList[evt.Id] = 0f;
      victim.SurvivalTime = m_survivalTime;
      killer.NumKills++;
    }

    private void HandleEntityRemoved(Event e)
    {
      var evt = (EntityRemovedEvent) e;

      m_entityMapping.Remove(evt.Entity.Id);
      m_deadList.Remove(evt.Entity.Id);
    }

    #endregion

    #region IDisposable Implementation

    private bool m_disposed = false;

    protected override void Dispose(bool disposing)
    {
      if (!Initialized || m_disposed)
      {
        return;
      }

      ClearEventListeners();

      base.Dispose(disposing);
      m_disposed = true;
    }

    #endregion
  }
}
