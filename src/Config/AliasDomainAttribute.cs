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
	/// Assembly level attribute that specifies a domain to alias to this assembly's repository.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>AliasDomainAttribute is obsolete. Use AliasRepositoryAttribute instead of AliasDomainAttribute.</b>
	/// </para>
	/// <para>
	/// An assembly's logger repository is defined by its <see cref="DomainAttribute"/>,
	/// however this can be overidden by an assembly loaded before the target assembly.
	/// </para>
	/// <para>
	/// An assembly can alias another assembly's domain to its repository by
	/// specifying this attribute with the name of the target domain.
	/// </para>
	/// <para>
	/// This attribute can only be specified on the assembly and may be used
	/// as many times as nessasary to alias all the required domains.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	[AttributeUsage(AttributeTargets.Assembly,AllowMultiple=true)]
	[Serializable]
	[Obsolete("Use AliasRepositoryAttribute instead of AliasDomainAttribute")]
	public sealed class AliasDomainAttribute : AliasRepositoryAttribute
	{
		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="AliasDomainAttribute" /> class with 
		/// the specified domain to alias to this assembly's repository.
		/// </summary>
		/// <param name="name">The domain to alias to this assemby's repository.</param>
		public AliasDomainAttribute(string name) : base(name)
		{
		}

		#endregion Public Instance Constructors
	}
}
