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

using SampleAppendersApp.Appender;

// Configure log4net using the .config file
[assembly: log4net.Config.XmlConfigurator(Watch = true)]
// This will cause log4net to look for a configuration file
// called ConsoleApp.exe.config in the application base
// directory (i.e. the directory containing SampleAppendersApp.exe)

namespace SampleAppendersApp
{
  /// <summary>
  /// Example of how to simply configure and use log4net
  /// </summary>
  public static class LoggingExample
  {
    // Create a logger for use in this class
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(LoggingExample));

    /// <summary>
    /// Application entry point
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
    public static void Main()
    {
      log4net.ThreadContext.Properties["session"] = 21;

      // Hookup the FireEventAppender event
      if (FireEventAppender.Instance is not null)
        FireEventAppender.Instance.MessageLoggedEvent += FireEventAppender_MessageLoggedEventHandler;

      // Log an info level message
      if (log.IsInfoEnabled) log.Info("Application [ConsoleApp] Start");

      // Log a debug message. Test if debug is enabled before
      // attempting to log the message. This is not required but
      // can make running without logging faster.
      if (log.IsDebugEnabled) log.Debug("This is a debug message");

      // Log a custom object as the log message
      log.Warn(new MsgObj(42, "So long and thanks for all the fish"));

      try
      {
        Bar();
      }
      catch (ArithmeticException ex)
      {
        // Log an error with an exception
        log.Error("Exception thrown from method Bar", ex);
      }

      log.Error("Hey this is an error!");

      // Log an info level message
      if (log.IsInfoEnabled) log.Info("Application [ConsoleApp] End");

      Console.Write("Press Enter to exit...");
      Console.ReadLine();
    }

    // Helper methods to demonstrate location information and nested exceptions

    private static void Bar() => Goo();

    private static void Foo() => throw new InvalidTimeZoneException("This is an Exception");

    private static void Goo()
    {
      try
      {
        Foo();
      }
      catch (Exception ex)
      {
        throw new ArithmeticException("Failed in Goo. Calling Foo. Inner Exception provided", ex);
      }
    }

    private static void FireEventAppender_MessageLoggedEventHandler(object? sender, MessageLoggedEventArgs e)
      => System.Diagnostics.Trace.WriteLine("EVENT ****" + e.LoggingEvent.RenderedMessage + "****");

    /// <inheritdoc/>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    public sealed class MsgObj(int type, string error)
    {
      /// <inheritdoc/>
      public int MessageType { get; } = type;

      /// <inheritdoc/>
      public string ErrorText { get; } = error;
    }
  }
}
