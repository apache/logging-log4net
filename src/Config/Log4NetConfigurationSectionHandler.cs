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

using System.Configuration;
using System.Xml;

namespace log4net.Config
{
	/// <summary>
	/// Class to register for the log4net section of the configuration file
	/// </summary>
	/// <remarks>
	/// The log4net section of the configuration file needs to have a section
	/// handler registered. This is the section handler used. It simply returns
	/// the XML element that is the root of the section.
	/// </remarks>
	/// <example>
	/// Example of registering the log4net section handler :
	/// <code>
	/// &lt;?xml version="1.0" encoding="utf-8" ?&gt;
	/// &lt;configuration&gt;
	///		&lt;configSections&gt;
	///			&lt;section name="log4net" type="log4net.Config.Log4NetConfigurationSectionHandler, log4net" /&gt;
	///		&lt;/configSections&gt;
	///		&lt;log4net&gt;
	///			log4net configuration XML goes here
	///		&lt;/log4net&gt;
	/// &lt;/configuration&gt;
	/// </code>
	/// </example>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class Log4NetConfigurationSectionHandler : IConfigurationSectionHandler
	{
		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="Log4NetConfigurationSectionHandler"/> class.
		/// </summary>
		public Log4NetConfigurationSectionHandler()
		{
		}

		#endregion Public Instance Constructors

		#region Implementation of IConfigurationSectionHandler

		/// <summary>
		/// Parses the configuration section.
		/// </summary>
		/// <param name="parent">The configuration settings in a corresponding parent configuration section.</param>
		/// <param name="configContext">The configuration context when called from the ASP.NET configuration system. Otherwise, this parameter is reserved and is a null reference.</param>
		/// <param name="section">The <see cref="XmlNode" /> for the log4net section.</param>
		/// <returns>The <see cref="XmlNode" /> for the log4net section.</returns>
		public object Create(object parent, object configContext, XmlNode section)
		{
			return section;
		}

		#endregion Implementation of IConfigurationSectionHandler
	}
}
