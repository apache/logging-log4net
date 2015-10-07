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

#using <mscorlib.dll>
#include <tchar.h>

using namespace System;

// Configure log4net using the .config file
[assembly: log4net::Config::XmlConfiguratorAttribute(Watch=true)];
// This will cause log4net to look for a configuration file
// called ConsoleApp.exe.config in the application base
// directory (i.e. the directory containing ConsoleApp.exe)


namespace ConsoleApp
{
	/// <summary>
	/// Example of how to simply configure and use log4net
	/// </summary>
	public __gc class LoggingExample
	{
	private:
		// Create a logger for use in this class
		static log4net::ILog* log = log4net::LogManager::GetLogger(System::Reflection::MethodBase::GetCurrentMethod()->DeclaringType);
		// NOTE that using System.Reflection.MethodBase.GetCurrentMethod().DeclaringType
		// is equivalent to typeof(LoggingExample) but is more portable
		// i.e. you can copy the code directly into another class without
		// needing to edit the code.

	public:
		/// <summary>
		/// Application entry point
		/// </summary>
		/// <param name="args">command line arguments</param>
		static void Main(String* args[])
		{
			// Log an info level message
			if (log->IsInfoEnabled) log->Info(S"Application [ConsoleApp] Start");

			// Log a debug message. Test if debug is enabled before
			// attempting to log the message. This is not required but
			// can make running without logging faster.
			if (log->IsDebugEnabled) log->Debug(S"This is a debug message");

			try
			{
				Bar();
			}
			catch(Exception* ex)
			{
				// Log an error with an exception
				log->Error(S"Exception thrown from method Bar", ex);
			}

			log->Error(S"Hey this is an error!");

			try
			{
				// Push a message on to the Nested Diagnostic Context stack
				log4net::NDC::Push(S"NDC_Message");
				log->Warn(S"This should have an NDC message");

				// Set a Mapped Diagnostic Context value  
				log4net::MDC::Set(S"auth", S"auth-none");
				log->Warn(S"This should have an MDC message for the key 'auth'");

			}
			__finally 
			{
				// Pop the NDC message off the stack at the end of the block
				log4net::NDC::Pop();
			}

			log->Warn(S"See the NDC has been popped of! The MDC 'auth' key is still with us.");

			// Log an info level message
			if (log->IsInfoEnabled) log->Info(S"Application [ConsoleApp] End");

			Console::Write(S"Press Enter to exit...");
			Console::ReadLine();
		}

	private:
		// Helper methods to demonstrate location information and nested exceptions

		static void Bar()
		{
			Goo();
		}

		static void Foo()
		{
			throw new Exception(S"This is an Exception");
		}

		static void Goo()
		{
			try
			{
				Foo();
			}
			catch(Exception* ex)
			{
				throw new ArithmeticException(S"Failed in Goo. Calling Foo. Inner Exception provided", ex);
			}
		}
	};
}

// This is the entry point for this application
int _tmain(void)
{
    // TODO: Please replace the sample code below with your own.
    //Console::WriteLine(S"Hello World");

	ConsoleApp::LoggingExample::Main(NULL);
    return 0;
}
