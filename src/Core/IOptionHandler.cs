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

namespace log4net.Core
{
	/// <summary>
	/// A string based interface to configure components.
	/// </summary>
	/// <author>Nicko Cadell</author>
	public interface IOptionHandler
	{
		/// <summary>
		/// Activate the options that were previously set with calls to option setters.
		/// </summary>
		/// <remarks>
		/// This allows to defer activation of the options until all
		/// options have been set. This is required for components which have
		/// related options that remain ambiguous until all are set.
		/// </remarks>
		void ActivateOptions();
	}
}
