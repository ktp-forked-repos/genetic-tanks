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
    // update rate for collision checks
    private const float UpdateInterval = 1f / 5f;

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
      ForwardCollision,
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

    private float m_updateTime = 0f;
    private float m_collisionUpdateTime = 0f;

    private AiState m_aiState;
    private MoveState m_moveState;

    private Vector2 m_rayOrigin;
    private bool m_centerObstacle;
    private bool m_leftObstacle;
    private bool m_rightObstacle;
    
    private readonly List<Entity> m_contacts = new List<Entity>();

    private Entity m_target = null;
    private float m_targetRange = 0f;
    private float m_targetHeading = 0f;
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
      m_updateTime += deltaTime;
      if (m_updateTime < UpdateInterval)
      {
        return;
      }

      m_updateTime %= UpdateInterval;

      switch (m_aiState)
      {
        case AiState.ApproachEnemy:
          UpdateApproach();
          break;

        case AiState.Attack:
          UpdateAttack();
          break;
      }
    }

    #endregion
    #region Private Methods
    #region Movement Control Methods

    private void UpdateMovement()
    {
      switch (m_moveState)
      {
        case MoveState.ForwardCollision:
          UpdateForwardCollision();
          break;

        case MoveState.TurnLeftCollision:
        case MoveState.TurnRightCollision:
          UpdateTurnCollision();
          break;
      }
    }

    private void UpdateForwardCollision()
    {
      if (!(m_leftObstacle || m_centerObstacle || m_rightObstacle))
      {
        return;
      }

      SelectCollisionTurnDirection();
    }

    private void UpdateTurnCollision()
    {
      if (!m_leftObstacle && !m_centerObstacle && !m_rightObstacle)
      {
        SetMoveState(MoveState.ForwardCollision);
      }
    }

    private void SelectCollisionTurnDirection()
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
    
    private void DoCollisionRaycasts()
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
          Log.DebugFormat("{0} stopping", Parent.FullName);
          m_messenger.QueueMessage(new MoveMessage(MoveCommand.AllStop));
          break;

        case MoveState.Forward:
        case MoveState.ForwardCollision:
          Log.DebugFormat("{0} moving forward", Parent.FullName);
          m_messenger.QueueMessage(new MoveMessage(MoveCommand.TurnStop));
          m_messenger.QueueMessage(
            new MoveMessage(MoveCommand.SpeedForwardFull));
          break;

        case MoveState.TurnLeft:
        case MoveState.TurnLeftCollision:
          Log.DebugFormat("{0} turning left", Parent.FullName);
          m_messenger.QueueMessage(new MoveMessage(MoveCommand.TurnLeftFull));
          m_messenger.QueueMessage(
            new MoveMessage(MoveCommand.SpeedStop));
          break;

        case MoveState.TurnRight:
        case MoveState.TurnRightCollision:
          Log.DebugFormat("{0} turning right", Parent.FullName);
          m_messenger.QueueMessage(new MoveMessage(MoveCommand.TurnRightFull));
          m_messenger.QueueMessage(
            new MoveMessage(MoveCommand.SpeedStop));
          break;
      }
    }

    #endregion

    private void SetState(AiState state)
    {
      m_aiState = state;

      switch (m_aiState)
      {
        case AiState.Search:
          SetMoveState(MoveState.ForwardCollision);
          break;

        case AiState.ApproachEnemy:
          SetMoveState(MoveState.Stopped);
          break;

        case AiState.Attack:
          SetMoveState(MoveState.Stopped);
          break;
      }
    }

    private void UpdateApproach()
    {
      UpdateTargetInfo();

      var angleDiff = m_targetHeading - Parent.Transform.Rotation;
      if (angleDiff <= -180f)
      {
        angleDiff += 360f;
      }
      else if (angleDiff >= 180f)
      {
        angleDiff -= 360f;
      }

      var desiredRange = m_state.GunRange - (m_state.GunRange / 10f);

      // align to the target heading
      if (Math.Abs(angleDiff) > 10f)
      {
        if (angleDiff < 0f && m_moveState != MoveState.TurnRight)
        {
          SetMoveState(MoveState.TurnRight);
        }
        else if (angleDiff > 0f && m_moveState != MoveState.TurnLeft)
        {
          SetMoveState(MoveState.TurnLeft);
        }
      }
      // move within range
      else if (m_targetRange > desiredRange)
      {
        if (m_moveState != MoveState.Forward)
        {
          SetMoveState(MoveState.Forward);
        }
      }
      else
      {
        SetState(AiState.Attack);
      }
    }

    private void UpdateTargetInfo()
    {
      var targetDirection =
        m_target.Transform.Position - Parent.Transform.Position;
      var angle = Math.Atan2(targetDirection.Y, targetDirection.X) -
                  Math.Atan2(Vector2.UnitX.Y, Vector2.UnitX.X);

      m_targetRange = targetDirection.Length();
      m_targetHeading = MathHelper.ToDegrees((float)angle);
    }

    private void UpdateAttack()
    {
      
    }

    private void SelectTarget()
    {
      Entity closest = null;
      float minDistance = float.MaxValue;

      foreach (var contact in m_contacts)
      {
        var distVec = contact.Transform.Position - Parent.Transform.Position;
        var distance = distVec.LengthSquared();
        if (distance < minDistance)
        {
          minDistance = distance;
          closest = contact;
        }
      }

      m_target = closest;
      m_messenger.QueueMessage(new SetTargetMessage(m_target));

      if (m_target == null)
      {
        Log.DebugFormat("{0} cleared target", Parent.FullName);
        SetState(AiState.Search);
      }
      else
      {
        Log.DebugFormat("{0} set target {1}", Parent.FullName, 
          m_target.FullName);
        SetState(AiState.ApproachEnemy);
      }
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

      m_contacts.Add(entity);
      if (m_target == null)
      {
        SelectTarget();
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

      m_contacts.Remove(entity);
      if (entity.Id == m_target.Id)
      {
        SelectTarget();
      }
    }

    private void HandlePostStep(float deltaTime)
    {
      m_collisionUpdateTime += deltaTime;
      if (m_collisionUpdateTime < UpdateInterval)
      {
        return;
      }

      m_collisionUpdateTime %= UpdateInterval;

      if (m_moveState != MoveState.Stopped)
      {
        DoCollisionRaycasts();
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
