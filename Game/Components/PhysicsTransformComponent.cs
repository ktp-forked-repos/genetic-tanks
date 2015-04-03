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

    /// <summary>
    /// Create the transform.
    /// </summary>
    /// <param name="parent"></param>
    protected PhysicsTransformComponent(Entity parent) 
      : base(parent)
    {
    }

    /// <summary>
    /// The physics world the object lives in.
    /// </summary>
    public World World { get; protected set; }

    /// <summary>
    /// The physics body bound to the transform.  Not valid until after the 
    /// component is initialized.
    /// </summary>
    public Body Body { get; protected set; }

    /// <summary>
    /// See <see cref="TransformComponent.Position"/>.  Accessing the position 
    /// is not valid until the component is initialized.
    /// </summary>
    public sealed override Vector2 Position
    {
      get
      {
        return Body == null ? Vector2.Zero : Body.Position;
      }
      set
      {
        if (Body != null)
        {
          Body.Position = value;
        }
      }
    }

    /// <summary>
    /// See <see cref="TransformComponent.Rotation"/>.  Accessing the rotation 
    /// is not valid until the component is initialized.
    /// </summary>
    public sealed override float Rotation
    {
      get
      {
        return Body == null ? 0 : MathHelper.ToDegrees(Body.Rotation);
      }
      set
      {
        if (Body != null)
        {
          Body.Rotation = MathHelper.ToRadians(value);
        }
      }
    }

    public float RaycastFromBody(Vector2 localSource, Vector2 localTarget, 
      Category category)
    {
      var source = Body.GetWorldPoint(localSource);
      var target = Body.GetWorldPoint(localTarget);
      var result = Vector2.Zero;

      World.RayCast((fixture, point, normal, frac) => 
        RaycastCallback(ref result, category, fixture, point, normal, frac),
        source, target);

      return result.Length();
    }

    #region Private Methods

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
  }
}
