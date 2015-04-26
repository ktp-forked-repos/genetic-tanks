using System.Reflection;
using GeneticTanks.Game.Processes;
using log4net;

namespace GeneticTanks.Game.Managers
{
  sealed class ProcessManager
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    private static uint _lastProcessId = Process.InvalidId;

    public static uint NextId
    {
      get
      {
        _lastProcessId++;
        return _lastProcessId;
      }
    }
  }
}
