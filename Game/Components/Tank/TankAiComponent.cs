using System;
using System.Collections.Generic;
using System.Reflection;
using FarseerPhysics.Dynamics;
using GeneticTanks.Game.Components.Messages;
using log4net;
using Microsoft.Xna.Framework;

namespace GeneticTanks.Game.Components.Tank
{
  sealed class TankAiComponent
    : Component
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    private const float UpdateInterval = 1f / 5f;

    private const float RaycastDistance = 25f;

    // 5 rays form a cone to sweep for obstacles in front of the tank
    private static readonly Vector2 ForwardRay =
      PhysicsTransformComponent.ForwardVector * RaycastDistance;
    private static readonly Vector2 LeftRay = 
      new Vector2(RaycastDistance,
        RaycastDistance * (float)Math.Tan(MathHelper.ToRadians(40f)));
    private static readonly Vector2 LeftHalfRay =
      new Vector2(RaycastDistance,
        RaycastDistance * (float)Math.Tan(MathHelper.ToRadians(20f)));
    private static readonly Vector2 RightRay =
      new Vector2(RaycastDistance,
        -RaycastDistance * (float)Math.Tan(MathHelper.ToRadians(40f)));
    private static readonly Vector2 RightHalfRay =
      new Vector2(RaycastDistance,
        -RaycastDistance * (float)Math.Tan(MathHelper.ToRadians(20f)));

    private static readonly Category RayCategories =
      PhysicsManager.TankCategory | PhysicsManager.TerrainCategory;

    private static readonly Random Random = new Random();

    enum AiState
    {
      SearchForward,
      SearchTurnLeft,
      SearchTurnRight,
      ApproachEnemy,
      Attack
    }

    #region Private Fields
    private readonly EntityManager m_entityManager;
    private readonly EventManager m_eventManager;

    private MessageComponent m_messenger;
    private TankPhysicsTransformComponent m_physics;
    private TankStateComponent m_state;

    private float m_timeSinceLastUpdate = 0f;

    private AiState m_aiState;

    private Vector2 m_rayOrigin;
    private float m_forwardObstacle;
    private float m_leftObstacle;
    private float m_leftHalfObstacle;
    private float m_rightObstacle;
    private float m_rightHalfObstacle;
    
    private readonly List<Entity> m_contacts = new List<Entity>();
    private Entity m_target = null;
    #endregion

    public TankAiComponent(Entity parent, EntityManager entityManager, 
      EventManager eventManager)
      : base(parent)
    {
      if (entityManager == null)
      {
        throw new ArgumentNullException("entityManager");
      }
      if (eventManager == null)
      {
        throw new ArgumentNullException("eventManager");
      }

      m_entityManager = entityManager;
      m_eventManager = eventManager;

      NeedsUpdate = true;
    }
    
    #region Component Implementation

    public override bool Initialize()
    {
      if (!RetrieveSibling(out m_messenger))
      {
        return false;
      }
      if (!RetrieveSibling(out m_physics))
      {
        return false;
      }
      if (!RetrieveSibling(out m_state))
      {
        return false;
      }

      m_rayOrigin = new Vector2(m_state.Dimensions.X, 0);
      
      m_messenger.AddListener<SensorNewContactMessage>(HandleSensorNewContact);
      m_messenger.AddListener<SensorLostContactMessage>(
        HandleSensorLostContact);

      SetState(AiState.SearchForward);

      return true;
    }
    
    public override void Update(float deltaTime)
    {
      m_timeSinceLastUpdate += deltaTime;
      if (m_timeSinceLastUpdate < UpdateInterval)
      {
        return;
      }
      m_timeSinceLastUpdate = m_timeSinceLastUpdate % UpdateInterval;

      UpdateRaycasts();
      switch (m_aiState)
      {
        case AiState.SearchForward:
          UpdateSearchForward();
          break;

        case AiState.SearchTurnLeft:
        case AiState.SearchTurnRight:
          UpdateSearchTurn();
          break;
      }
    }

    #endregion
    #region Private Methods

