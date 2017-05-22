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

namespace log4net.Appender
{
	/// <summary>
	/// Interface for appenders that support bulk logging.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This interface is service locator for creating the <see cref="IAppender"/>
	/// instance. It should be used only when appender is not known or appender
	/// has external dependecies which can only be resovled at runtime.
	/// </para>
	/// </remarks>
	/// <author>Hitesh Chauhan</author>
	public interface IAppenderServiceLocator
	{

		/// <summary>
		/// Gets the instance of an appender by appender name.
		/// </summary>
		/// <param name="appenderName">Name of the appender.</param>
		/// <param name="args">The arguments.</param>
		/// <remarks>
		/// <para>
		/// This method is called to get an instace of an appender by appender name.
		/// </para>
		/// </remarks>
		/// <returns></returns>
		IAppender GetInstance(string appenderName, params string[] args);

	}
}