#region Copyright
// 
// This framework is based on log4j see http://jakarta.apache.org/log4j
// Copyright (C) The Apache Software Foundation. All rights reserved.
//
// This software is published under the terms of the Apache Software
// License version 1.1, a copy of which has been included with this
// distribution in the LICENSE.txt file.
// 
#endregion

namespace ConsoleApp
{
	using System;

	/// <summary>
	/// Illustrates using log4net to conditionally log events, and 
	/// using log4net to log exceptions, ...
	/// </summary>
	public class LoggingExample
	{
		// Create a logger for use in this class
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(LoggingExample));

		/// <summary>
		/// Logs events.
		/// </summary>
		public static void LogEvents()
		{
			// Log a debug message. Test if debug is enabled before
			// attempting to log the message. This is not required but
			// can make running without logging faster.
			if (log.IsDebugEnabled) log.Debug("This is a debug message");

			try
			{
				Bar();
			}
			catch(Exception ex)
			{
				// Log an error with an exception
				log.Error("Exception thrown from method Bar", ex);
			}

			log.Error("Hey this is an error!");

			// Push a message on to the Nested Diagnostic Context stack
			using(log4net.NDC.Push("NDC_Message"))
			{
				log.Warn("This should have an NDC message");

				// Set a Mapped Diagnostic Context value  
				log4net.MDC.Set("auth", "auth-none");
				log.Warn("This should have an MDC message for the key 'auth'");

			} // The NDC message is popped off the stack at the end of the using {} block

			log.Warn("See the NDC has been popped of! The MDC 'auth' key is still with us.");
		}

		// Helper methods to demonstrate location information and nested exceptions

		private static void Bar()
		{
			Goo();
		}

		private static void Foo()
		{
			throw new Exception("This is an Exception");
		}

		private static void Goo()
		{
			try
			{
				Foo();
			}
			catch(Exception ex)
			{
				throw new ArithmeticException("Failed in Goo. Calling Foo. Inner Exception provided", ex);
			}
		}
	}
}
