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

    #region Constants
    // update rate for the main ai
    private const float UpdateInterval = 1f / 4f;
    // update rate for collision checks
    private const float CollisionUpdateInterval = 1f / 5f;

    // distance for collision ray casts
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

    // collision categories used in ray casting
    private static readonly Category RayCategories =
      PhysicsManager.TankCategory | PhysicsManager.TerrainCategory;
    #endregion

    private static readonly Random Random = new Random();

    enum AiState
    {
      Search,
      ApproachEnemy,
      Attack
    }

    enum MoveState
    {
      Stopped,
      Forward,
      TurnLeft,
      TurnRight,
      TurnLeftCollision,
      TurnRightCollision
    }

    #region Private Fields
    private readonly EntityManager m_entityManager;
    private readonly EventManager m_eventManager;
    private readonly PhysicsManager m_physicsManager;

    private MessageComponent m_messenger;
    private TankPhysicsTransformComponent m_physics;
    private TankStateComponent m_state;

    private float m_timeSinceLastUpdate = 0f;
    private float m_collisionTime = 0f;

    private AiState m_aiState;
    private MoveState m_moveState;

    private Vector2 m_rayOrigin;
    private bool m_centerObstacle;
    private bool m_leftObstacle;
    private bool m_rightObstacle;
    
    private readonly List<Entity> m_contacts = new List<Entity>();
    private Entity m_target = null;
    #endregion

    /// <summary>
    /// Create the component.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="entityManager"></param>
    /// <param name="eventManager"></param>
    /// <param name="physicsManager"></param>
    public TankAiComponent(Entity parent, EntityManager entityManager, 
      EventManager eventManager, PhysicsManager physicsManager)
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
      if (physicsManager == null)
      {
        throw new ArgumentNullException("physicsManager");
      }

      m_entityManager = entityManager;
      m_eventManager = eventManager;
      m_physicsManager = physicsManager;

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

      m_physicsManager.PostStep += HandlePostStep;

      SetState(AiState.Search);
      return true;
    }
    
    public override void Update(float deltaTime)
    {
      m_timeSinceLastUpdate += deltaTime;
      if (m_timeSinceLastUpdate < UpdateInterval)
      {
        return;
      }
      m_timeSinceLastUpdate %= UpdateInterval;

      switch (m_aiState)
      {
        case AiState.ApproachEnemy:
          break;

        case AiState.Attack:
          break;
      }
    }

    #endregion
    #region Private Methods

    private void UpdateMovement()
    {
      switch (m_moveState)
      {
        case MoveState.Forward:
          UpdateMoveForward();
          break;

        case MoveState.TurnLeftCollision:
        case MoveState.TurnRightCollision:
          UpdateMoveTurn();
          break;
      }
    }

    private void UpdateMoveForward()
    {
      if (!(m_leftObstacle || m_centerObstacle || m_rightObstacle))
      {
        return;
      }

      SelectTurnDirection();
    }

    private void UpdateMoveTurn()
    {
      if (!m_leftObstacle && !m_centerObstacle && !m_rightObstacle)
      {
        SetMoveState(MoveState.Forward);
      }
    }

    private void SelectTurnDirection()
    {
      // obstacle on either side, or neither side, random direction
      if ((m_leftObstacle && m_rightObstacle) || 
          (!m_leftObstacle && !m_rightObstacle))
      {
        var state = Random.NextDouble() < 0.5
          ? MoveState.TurnLeftCollision
          : MoveState.TurnRightCollision;
        SetMoveState(state);
      }
      else if (m_leftObstacle)
      {
        SetMoveState(MoveState.TurnRightCollision);
      }
      else
      {
        SetMoveState(MoveState.TurnLeftCollision);
      }
    }
    
    private void DoRaycasts()
    {
      var left = 
        m_physics.RaycastDistance(m_rayOrigin, LeftRay, RayCategories);
      var leftHalf =
        m_physics.RaycastDistance(m_rayOrigin, LeftHalfRay, RayCategories);
      var center =
        m_physics.RaycastDistance(m_rayOrigin, ForwardRay, RayCategories);
      var rightHalf =
        m_physics.RaycastDistance(m_rayOrigin, RightHalfRay, RayCategories);
      var right =
        m_physics.RaycastDistance(m_rayOrigin, RightRay, RayCategories);

      m_leftObstacle = left > 0f || leftHalf > 0f;
      m_centerObstacle = center > 0f;
      m_rightObstacle = right > 0f || rightHalf > 0f;
    }

    private void SetMoveState(MoveState state)
    {
      m_moveState = state;

      switch (m_moveState)
      {
        case MoveState.Stopped:
          m_messenger.QueueMessage(new MoveMessage(MoveCommand.AllStop));
          break;

        case MoveState.Forward:
          m_messenger.QueueMessage(new MoveMessage(MoveCommand.TurnStop));
          m_messenger.QueueMessage(
            new MoveMessage(MoveCommand.SpeedForwardFull));
          break;

        case MoveState.TurnLeft:
        case MoveState.TurnLeftCollision:
          m_messenger.QueueMessage(new MoveMessage(MoveCommand.TurnLeftFull));
          m_messenger.QueueMessage(
            new MoveMessage(MoveCommand.SpeedStop));
          break;

        case MoveState.TurnRight:
        case MoveState.TurnRightCollision:
          m_messenger.QueueMessage(new MoveMessage(MoveCommand.TurnRightFull));
          m_messenger.QueueMessage(
            new MoveMessage(MoveCommand.SpeedStop));
          break;
      }
    }

    private void SetState(AiState state)
    {
      m_aiState = state;

      switch (m_aiState)
      {
        case AiState.Search:
          SetMoveState(MoveState.Forward);
          break;

        case AiState.ApproachEnemy:
          break;

        case AiState.Attack:
          SetMoveState(MoveState.Stopped);
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

    private void HandlePostStep(float deltaTime)
    {
      m_collisionTime += deltaTime;
      if (m_collisionTime < CollisionUpdateInterval)
      {
        return;
      }

      m_collisionTime %= CollisionUpdateInterval;

      if (m_moveState != MoveState.Stopped)
      {
        DoRaycasts();
        UpdateMovement();
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

      m_physicsManager.PostStep -= HandlePostStep;

      base.Dispose(disposing);
      m_disposed = true;
    }

    #endregion
  }
}
