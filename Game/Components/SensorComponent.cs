using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Factories;
using GeneticTanks.Game.Components.Messages;
using GeneticTanks.Game.Components.Tank;
using GeneticTanks.Game.Managers;
using log4net;
using SFML.Graphics;
using SFML.Window;

namespace GeneticTanks.Game.Components
{
  /// <summary>
  /// Adds a sensor that will broadcast events when it detects another tank.
  /// </summary>
  sealed class SensorComponent
    : RenderComponent
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    private static readonly Color FillColor = new Color
    {
      R = Color.Magenta.R,
      G = Color.Magenta.G,
      B = Color.Magenta.B,
      A = 32
    };

    #region Private Fields
    private readonly PhysicsManager m_physicsManager;
    private MessageComponent m_messenger;
    private TankStateComponent m_state;

    private RenderStates m_renderStates = new RenderStates
    {
      BlendMode = BlendMode.Alpha,
      Transform = Transform.Identity
    };
    private CircleShape m_shape;
    
    private Body m_body;
    private bool m_sensorEnabled;
    private readonly HashSet<uint> m_contacts = new HashSet<uint>(); 
    #endregion

    /// <summary>
    /// Create the component.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="pm"></param>
    public SensorComponent(Entity parent, PhysicsManager pm) 
      : base(parent)
    {
      if (pm == null)
      {
        throw new ArgumentNullException("pm");
      }

      m_physicsManager = pm;

      EnableDebugRender = true;
      ZDepth = 100;
      NeedsUpdate = false;
    }

    /// <summary>
    /// The state of the sensor.
    /// </summary>
    public bool Enabled
    {
      get { return m_sensorEnabled; }
      set { Enable(value); }
    }

    /// <summary>
    /// The state of sensor debug drawing.
    /// </summary>
    public bool EnableDebugRender { get; set; }

    /// <summary>
    /// Enable or disable the sensor.
    /// </summary>
    /// <param name="state"></param>
    public void Enable(bool state)
    {
      m_sensorEnabled = state;

      if (!Initialized)
      {
        return;
      }

      if (m_sensorEnabled)
      {
        m_body.CollidesWith = PhysicsManager.TankCategory;
        m_body.CollisionCategories = PhysicsManager.SensorCategory;
      }
      else
      {
        m_body.CollidesWith = Category.None;
        m_body.CollisionCategories = Category.None;
        m_contacts.Clear();
      }
    }

    /// <summary>
    /// All the contacts the sensor is currently tracking.
    /// </summary>
    public List<uint> Contacts { get { return m_contacts.ToList(); } } 

    #region RenderComponent Implementation

    public override bool Initialize()
    {
      if (!base.Initialize())
      {
        return false;
      }
      if (!RetrieveSibling(out m_messenger))
      {
        return false;
      }
      if (!RetrieveSibling(out m_state))
      {
        return false;
      }

      m_shape = new CircleShape
      {
        FillColor = FillColor,
        Radius = m_state.SensorRadius,
        Origin = new Vector2f(m_state.SensorRadius, m_state.SensorRadius)
      };

      m_body = BodyFactory.CreateBody(m_physicsManager.World, Parent.Id);
      FixtureFactory.AttachCircle(m_state.SensorRadius, 0, m_body, Parent.Id);
      m_body.BodyType = BodyType.Dynamic;
      m_body.IsSensor = true;

      m_body.OnCollision += HandleSensorCollision;
      m_body.OnSeparation += HandleSensorSeparation;
      m_physicsManager.PreStep += HandlePreStep;
      m_physicsManager.PostStep += HandlePostStep;
      
      Initialized = true;
      Enabled = true;
      return true;
    }
    
    public override void Update(float deltaTime)
    {
    }

    public override void Draw(RenderTarget target)
    {
      if (!EnableDebugRender || target == null)
      {
        return;
      }

      target.Draw(m_shape, m_renderStates);
    }

    #endregion
    #region Callbacks

    private bool HandleSensorCollision(Fixture fixtureA, Fixture fixtureB,
      Contact contact)
    {
      if ((fixtureB.CollisionCategories & PhysicsManager.TankCategory) > 0)
      {
        var id = Convert.ToUInt32(fixtureB.UserData);
        if (id != Parent.Id && !m_contacts.Contains(id))
        {
          m_contacts.Add(id);
          m_messenger.QueueMessage(new SensorNewContactMessage(id));
        }
      }

      return true;
    }

    private void HandleSensorSeparation(Fixture fixtureA, Fixture fixtureB)
    {
      var id = Convert.ToUInt32(fixtureB.UserData);
      if (m_contacts.Remove(id))
      {
        m_messenger.QueueMessage(new SensorLostContactMessage(id));
      }
    }

    // necessary to sync the position to the parent object before the first
    // physics update
    private void HandlePreStep(float deltaTime)
    {
      m_body.Position = Parent.Transform.Position;
      m_physicsManager.PreStep -= HandlePreStep;
    }

    private void HandlePostStep(float deltaTime)
    {
      m_body.Position = Parent.Transform.Position;
      m_renderStates.Transform = Parent.Transform.GraphicsTransform;
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

      if (disposing)
      {
        m_shape.Dispose();
      }

      m_physicsManager.PostStep -= HandlePostStep;
      m_body.OnCollision -= HandleSensorCollision;
      m_body.OnSeparation -= HandleSensorSeparation;

      m_physicsManager.World.RemoveBody(m_body);
      m_body = null;

      base.Dispose(disposing);
      m_disposed = true;
    }

    #endregion
  }
}
