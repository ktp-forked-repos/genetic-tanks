using System;
using log4net;
using log4net.Core;

namespace GeneticTanks.Extensions
{
  /// <summary>
  /// Extensions to help with log4net logging.
  /// </summary>
  static class LogExtensions
  {
    /// <summary>
    /// Logs a message at the verbose level, if enabled.
    /// </summary>
    /// <param name="log"></param>
    /// <param name="message"></param>
    public static void Verbose(this ILog log, string message)
    {
      log.Logger.Log(
          System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
          Level.Verbose, message, null
          );
    }

    /// <summary>
    /// Logs an exception at the verbose level, if enabled.
    /// </summary>
    /// <param name="log"></param>
    /// <param name="exception"></param>
    public static void Verbose(this ILog log, Exception exception)
    {
      log.Logger.Log(
          System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
          Level.Verbose, null, exception
          );
    }

    /// <summary>
    /// Logs a message at the verbose level, if enabled.
    /// 
    /// Performs string formatting only if verbose is enabled.
    /// </summary>
    /// <param name="log"></param>
    /// <param name="format"></param>
    /// <param name="args"></param>
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

    /// <summary>
    /// Logs a message at the verbose level, if it is enabled and condition is 
    /// true.
    /// 
    /// Performs string formatting only if verbose is enabled and condition is 
    /// true.
    /// </summary>
    /// <param name="log"></param>
    /// <param name="condition"></param>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public static void VerboseFmtIf(this ILog log, bool condition,
      string format, params object[] args)
    {
      if (condition)
      {
        log.VerboseFmt(format, args);
      }
    }

    /// <summary>
    /// Logs a message at the debug level, if enabled.
    /// 
    /// Performs string formatting only if debug is enabled.
    /// </summary>
    /// <param name="log"></param>
    /// <param name="format"></param>
    /// <param name="args"></param>
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

    /// <summary>
    /// Logs a message at the debug level, if it is enabled and condition is 
    /// true.
    /// 
    /// Performs string formatting only if debug is enabled and condition is 
    /// true.
    /// </summary>
    /// <param name="log"></param>
    /// <param name="condition"></param>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public static void DebugFmtIf(this ILog log, bool condition,
      string format, params object[] args)
    {
      if (condition)
      {
        log.DebugFmt(format, args);
      }
    }

    /// <summary>
    /// Logs a message at the info level, if enabled.
    /// 
    /// Performs string formatting only if info is enabled.
    /// </summary>
    /// <param name="log"></param>
    /// <param name="format"></param>
    /// <param name="args"></param>
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

    /// <summary>
    /// Logs a message at the info level, if it is enabled and condition is 
    /// true.
    /// 
    /// Performs string formatting only if info is enabled and condition is 
    /// true.
    /// </summary>
    /// <param name="log"></param>
    /// <param name="condition"></param>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public static void InfoFmtIf(this ILog log, bool condition,
      string format, params object[] args)
    {
      if (condition)
      {
        log.InfoFmt(format, args);
      }
    }

    /// <summary>
    /// Logs a message at the warn level, if enabled.
    /// 
    /// Performs string formatting only if warn is enabled.
    /// </summary>
    /// <param name="log"></param>
    /// <param name="format"></param>
    /// <param name="args"></param>
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

    /// <summary>
    /// Logs a message at the warn level, if it is enabled and condition is 
    /// true.
    /// 
    /// Performs string formatting only if warn is enabled and condition is 
    /// true.
    /// </summary>
    /// <param name="log"></param>
    /// <param name="condition"></param>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public static void WarnFmtIf(this ILog log, bool condition,
      string format, params object[] args)
    {
      if (condition)
      {
        log.WarnFmt(format, args);
      }
    }

    /// <summary>
    /// Logs a message at the error level, if enabled.
    /// 
    /// Performs string formatting only if error is enabled.
    /// </summary>
    /// <param name="log"></param>
    /// <param name="format"></param>
    /// <param name="args"></param>
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

    /// <summary>
    /// Logs a message at the error level, if it is enabled and condition is 
    /// true.
    /// 
    /// Performs string formatting only if error is enabled and condition is 
    /// true.
    /// </summary>
    /// <param name="log"></param>
    /// <param name="condition"></param>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public static void ErrorFmtIf(this ILog log, bool condition,
      string format, params object[] args)
    {
      if (condition)
      {
        log.ErrorFmt(format, args);
      }
    }
  }
}
