#region Apache License
//
// Licensed to the Apache Software Foundation (ASF) under one or more 
// contributor license agreements. See the NOTICE file distributed with
// this work for additional information regarding copyright ownership. 
// The ASF licenses this file to you under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with 
// the License. You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.Collections;
using System.Globalization;
using log4net;
using log4net.Appender;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace NotLogging
{
  /// <summary>
  /// NotLogging
  /// </summary>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider")]
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
  internal static class NotLogging
  {
    #region Init Code

    private static int warmupCycles = 10000;
    private static readonly ILog SHORT_LOG = LogManager.GetLogger("A0123456789");
    private static readonly ILog MEDIUM_LOG = LogManager.GetLogger("A0123456789.B0123456789");
    private static readonly ILog LONG_LOG = LogManager.GetLogger("A0123456789.B0123456789.C0123456789");
    private static readonly ILog INEXISTENT_SHORT_LOG = LogManager.GetLogger("I0123456789");
    private static readonly ILog INEXISTENT_MEDIUM_LOG = LogManager.GetLogger("I0123456789.B0123456789");
    private static readonly ILog INEXISTENT_LONG_LOG = LogManager.GetLogger("I0123456789.B0123456789.C0123456789");
    private static readonly ILog[] LOG_ARRAY = [
      SHORT_LOG,
      MEDIUM_LOG,
      LONG_LOG,
      INEXISTENT_SHORT_LOG,
      INEXISTENT_MEDIUM_LOG,
      INEXISTENT_LONG_LOG];
    private static readonly TimedTest[] TIMED_TESTS = [
      new SimpleMessage_Bare(),
      new SimpleMessage_Array(),
      new SimpleMessage_MethodGuard_Bare(),
      new SimpleMessage_LocalGuard_Bare(),
      new ComplexMessage_Bare(),
      new ComplexMessage_Array(),
      new ComplexMessage_MethodGuard_Bare(),
      new ComplexMessage_MethodGuard_Array(),
      new ComplexMessage_MemberGuard_Bare(),
      new ComplexMessage_LocalGuard_Bare()];

    private static void Usage()
    {
      Console.WriteLine(
        "Usage: NotLogging <true|false> <runLength>" + Environment.NewLine +
        "\t true indicates shipped code" + Environment.NewLine +
        "\t false indicates code in development" + Environment.NewLine +
        "\t runLength is an int representing the run length of loops" + Environment.NewLine +
        "\t We suggest that runLength be at least 1000000 (1 million).");
      Environment.Exit(1);
    }

    /// <summary>
    /// Program wide initialization method
    /// </summary>
    /// <param name="args"></param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
    private static int ProgramInit(string[] args)
    {
      if (args is not string[] { Length: 2 })
      {
        Usage();
        return 0;
      }
      if (!int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int runLength))
      {
        Usage();
        return 0;
      }

      ConsoleAppender appender = new() { Layout = new SimpleLayout() };
      ((SimpleLayout)appender.Layout).ActivateOptions();
      appender.ActivateOptions();

      if ("false" == args[0])
      {
        // nothing to do
      }
      else if ("true" == args[0])
      {
        Console.WriteLine("Flagging as shipped code.");
        ((Hierarchy)LogManager.GetRepository()).Threshold = log4net.Core.Level.Warn;
      }
      else
      {
        Usage();
      }

      ((Logger)SHORT_LOG.Logger).Level = log4net.Core.Level.Info;
      ((Hierarchy)LogManager.GetRepository()).Root.Level = log4net.Core.Level.Info;
      ((Hierarchy)LogManager.GetRepository()).Root.AddAppender(appender);

      return runLength;
    }

    #endregion

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main(string[] argv)
    {
      if (System.Diagnostics.Debugger.IsAttached)
      {
        warmupCycles = 0;
        argv = ["false", "2"];
      }
      if (argv.Length != 2)
        Usage();

      int runLength = ProgramInit(argv);

      Console.WriteLine();
      Console.Write("Warming Up...");

      if (warmupCycles > 0)
        foreach (ILog logger in LOG_ARRAY)
          foreach (TimedTest timedTest in TIMED_TESTS)
            timedTest.Run(logger, warmupCycles);

      Console.WriteLine("Done");
      Console.WriteLine();

      // Calculate maximum description length
      int maxDescLen = 0;
      foreach (TimedTest timedTest in TIMED_TESTS)
        maxDescLen = Math.Max(maxDescLen, timedTest.Description.Length);

      string formatString = "{0,-" + (maxDescLen + 1) + "} {1,9:G} ticks. Log: {2}";
      double delta;

      ArrayList averageData = [];

      foreach (TimedTest timedTest in TIMED_TESTS)
      {
        double total = 0;
        foreach (ILog logger in LOG_ARRAY)
        {
          delta = timedTest.Run(logger, runLength);
          Console.WriteLine(string.Format(formatString, timedTest.Description, delta, ((Logger)logger.Logger).Name));

          total += delta;
        }
        Console.WriteLine();

        averageData.Add(new object[] { timedTest, total / ((double)LOG_ARRAY.Length) });
      }
      Console.WriteLine();
      Console.WriteLine("Averages:");
      Console.WriteLine();

      foreach (object[] pair in averageData)
      {
        string avgFormatString = "{0,-" + (maxDescLen + 1) + "} {1,9:G} ticks.";
        Console.WriteLine(string.Format(avgFormatString, ((TimedTest)pair[0]).Description, ((double)pair[1])));
      }
    }

    internal abstract class TimedTest
    {
      public abstract double Run(ILog log, long runLength);
      public abstract string Description { get; }
    }

    #region Tests calling Debug(string)

    internal sealed class SimpleMessage_Bare : TimedTest
    {
      public override double Run(ILog log, long runLength)
      {
        DateTime before = DateTime.Now;
        for (int i = 0; i < runLength; i++)
        {
          log.Debug("msg");
        }
        DateTime after = DateTime.Now;
        TimeSpan diff = after - before;
        return ((double)diff.Ticks) / ((double)runLength);
      }

      public override string Description => "log.Debug(\"msg\");";
    }

    internal sealed class ComplexMessage_MethodGuard_Bare : TimedTest
    {
      public override double Run(ILog log, long runLength)
      {
        DateTime before = DateTime.Now;
        for (int i = 0; i < runLength; i++)
        {
          if (log.IsDebugEnabled)
          {
            log.Debug("msg" + i + "msg");
          }
        }
        DateTime after = DateTime.Now;
        TimeSpan diff = after - before;
        return ((double)diff.Ticks) / ((double)runLength);
      }

      public override string Description => "if(log.IsDebugEnabled) log.Debug(\"msg\" + i + \"msg\");";
    }

    internal sealed class ComplexMessage_Bare : TimedTest
    {
      public override double Run(ILog log, long runLength)
      {
        DateTime before = DateTime.Now;
        for (int i = 0; i < runLength; i++)
        {
          log.Debug("msg" + i + "msg");
        }
        DateTime after = DateTime.Now;
        TimeSpan diff = after - before;
        return ((double)diff.Ticks) / ((double)runLength);
      }

      public override string Description => "log.Debug(\"msg\" + i + \"msg\");";
    }

    #endregion

    #region Tests calling Debug(new object[] { ... })

    internal sealed class SimpleMessage_Array : TimedTest
    {
      public override double Run(ILog log, long runLength)
      {
        DateTime before = DateTime.Now;
        for (int i = 0; i < runLength; i++)
        {
          log.Debug(new object[] { "msg" });
        }
        DateTime after = DateTime.Now;
        TimeSpan diff = after - before;
        return ((double)diff.Ticks) / ((double)runLength);
      }

      public override string Description => "log.Debug(new object[] { \"msg\" });";
    }

    internal sealed class ComplexMessage_MethodGuard_Array : TimedTest
    {
      public override double Run(ILog log, long runLength)
      {
        DateTime before = DateTime.Now;
        for (int i = 0; i < runLength; i++)
        {
          if (log.IsDebugEnabled)
          {
            log.Debug(new object[] { "msg", i, "msg" });
          }
        }
        DateTime after = DateTime.Now;
        TimeSpan diff = after - before;
        return ((double)diff.Ticks) / ((double)runLength);
      }

      public override string Description => "if(log.IsDebugEnabled) log.Debug(new object[] { \"msg\" , i , \"msg\" });";
    }

    internal sealed class ComplexMessage_Array : TimedTest
    {
      public override double Run(ILog log, long runLength)
      {
        DateTime before = DateTime.Now;
        for (int i = 0; i < runLength; i++)
        {
          log.Debug(new object[] { "msg", i, "msg" });
        }
        DateTime after = DateTime.Now;
        TimeSpan diff = after - before;
        return ((double)diff.Ticks) / ((double)runLength);
      }

      public override string Description => "log.Debug(new object[] { \"msg\" , i , \"msg\" });";
    }

    #endregion

    #region Tests calling Debug(string) (using class members)

    internal sealed class ComplexMessage_MemberGuard_Bare : TimedTest
    {
      public override double Run(ILog log, long runLength) => new Impl(log).Run(runLength);

      public override string Description => "if (m_isEnabled) log.Debug(\"msg\" + i + \"msg\");";

      private sealed class Impl
      {
        private readonly ILog log;
        private readonly bool isEnabled;

        public Impl(ILog log)
        {
          this.log = log;
          isEnabled = this.log.IsDebugEnabled;
        }

        public double Run(long runLength)
        {

          DateTime before = DateTime.Now;
          for (int i = 0; i < runLength; i++)
          {
            if (isEnabled)
            {
              log.Debug("msg" + i + "msg");
            }
          }
          DateTime after = DateTime.Now;
          TimeSpan diff = after - before;
          return ((double)diff.Ticks) / ((double)runLength);
        }
      }
    }

    internal sealed class SimpleMessage_LocalGuard_Bare : TimedTest
    {
      public override double Run(ILog log, long runLength)
      {
        bool isEnabled = log.IsDebugEnabled;

        DateTime before = DateTime.Now;
        for (int i = 0; i < runLength; i++)
        {
          if (isEnabled) log.Debug("msg");
        }
        DateTime after = DateTime.Now;
        TimeSpan diff = after - before;
        return ((double)diff.Ticks) / ((double)runLength);
      }

      public override string Description => "if (isEnabled) log.Debug(\"msg\");";
    }

    internal sealed class SimpleMessage_MethodGuard_Bare : TimedTest
    {
      public override double Run(ILog log, long runLength)
      {
        DateTime before = DateTime.Now;
        for (int i = 0; i < runLength; i++)
        {
          if (log.IsDebugEnabled) log.Debug("msg");
        }
        DateTime after = DateTime.Now;
        TimeSpan diff = after - before;
        return ((double)diff.Ticks) / ((double)runLength);
      }

      public override string Description => "if (log.IsDebugEnabled) log.Debug(\"msg\");";
    }

    internal sealed class ComplexMessage_LocalGuard_Bare : TimedTest
    {
      public override double Run(ILog log, long runLength)
      {
        bool isEnabled = log.IsDebugEnabled;

        DateTime before = DateTime.Now;
        for (int i = 0; i < runLength; i++)
        {
          if (isEnabled) log.Debug("msg" + i + "msg");
        }
        DateTime after = DateTime.Now;
        TimeSpan diff = after - before;
        return ((double)diff.Ticks) / ((double)runLength);
      }

      public override string Description => "if (isEnabled) log.Debug(\"msg\" + i + \"msg\");";
    }
    #endregion
  }
}