    private void UpdateSearchForward()
    {
      var left = m_leftHalfObstacle > 0f || m_leftObstacle > 0f;
      var center = m_forwardObstacle > 0;
      var right = m_rightHalfObstacle > 0f || m_rightObstacle > 0f;

      if (!(center || left || right))
      {
        return;
      }

      if (!center)
      {
        // in this case we don't care unless side obstacles are within 10m
        left = left && (m_leftHalfObstacle < 15f || m_leftObstacle < 15f);
        right = right && (m_rightHalfObstacle < 15f || m_rightObstacle < 15f);

        SelectTurnDirection(left, right);
      }
      else
      {
        SelectTurnDirection(left, right);
      }
    }

    private void UpdateSearchTurn()
    {
      var left = m_leftHalfObstacle > 0f || m_leftObstacle > 0f;
      var center = m_forwardObstacle > 0;
      var right = m_rightHalfObstacle > 0f || m_rightObstacle > 0f;

      if (!left && !center && !right)
      {
        SetState(AiState.SearchForward);
      }
    }

    private void SelectTurnDirection(bool leftObstacle, bool rightObstacle)
    {
      // obstacle on either side, or neither side, random direction
      if ((leftObstacle && rightObstacle) || (!leftObstacle && !rightObstacle))
      {
        var state = Random.NextDouble() < 0.5
          ? AiState.SearchTurnLeft
          : AiState.SearchTurnRight;
        SetState(state);
      }
      else if (leftObstacle)
      {
        SetState(AiState.SearchTurnRight);
      }
      else
      {
        SetState(AiState.SearchTurnLeft);
      }
    }
    
    private void UpdateRaycasts()
    {
      m_leftObstacle = 
        m_physics.RaycastDistance(m_rayOrigin, LeftRay, RayCategories);
      m_leftHalfObstacle =
        m_physics.RaycastDistance(m_rayOrigin, LeftHalfRay, RayCategories);
      m_forwardObstacle =
        m_physics.RaycastDistance(m_rayOrigin, ForwardRay, RayCategories);
      m_rightHalfObstacle =
        m_physics.RaycastDistance(m_rayOrigin, RightHalfRay, RayCategories);
      m_rightObstacle =
        m_physics.RaycastDistance(m_rayOrigin, RightRay, RayCategories);
    }

    private void SetState(AiState state)
    {
      m_aiState = state;

      switch (m_aiState)
      {
        case AiState.SearchForward:
          m_messenger.QueueMessage(new MoveMessage(MoveCommand.TurnStop));
          m_messenger.QueueMessage(
            new MoveMessage(MoveCommand.SpeedForwardFull));
          break;

        case AiState.SearchTurnLeft:
          m_messenger.QueueMessage(new MoveMessage(MoveCommand.TurnLeftFull));
          m_messenger.QueueMessage(
            new MoveMessage(MoveCommand.SpeedStop));
          break;

        case AiState.SearchTurnRight:
          m_messenger.QueueMessage(new MoveMessage(MoveCommand.TurnRightFull));
          m_messenger.QueueMessage(
            new MoveMessage(MoveCommand.SpeedStop));
          break;

        case AiState.ApproachEnemy:
          break;
        case AiState.Attack:
          break;
      }
    }

    private void SetTarget(Entity target)
    {
      m_target = target;
      m_messenger.QueueMessage(new SetTargetMessage(m_target));
    }

    #endregion
    #region Callbacks

    private void HandleSensorNewContact(Message m)
    {
      var msg = (SensorNewContactMessage) m;
      var entity = m_entityManager.GetEntity(msg.ContactId);

      if (entity == null)
      {
        return;
      }

      if (m_target == null)
      {
        SetTarget(entity);
      }
    }

    private void HandleSensorLostContact(Message m)
    {
      var msg = (SensorLostContactMessage)m;
      var entity = m_entityManager.GetEntity(msg.ContactId);

      if (entity == null)
      {
        return;
      }

      if (entity == m_target)
      {
        SetTarget(null);
      }
    }
    
    #endregion
    #region IDisposable

    private bool m_disposed = false;

    protected override void Dispose(bool disposing)
    {
      if (!Initialized || m_disposed)
      {
        return;
      }

      if (disposing)
      {
        
      }

      m_messenger.RemoveListener<SensorNewContactMessage>(
        HandleSensorNewContact);
      m_messenger.RemoveListener<SensorNewContactMessage>(
        HandleSensorLostContact);

      base.Dispose(disposing);
      m_disposed = true;
    }

    #endregion
  }
}
