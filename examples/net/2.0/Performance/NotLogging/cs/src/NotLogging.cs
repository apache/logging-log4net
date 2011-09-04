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

namespace NotLogging 
{
	using System;
	using System.Collections;

	using log4net;
	using log4net.Appender;
	using log4net.Layout;
	using log4net.Repository;
	using log4net.Repository.Hierarchy;

	public class NotLogging 
	{
		#region Init Code

		private static int WARM_UP_CYCLES = 10000;

		static readonly ILog SHORT_LOG = LogManager.GetLogger("A0123456789");
		static readonly ILog MEDIUM_LOG= LogManager.GetLogger("A0123456789.B0123456789");
		static readonly ILog LONG_LOG  = LogManager.GetLogger("A0123456789.B0123456789.C0123456789");

		static readonly ILog INEXISTENT_SHORT_LOG = LogManager.GetLogger("I0123456789");
		static readonly ILog INEXISTENT_MEDIUM_LOG= LogManager.GetLogger("I0123456789.B0123456789");
		static readonly ILog INEXISTENT_LONG_LOG  = LogManager.GetLogger("I0123456789.B0123456789.C0123456789");


		static readonly ILog[] LOG_ARRAY = new ILog[] {
														  SHORT_LOG, 
														  MEDIUM_LOG, 
														  LONG_LOG, 
														  INEXISTENT_SHORT_LOG,
														  INEXISTENT_MEDIUM_LOG,
														  INEXISTENT_LONG_LOG};

		static readonly TimedTest[] TIMED_TESTS = new TimedTest[] {
																	  new SimpleMessage_Bare(),
																	  new SimpleMessage_Array(),
																	  new SimpleMessage_MethodGuard_Bare(),
																	  new SimpleMessage_LocalGuard_Bare(),
																	  new ComplexMessage_Bare(),
																	  new ComplexMessage_Array(),
																	  new ComplexMessage_MethodGuard_Bare(),
																	  new ComplexMessage_MethodGuard_Array(),
																	  new ComplexMessage_MemberGuard_Bare(),
																	  new ComplexMessage_LocalGuard_Bare()};

		private static void Usage() 
		{
			System.Console.WriteLine(
				"Usage: NotLogging <true|false> <runLength>" + Environment.NewLine +
				"\t true indicates shipped code" + Environment.NewLine +
				"\t false indicates code in development" + Environment.NewLine +
				"\t runLength is an int representing the run length of loops"  + Environment.NewLine +
				"\t We suggest that runLength be at least 1000000 (1 million).");
			Environment.Exit(1);
		}


