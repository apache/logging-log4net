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
