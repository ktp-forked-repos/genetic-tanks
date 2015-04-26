using System;
using System.Collections.Generic;
using System.Reflection;
using GeneticTanks.Game.Managers;
using log4net;

namespace GeneticTanks.GeneticAlgorithm
{
  sealed class Population
    : IDisposable
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    public static readonly int Size = 
      Properties.Settings.Default.PopulationSize;

    private readonly EventManager m_eventManager;
    private readonly Dictionary<uint, TankGenome> m_activeGenomes = 
      new Dictionary<uint, TankGenome>();

    public Population(EventManager eventManager)
    {
      if (eventManager == null)
      {
        throw new ArgumentNullException("eventManager");
      }

      m_eventManager = eventManager;
    }

    public void Clear()
    {
      m_activeGenomes.Clear();
    }

    public void Random()
    {
      
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

      m_disposed = true;
    }

    ~Population()
    {
      Dispose(false);
    }

    #endregion
  }
}
