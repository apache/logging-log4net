#region Copyright & License
//
// Copyright 2001-2004 The Apache Software Foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
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

using log4net.Appender;

namespace log4net.Core
{
	/// <summary>
	/// Interface for attaching appenders to objects.
	/// </summary>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public interface IAppenderAttachable
	{
		/// <summary>
		/// Attaches an appender.
		/// </summary>
		/// <param name="newAppender">The appender to add.</param>
		void AddAppender(IAppender newAppender);

		/// <summary>
		/// Gets all attached appenders.
		/// </summary>
		/// <returns>
		/// A collection of attached appenders, or <c>null</c> if there
		/// are no attached appenders.
		/// </returns>
		AppenderCollection Appenders {get;}

		/// <summary>
		/// Gets an attached appender with the specified name.
		/// </summary>
		/// <param name="name">The name of the appender to get.</param>
		/// <returns>
		/// The appender with the name specified, or <c>null</c> if no appender with the
		/// specified name is found.
		/// </returns>
		IAppender GetAppender(string name);

		/// <summary>
		/// Removes all attached appenders.
		/// </summary>
		void RemoveAllAppenders();

		/// <summary>
		/// Removes the specified appender from the list of attached appenders.
		/// </summary>
		/// <param name="appender">The appender to remove.</param>
		void RemoveAppender(IAppender appender);

		/// <summary>
		/// Removes the appender with the specified name from the list of appenders.
		/// </summary>
		/// <param name="name">The name of the appender to remove.</param>
		void RemoveAppender(string name);   	
	}
}
