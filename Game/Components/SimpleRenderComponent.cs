
using System;
using System.Diagnostics;
using System.Reflection;
using GeneticTanks.Extensions;
using GeneticTanks.Game.Managers;
using log4net;
using SFML.Graphics;

namespace GeneticTanks.Game.Components
{
  /// <summary>
  /// Renders a simple primitive shape.  Creation of the shape is delegated to 
  /// a function for better flexibility.
  /// </summary>
  sealed class SimpleRenderComponent
    : RenderComponent
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    #region Private Fields
    private readonly Func<Shape> m_shapeCreator; 
    private Shape m_shape;
    #endregion

    /// <summary>
    /// Create the component.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="zDepth"></param>
    /// <param name="shapeCreator"></param>
    public SimpleRenderComponent(Entity parent, RenderDepth zDepth, 
      Func<Shape> shapeCreator) 
      : base(parent)
    {
      if (shapeCreator == null)
      {
        throw new ArgumentNullException("shapeCreator");
      }

      ZDepth = zDepth;
      m_shapeCreator = shapeCreator;
      NeedsUpdate = false;
    }

    #region Properties

    /// <summary>
    /// Fill color of the shape.  Only valid after initialization.
    /// </summary>
    public Color FillColor
    {
      get
      {
        Debug.Assert(m_shape != null);
        return m_shape.FillColor;
      }
      set
      {
        Debug.Assert(m_shape != null);
        m_shape.FillColor = value;
      }
    }

    /// <summary>
    /// Outline color of the shape.  Only valid after initialization.
    /// </summary>
    public Color OutlineColor
    {
      get
      {
        Debug.Assert(m_shape != null);
        return m_shape.OutlineColor;
      }
      set
      {
        Debug.Assert(m_shape != null);
        m_shape.OutlineColor = value;
      }
    }

    /// <summary>
    /// Outline thickness of the shape.  Only valid after initialization.
    /// </summary>
    public float OutlineThickness
    {
      get
      {
        Debug.Assert(m_shape != null);
        return m_shape.OutlineThickness;
      }
      set 
      { 
        Debug.Assert(m_shape != null); 
        m_shape.OutlineThickness = value; 
      }
    }

    #endregion
    #region RenderComponent Implementation

    public override bool Initialize()
    {
      if (!base.Initialize())
      {
        return false;
      }

      m_shape = m_shapeCreator();
      if (m_shape == null)
      {
        Log.ErrorFmt("{0} had shape creator fail to return shape",
          Parent.FullName);
        return false;
      }

      Initialized = true;
      return true;
    }

    public override void Enable()
    {
    }

    public override void Disable()
    {
    }

    public override void Deactivate()
    {
    }

    public override void Update(float deltaTime)
    {
    }

    public override void Draw(RenderTarget target)
    {
      if (!Initialized || target == null)
      {
        return;
      }

      RenderStates.Transform = Parent.Transform.GraphicsTransform;
      target.Draw(m_shape, RenderStates);
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

      base.Dispose(disposing);
      m_disposed = true;
    }

    #endregion
  }
}
