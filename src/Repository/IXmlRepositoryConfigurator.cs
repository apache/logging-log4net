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

using System;

namespace log4net.Repository
{
	/// <summary>
	/// Interface used by Xml configurator to configure a <see cref="ILoggerRepository"/>.
	/// </summary>
	/// <remarks>
	/// A <see cref="ILoggerRepository"/> should implement this interface to support
	/// configuration by the <see cref="log4net.Config.XmlConfigurator"/>.
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public interface IXmlRepositoryConfigurator
	{
		/// <summary>
		/// Initialise the log4net system using the specified config
		/// </summary>
		/// <param name="element">the element containing the root of the config</param>
		void Configure(System.Xml.XmlElement element);
	}
}