		/// <summary>
		/// Program wide initialization method
		/// </summary>
		/// <param name="args"></param>
		private static int ProgramInit(String[] args) 
		{
			int runLength = 0;

			try 
			{
				runLength = int.Parse(args[1]);      
			}
			catch(Exception e) 
			{
				System.Console.Error.WriteLine(e);
				Usage();
			}      
    
			ConsoleAppender appender = new ConsoleAppender();
			appender.Layout = new SimpleLayout();
			((SimpleLayout)appender.Layout).ActivateOptions();
			appender.ActivateOptions();
	    
			if("false" == args[0]) 
			{
				// nothing to do
			} 
			else if ("true" == args[0]) 
			{
				System.Console.WriteLine("Flagging as shipped code.");
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
		static void Main(string[] argv) 
		{
			if (System.Diagnostics.Debugger.IsAttached) 
			{
				WARM_UP_CYCLES = 0;
				argv = new string[] { "false", "2" };
			}
			if(argv.Length != 2) 
			{
				Usage();
			}

			int runLength = ProgramInit(argv);

			System.Console.WriteLine();
			System.Console.Write("Warming Up...");

			if (WARM_UP_CYCLES > 0) 
			{
				foreach(ILog logger in LOG_ARRAY) 
				{
					foreach(TimedTest timedTest in TIMED_TESTS) 
					{
						timedTest.Run(logger, WARM_UP_CYCLES);
					}
				}
			}
			System.Console.WriteLine("Done");
			System.Console.WriteLine();

			// Calculate maximum description length
			int maxDescLen = 0;
			foreach(TimedTest timedTest in TIMED_TESTS) 
			{
				maxDescLen = Math.Max(maxDescLen, timedTest.Description.Length);
			}

			string formatString = "{0,-"+(maxDescLen+1)+"} {1,9:G} ticks. Log: {2}";
			double delta;

			ArrayList averageData = new ArrayList();

			foreach(TimedTest timedTest in TIMED_TESTS) 
			{
				double total = 0;
				foreach(ILog logger in LOG_ARRAY) 
				{
					delta = timedTest.Run(logger, runLength);
					System.Console.WriteLine(string.Format(formatString, timedTest.Description, delta, ((Logger)logger.Logger).Name));

					total += delta;
				}
				System.Console.WriteLine();

				averageData.Add(new object[] { timedTest, total/((double)LOG_ARRAY.Length) });
			}
			System.Console.WriteLine();
			System.Console.WriteLine("Averages:");
			System.Console.WriteLine();

			foreach(object[] pair in averageData) 
			{
				string avgFormatString = "{0,-"+(maxDescLen+1)+"} {1,9:G} ticks.";
				System.Console.WriteLine(string.Format(avgFormatString, ((TimedTest)pair[0]).Description, ((double)pair[1])));
			}
		}
	}

	abstract class TimedTest 
	{
		abstract public double Run(ILog log, long runLength);
		abstract public string Description {get;}
	}

	#region Tests calling Debug(string)

	class SimpleMessage_Bare : TimedTest 
	{
		override public double Run(ILog log, long runLength) 
		{
			DateTime before = DateTime.Now;
			for(int i = 0; i < runLength; i++) 
			{
				log.Debug("msg");
			}
			DateTime after = DateTime.Now;
			TimeSpan diff = after - before;
			return ((double)diff.Ticks)/((double)runLength);
		}

		override public string Description 
		{
			get { return "log.Debug(\"msg\");"; }
		}
	}
	class ComplexMessage_MethodGuard_Bare : TimedTest 
	{
		override public double Run(ILog log, long runLength) 
		{
			DateTime before = DateTime.Now;
			for(int i = 0; i < runLength; i++) 
			{
				if(log.IsDebugEnabled) 
				{
					log.Debug("msg" + i + "msg");
				}
			}    
			DateTime after = DateTime.Now;
			TimeSpan diff = after - before;
			return ((double)diff.Ticks)/((double)runLength);
		}

		override public string Description 
		{
			get { return "if(log.IsDebugEnabled) log.Debug(\"msg\" + i + \"msg\");"; }
		}
	}
	class ComplexMessage_Bare : TimedTest 
	{
		override public double Run(ILog log, long runLength) 
		{
			DateTime before = DateTime.Now;
			for(int i = 0; i < runLength; i++) 
			{
				log.Debug("msg" + i + "msg");
			}
			DateTime after = DateTime.Now;
			TimeSpan diff = after - before;
			return ((double)diff.Ticks)/((double)runLength);
		}

		override public string Description 
		{
			get { return "log.Debug(\"msg\" + i + \"msg\");"; }
		}
	}

	#endregion

	#region Tests calling Debug(new object[] { ... })

	class SimpleMessage_Array : TimedTest 
	{
		override public double Run(ILog log, long runLength) 
		{
			DateTime before = DateTime.Now;
			for(int i = 0; i < runLength; i++) 
			{
				log.Debug(new object[] { "msg" });
			}
			DateTime after = DateTime.Now;
			TimeSpan diff = after - before;
			return ((double)diff.Ticks)/((double)runLength);
		}

		override public string Description 
		{
			get { return "log.Debug(new object[] { \"msg\" });"; }
		}
	}
	class ComplexMessage_MethodGuard_Array : TimedTest 
	{
		override public double Run(ILog log, long runLength) 
		{
			DateTime before = DateTime.Now;
			for(int i = 0; i < runLength; i++) 
			{
				if(log.IsDebugEnabled) 
				{
					log.Debug(new object[] { "msg" , i , "msg" });
				}
			}    
			DateTime after = DateTime.Now;
			TimeSpan diff = after - before;
			return ((double)diff.Ticks)/((double)runLength);
		}

		override public string Description 
		{
			get { return "if(log.IsDebugEnabled) log.Debug(new object[] { \"msg\" , i , \"msg\" });"; }
		}
	}
	class ComplexMessage_Array : TimedTest 
	{
		override public double Run(ILog log, long runLength) 
		{
			DateTime before = DateTime.Now;
			for(int i = 0; i < runLength; i++) 
			{
				log.Debug(new object[] { "msg" , i , "msg" });
			}
			DateTime after = DateTime.Now;
			TimeSpan diff = after - before;
			return ((double)diff.Ticks)/((double)runLength);
		}

		override public string Description 
		{
			get { return "log.Debug(new object[] { \"msg\" , i , \"msg\" });"; }
		}
	}

	#endregion

	#region Tests calling Debug(string) (using class members)

	class ComplexMessage_MemberGuard_Bare : TimedTest 
	{
		override public double Run(ILog log, long runLength) 
		{
			return (new Impl(log)).Run(runLength);
		}

		override public string Description 
		{
			get { return "if(m_isEnabled) m_log.Debug(\"msg\" + i + \"msg\");"; }
		}

		class Impl 
		{
			private readonly ILog m_log;
			private readonly bool m_isEnabled;

			public Impl(ILog log) 
			{
				m_log = log;
				m_isEnabled = m_log.IsDebugEnabled;
			}

			public double Run(long runLength) 
			{

				DateTime before = DateTime.Now;
				for(int i = 0; i < runLength; i++) 
				{
					if(m_isEnabled) 
					{
						m_log.Debug("msg" + i + "msg");
					}
				}    
				DateTime after = DateTime.Now;
				TimeSpan diff = after - before;
				return ((double)diff.Ticks)/((double)runLength);
			}
		}
	}
	class SimpleMessage_LocalGuard_Bare : TimedTest 
	{
		override public double Run(ILog log, long runLength) 
		{
			bool isEnabled = log.IsDebugEnabled;

			DateTime before = DateTime.Now;
			for(int i = 0; i < runLength; i++) 
			{
				if (isEnabled) log.Debug("msg");
			}
			DateTime after = DateTime.Now;
			TimeSpan diff = after - before;
			return ((double)diff.Ticks)/((double)runLength);
		}

		override public string Description 
		{
			get { return "if (isEnabled) log.Debug(\"msg\");"; }
		}
	}
	class SimpleMessage_MethodGuard_Bare : TimedTest 
	{
		override public double Run(ILog log, long runLength) 
		{
			DateTime before = DateTime.Now;
			for(int i = 0; i < runLength; i++) 
			{
				if (log.IsDebugEnabled) log.Debug("msg");
			}
			DateTime after = DateTime.Now;
			TimeSpan diff = after - before;
			return ((double)diff.Ticks)/((double)runLength);
		}

		override public string Description 
		{
			get { return "if (log.IsDebugEnabled) log.Debug(\"msg\");"; }
		}
	}
	class ComplexMessage_LocalGuard_Bare : TimedTest 
	{
		override public double Run(ILog log, long runLength) 
		{
			bool isEnabled = log.IsDebugEnabled;

			DateTime before = DateTime.Now;
			for(int i = 0; i < runLength; i++) 
			{
				if(isEnabled) log.Debug("msg" + i + "msg");
			}    
			DateTime after = DateTime.Now;
			TimeSpan diff = after - before;
			return ((double)diff.Ticks)/((double)runLength);
		}

		override public string Description 
		{
			get { return "if (isEnabled) log.Debug(\"msg\" + i + \"msg\");"; }
		}
	}
	#endregion 

}
