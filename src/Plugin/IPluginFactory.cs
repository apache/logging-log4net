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

namespace log4net.Plugin
{
	/// <summary>
	/// Interface used to create plugins.
	/// </summary>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public interface IPluginFactory
	{
		/// <summary>
		/// Creates the plugin object.
		/// </summary>
		IPlugin CreatePlugin();
	}
}
