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

namespace SharedModule
{
	/// <summary>
	/// Summary description for Math.
	/// </summary>
	public class Math
	{
		// Create a logger for use in this class
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public Math()
		{
			if (log.IsDebugEnabled) log.Debug("Constructor");
		}

		public int Subtract(int left, int right)
		{
			int result = left - right;
			if (log.IsInfoEnabled) log.Info("" + left + " - " + right + " = " + result);
			return result;
		}
	}
}
