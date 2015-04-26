using System;
using System.Diagnostics;
using System.Reflection;
using GeneticTanks.Extensions;
using GeneticTanks.Game.Managers;
using log4net;

namespace GeneticTanks.Game.Processes
{
  abstract class Process
    : IDisposable
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    /// The only invalid process id.
    /// </summary>
    public const uint InvalidId = 0;

    public enum State
    {
      // inactive states
      NotInitialized,
      // living states
      Running,
      Paused,
      // dead states
      Succeeded,
      Failed,
      Aborted
    }
    
    /// <summary>
    /// Create the process.
    /// </summary>
    protected Process()
    {
      Id = ProcessManager.NextId;
      Name = string.Format("PID {0} ({1})", Id, GetType().Name);
      CurrentState = State.NotInitialized;
    }

    /// <summary>
    /// The id of this process.
    /// </summary>
    public uint Id { get; private set; }

    /// <summary>
    /// The name of this process, consisting of its Id and type name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// The amount of time in seconds that the process has been running.
    /// </summary>
    public float Runtime { get; private set; }
    
    /// <summary>
    /// The current state of the process.
    /// </summary>
    public State CurrentState { get; private set; }

    /// <summary>
    /// The child of this process.  The child will be activated if and only if 
    /// its parent process completes successfully.
    /// </summary>
    public Process Child { get; private set; }

    /// <summary>
    /// Determine if the process is alive (running or paused).
    /// </summary>
    public bool IsAlive
    {
      get
      {
        return CurrentState == State.Running || IsPaused;
      }
    }

    /// <summary>
    /// Determine if the process is paused.
    /// </summary>
    public bool IsPaused { get { return CurrentState == State.Paused; } }

    /// <summary>
    /// Determine if the process is dead (succeeded, failed, or aborted).
    /// </summary>
    public bool IsDead
    {
      get
      {
        return CurrentState == State.Succeeded ||
               CurrentState == State.Failed || CurrentState == State.Aborted;
      }
    }

    #region ProcessManager Control Methods

    /// <summary>
    /// Initialize the process.
    /// </summary>
    /// <returns>
    /// True if initialization succeeded.
    /// </returns>
    public bool Initialize()
    {
      Debug.Assert(CurrentState == State.NotInitialized);

      if (!OnInitialize())
      {
        Log.ErrorFmt("{0} failed to initialize", Name);
        return false;
      }

      Log.VerboseFmt("{0} initialized", Name);
      CurrentState = State.Running;
      return true;
    }

    /// <summary>
    /// Updates the process execution.
    /// </summary>
    /// <param name="deltaTime"></param>
    public void Update(float deltaTime)
    {
      Debug.Assert(IsAlive);

      if (CurrentState == State.Running)
      {
        Runtime += deltaTime;
        OnUpdate(deltaTime);
      }
    }

    /// <summary>
    /// Called when the process finishes its task successfully.
    /// </summary>
    public void Succeed()
    {
      Debug.Assert(!IsDead);

      Log.VerboseFmt("{0} succeeded", Name);
      CurrentState = State.Succeeded;
      OnSucceed();
    }

    /// <summary>
    /// Called when the process fails in its task.
    /// </summary>
    public void Fail()
    {
      Debug.Assert(!IsDead);

      Log.VerboseFmt("{0} failed", Name);
      CurrentState = State.Failed;
      OnFail();
    }

    /// <summary>
    /// Called when the process is aborted without completing.  Affects only 
    /// this process.
    /// </summary>
    public void Abort()
    {
      Debug.Assert(!IsDead);

      Log.VerboseFmt("{0} aborted", Name);
      CurrentState = State.Aborted;
      OnAbort();
    }

    /// <summary>
    /// Aborts this process and all of its children.
    /// </summary>
    public void AbortAll()
    {
      Abort();
      if (Child != null)
      {
        Child.AbortAll();
      }
    }

    #endregion

    #region Child Accessors

    /// <summary>
    /// Attaches a child process to the end of this process chain.
    /// </summary>
    /// <param name="process"></param>
    public void AttachChild(Process process)
    {
      if (process == null)
      {
        throw new ArgumentNullException("process");
      }

      if (Child == null)
      {
        Child = process;
      }
      else
      {
        Child.AttachChild(process);
      }
    }

    /// <summary>
    /// Removes and returns the child of this process or null if there is no 
    /// child.
    /// </summary>
    /// <returns></returns>
    public Process RemoveChild()
    {
      Process result = null;

      if (Child != null)
      {
        result = Child;
        Child = null;
      }

      return result;
    }

    #endregion

    #region Interface

    /// <summary>
    /// Override to perform initialization of the process.
    /// </summary>
    /// <returns>
    /// The success or failure of initialization.
    /// </returns>
    protected virtual bool OnInitialize()
    {
      return true;
    }

    /// <summary>
    /// Override to perform the main work of the process.  Called every frame.  
    /// Generally this function is responsible for completing the process by 
    /// calling Succeed(), Fail(), or Abort().  Note that Runtime is updated 
    /// before this method is called.
    /// </summary>
    /// <param name="deltaTime">
    /// Elapsed time since OnUpdate was last called.
    /// </param>
    protected abstract void OnUpdate(float deltaTime);

    /// <summary>
    /// Override to perform an action when the process succeeds.
    /// </summary>
    protected virtual void OnSucceed()
    {
    }

    /// <summary>
    /// Override to perform an action when the process fails.
    /// </summary>
    protected virtual void OnFail()
    {
    }

    /// <summary>
    /// Override to perform an action when the process is aborted.
    /// </summary>
    protected virtual void OnAbort()
    {
    }

    #endregion

    #region IDisposable Implementation

    private bool m_disposed = false;

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (m_disposed)
      {
        return;
      }

      Log.VerboseFmt("{0} disposing", Name);

      if (disposing && Child != null)
      {
        Child.Dispose();
      }

      Child = null;

      m_disposed = true;
    }

    ~Process()
    {
      Dispose(false);
    }

    #endregion
  }
}
