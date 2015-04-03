using System;
using System.Reflection;
using FarseerPhysics.Dynamics;
using log4net;
using Microsoft.Xna.Framework;

namespace GeneticTanks.Game.Components
{
  /// <summary>
  /// Provides a TransformComponent that is locked to a physics body.
  /// </summary>
  abstract class PhysicsTransformComponent
    : TransformComponent
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    public static readonly Vector2 ForwardVector = new Vector2(1, 0);
    public static readonly Vector2 BackVector = new Vector2(-1, 0);
    public static readonly Vector2 RightVector = new Vector2(0, 1);
    public static readonly Vector2 LeftVector = new Vector2(0, -1);

    protected PhysicsManager PhysicsManager { get; private set; }

    /// <summary>
    /// Create the transform.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="pm"></param>
    protected PhysicsTransformComponent(Entity parent, PhysicsManager pm) 
      : base(parent)
    {
      if (pm == null)
      {
        throw new ArgumentNullException("pm");
      }
      if (pm.World == null)
      {
        throw new ArgumentException(
          "PhysicsManager does not have a created World", "pm");
      }

      PhysicsManager = pm;
      World = PhysicsManager.World;
    }

    /// <summary>
    /// The physics world the object lives in.
    /// </summary>
    public World World { get; private set; }

    /// <summary>
    /// The physics body bound to the transform.  Not valid until after the 
    /// component is initialized.
    /// </summary>
    /// <remarks>
    /// The subclass creates the body, but afterwards it is owned and disposed 
    /// of by the base class.
    /// </remarks>
    public Body Body { get; protected set; }

    /// <summary>
    /// See <see cref="TransformComponent.Position"/>.  Accessing the position 
    /// is not valid until the component is initialized.
    /// </summary>
    public sealed override Vector2 Position
    {
      get { return Body.Position; }
      set { Body.Position = value;
      }
    }

    /// <summary>
    /// See <see cref="TransformComponent.Rotation"/>.  Accessing the rotation 
    /// is not valid until the component is initialized.
    /// </summary>
    public sealed override float Rotation
    {
      get { return MathHelper.ToDegrees(Body.Rotation); }
      set { Body.Rotation = MathHelper.ToRadians(value); }
    }

    /// <summary>
    /// Performs a raycast between points relative to the body of this object,
    /// allowing filtering by category.
    /// </summary>
    /// <param name="localSource"></param>
    /// <param name="localTarget"></param>
    /// <param name="category"></param>
    /// <returns>
    /// The world point of the closest contact point.
    /// </returns>
    public Vector2 RaycastPoint(Vector2 localSource, Vector2 localTarget, 
      Category category)
    {
      var source = Body.GetWorldPoint(localSource);
      var target = Body.GetWorldPoint(localTarget);
      var result = Vector2.Zero;

      World.RayCast((fixture, point, normal, frac) => 
        RaycastCallback(ref result, category, fixture, point, normal, frac),
        source, target);

      return result;
    }

    /// <summary>
    /// Performs a raycast between points relative to the body of this object,
    /// allowing filtering by category.
    /// </summary>
    /// <param name="localSource"></param>
    /// <param name="localTarget"></param>
    /// <param name="category"></param>
    /// <returns>
    /// The distance to the closest contact point.
    /// </returns>
    public float RaycastDistance(Vector2 localSource, Vector2 localTarget,
      Category category)
    {
      return RaycastPoint(localSource, localTarget, category).Length();
    }

    #region TransformComponent Implementation
    
    public override void Update(float deltaTime)
    {
    }

    #endregion
    #region Private Methods

    // handles ray cast contacts, ignoring contacts that are part of the 
    // parent entity and returning the closest contact in hitPoint
    private float RaycastCallback(ref Vector2 hitPoint, Category category,
      Fixture fixture, Vector2 point, Vector2 normal, float fraction)
    {
      if (Convert.ToUInt32(fixture.UserData) == Parent.Id)
      {
        return -1;
      }
      
      if ((fixture.CollisionCategories & category) > 0)
      {
        hitPoint = point;
        return fraction;
      }

      return 1;
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
      
      World.RemoveBody(Body);
      Body = null;
      World = null;

      base.Dispose(disposing);
      m_disposed = true;
    }

    #endregion
  }
}
