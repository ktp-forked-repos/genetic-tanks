using System;
using GeneticTanks.Extensions;
using Microsoft.Xna.Framework;
using SFML.Graphics;

namespace GeneticTanks.Game.Components
{
  /// <summary>
  /// Holds the transform information for an entity.
  /// </summary>
  sealed class TransformComponent
    : Component
  {
    private float m_rotation;

    /// <summary>
    /// Create the transform component.
    /// </summary>
    /// <param name="parent"></param>
    public TransformComponent(Entity parent) 
      : base(parent)
    {
      Position = Vector2.Zero;
    }

    /// <summary>
    /// The object's position.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// The object's rotation in degrees.  The value is always between 0 and
    /// 360 degrees.
    /// </summary>
    public float Rotation
    {
      get {return m_rotation; }
      set
      {
        m_rotation = value;
        if (m_rotation >= 360f)
        {
          m_rotation -= 360f;
        }
        else if (m_rotation < 0f)
        {
          m_rotation += 360f;
        }
      }
    }

    /// <summary>
    /// Uses the component to build a SFML transform.
    /// </summary>
    public Transform GraphicsTransform
    {
      get
      {
        var transform = Transform.Identity;
        transform.Translate(Position.ToVector2f().InvertY());
        transform.Rotate(-Rotation);
        return transform;
      }
    }

    /// <summary>
    /// Move the transform by the specified amount in the x and y axis.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public void Move(float x, float y)
    {
      Move(new Vector2(x, y));
    }

    /// <summary>
    /// Move the transform by the amount in the offset vector.
    /// </summary>
    /// <param name="offset"></param>
    /// <exception cref="ArgumentNullException">offset is null</exception>
    public void Move(Vector2 offset)
    {
      if (offset == null)
      {
        throw new ArgumentNullException("offset");
      }
      Position += offset;
    }

    /// <summary>
    /// Rotates the transform by the specified degrees.
    /// </summary>
    /// <param name="deg"></param>
    public void Rotate(float deg)
    {
      Rotation += deg;
    }

    #region Component Implementation

    public override bool Initialize()
    {
      return true;
    }

    public override void Update(float deltaTime)
    {
    }

    #endregion
  }
}
