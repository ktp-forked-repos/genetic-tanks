using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;

namespace GeneticTanks.Game.Components
{
  /// <summary>
  /// Provides a TransformComponent that is locked to a physics body.
  /// </summary>
  abstract class PhysicsTransformComponent
    : TransformComponent
  {
    protected Body Body { get; set; }

    protected PhysicsTransformComponent(Entity parent) 
      : base(parent)
    {
    }

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
  }
}
