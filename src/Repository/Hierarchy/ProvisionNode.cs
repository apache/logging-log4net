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

namespace log4net.Repository.Hierarchy
{
	/// <summary>
	/// ProvisionNodes are used in the <see cref="Hierarchy" /> when
	/// there is no specified <see cref="Logger" /> for that node.
	/// </summary>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	internal class ProvisionNode : System.Collections.ArrayList
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ProvisionNode" /> class 
		/// with the specified child logger.
		/// </summary>
		/// <param name="log">A child logger to add to this node.</param>
		internal ProvisionNode(Logger log) : base()
		{
			this.Add(log);
		}
	}
}
