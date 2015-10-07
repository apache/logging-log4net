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
using log4net.Core;
using System.Management.Instrumentation;

namespace log4net.Appender
{
	/// <summary>
	/// Subclass of <see cref="IEvent"/> for events that need to bind to a <see cref="LoggingEvent"/>
	/// </summary>
	/// <remarks>
	/// <para>
	/// If the <see cref="LoggingEvent"/>.<see cref="LoggingEvent.MessageObject"/> is
	/// a <see cref="IWmiBoundEvent"/> then the default behavior of the <see cref="WmiLayout"/>
	/// is to call the <see cref="Bind"/> method passing the <see cref="LoggingEvent"/>.
	/// This allows the event object to capture additional data from the <see cref="LoggingEvent"/>
	/// before it is fired.
	/// </para>
	/// </remarks>
	public interface IWmiBoundEvent : IEvent
	{
		/// <summary>
		/// This method is called before this instance is fired
		/// </summary>
		/// <param name="loggingEvent">the <see cref="LoggingEvent"/> containing the data</param>
		/// <remarks>
		/// <para>
		/// The <see cref="WmiLayout"/> calls this method passing the <see cref="LoggingEvent"/>
		/// object. Implementors should capture any required data from the <paramref name="loggingEvent"/>
		/// and store it in their instance prior to firing to WMI.
		/// </para>
		/// </remarks>
		void Bind(LoggingEvent loggingEvent);
	}
}
