using System;
using log4net;
using log4net.Core;

namespace GeneticTanks.Extensions
{
  static class LogExtensions
  {
    public static void Verbose(this ILog log, string message)
    {
      log.Logger.Log(
          System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
          Level.Verbose, message, null
          );
    }

    public static void Verbose(this ILog log, Exception exception)
    {
      log.Logger.Log(
          System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
          Level.Verbose, null, exception
          );
    }

    public static void VerboseFmt(this ILog log, string format, 
      params object[] args)
    {
      if (log.Logger.IsEnabledFor(Level.Verbose))
      {
        var msg = string.Format(format, args);
        log.Logger.Log(
          System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
          Level.Verbose, msg, null
          );
      }
    }

    public static void DebugFmt(this ILog log, string format,
      params object[] args)
    {
      if (log.Logger.IsEnabledFor(Level.Debug))
      {
        var msg = string.Format(format, args);
        log.Logger.Log(
          System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
          Level.Debug, msg, null
          );
      }
    }

    public static void InfoFmt(this ILog log, string format,
      params object[] args)
    {
      if (log.Logger.IsEnabledFor(Level.Info))
      {
        var msg = string.Format(format, args);
        log.Logger.Log(
          System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
          Level.Info, msg, null
          );
      }
    }

    public static void WarnFmt(this ILog log, string format,
      params object[] args)
    {
      if (log.Logger.IsEnabledFor(Level.Warn))
      {
        var msg = string.Format(format, args);
        log.Logger.Log(
          System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
          Level.Warn, msg, null
          );
      }
    }

    public static void ErrorFmt(this ILog log, string format,
      params object[] args)
    {
      if (log.Logger.IsEnabledFor(Level.Error))
      {
        var msg = string.Format(format, args);
        log.Logger.Log(
          System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
          Level.Error, msg, null
          );
      }
    }
  }
}
