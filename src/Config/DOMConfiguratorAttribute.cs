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
using System.Reflection;
using System.IO;

using log4net.Util;
using log4net.Repository;
using log4net.Repository.Hierarchy;

namespace log4net.Config
{
	/// <summary>
	/// Assembly level attribute to configure the <see cref="XmlConfigurator"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>AliasDomainAttribute is obsolete. Use AliasRepositoryAttribute instead of AliasDomainAttribute.</b>
	/// </para>
	/// <para>
	/// This attribute may only be used at the assembly scope and can only
	/// be used once per assembly.
	/// </para>
	/// <para>
	/// Use this attribute to configure the <see cref="XmlConfigurator"/>
	/// without calling one of the <see cref="XmlConfigurator.Configure()"/>
	/// methods.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	[AttributeUsage(AttributeTargets.Assembly)]
	[Serializable]
	[Obsolete("Use XmlConfiguratorAttribute instead of DOMConfiguratorAttribute")]
	public sealed class DOMConfiguratorAttribute : XmlConfiguratorAttribute
	{
	}
}
