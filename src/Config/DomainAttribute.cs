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

namespace log4net.Config
{
	/// <summary>
	/// Assembly level attribute that specifies the logging domain for the assembly.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>DomainAttribute is obsolete. Use RepositoryAttribute instead of DomainAttribute.</b>
	/// </para>
	/// <para>
	/// Assemblies are mapped to logging domains. Each domain has its own
	/// logging repository. This attribute specified on the assembly controls
	/// the configuration of the domain. The <see cref="RepositoryAttribute.Name"/> property specifies the name
	/// of the domain that this assembly is a part of. The <see cref="RepositoryAttribute.RepositoryType"/>
	/// specifies the type of the repository objects to create for the domain. If 
	/// this attribute is not specified and a <see cref="RepositoryAttribute.Name"/> is not specified
	/// then the assembly will be part of the default shared logging domain.
	/// </para>
	/// <para>
	/// This attribute can only be specified on the assembly and may only be used
	/// once per assembly.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	[AttributeUsage(AttributeTargets.Assembly)]
	[Serializable]
	[Obsolete("Use RepositoryAttribute instead of DomainAttribute")]
	public sealed class DomainAttribute : RepositoryAttribute
	{
		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="DomainAttribute" /> class.
		/// </summary>
		public DomainAttribute() : base()
		{
		}

		/// <summary>
		/// Initialise a new instance of the <see cref="DomainAttribute" /> class 
		/// with the name of the domain.
		/// </summary>
		/// <param name="name">The name of the domain.</param>
		public DomainAttribute(string name) : base(name)
		{
		}

		#endregion Public Instance Constructors
	}
}
