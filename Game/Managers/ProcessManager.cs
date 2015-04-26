using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GeneticTanks.Game.Processes;
using log4net;

namespace GeneticTanks.Game.Managers
{
  /// <summary>
  /// Manages and controls the lifetime of all active processes.
  /// </summary>
  sealed class ProcessManager
    : IDisposable
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    private static uint _lastProcessId = Process.InvalidId;

    /// <summary>
    /// Gets the next valid process id.
    /// </summary>
    public static uint NextId
    {
      get
      {
        _lastProcessId++;
        return _lastProcessId;
      }
    }

    private readonly List<Process> m_processes = new List<Process>(25);
    private readonly List<Process> m_toAdd = new List<Process>();

    /// <summary>
    /// Updates all active processes.
    /// </summary>
    /// <param name="deltaTime"></param>
    public void Update(float deltaTime)
    {
      Predicate<Process> pred = process =>
      {
        switch (process.CurrentState)
        {
          case Process.State.NotInitialized:
            if (!process.Initialize())
            {
              process.Dispose();
              return true;
            }
            break;

          case Process.State.Running:
            process.Update(deltaTime);
            break;

          case Process.State.Succeeded:
            var child = process.RemoveChild();
            if (child != null)
            {
              m_toAdd.Add(child);
            }
            process.Dispose();
            return true;

          case Process.State.Failed:
          case Process.State.Aborted:
            process.Dispose();
            return true;
        }

        return false;
      };

      m_processes.RemoveAll(pred);
      m_processes.AddRange(m_toAdd);
      m_toAdd.Clear();
    }

    /// <summary>
    /// Adds a process to the manager.  Should be in the NotInitialized state.
    /// </summary>
    /// <param name="process"></param>
    public void AddProcess(Process process)
    {
      if (process == null)
      {
        throw new ArgumentNullException("process");
      }
      if (process.CurrentState != Process.State.NotInitialized)
      {
        Log.WarnFormat("Process {0} was added in state {1}",
          process.Name, process.CurrentState);
      }

      m_processes.Add(process);
    }

    #region IDisposable Implementation

    private bool m_disposed = false;

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
      if (m_disposed)
      {
        return;
      }

      foreach (var process in m_processes.Where(p => p.Alive))
      {
        process.AbortAll();
      }

      if (disposing)
      {
        foreach (var process in m_processes)
        {
          process.Dispose();
        }
      }

      m_processes.Clear();
      m_disposed = true;
    }

    ~ProcessManager()
    {
      Dispose(false);
    }

    #endregion
  }
}
