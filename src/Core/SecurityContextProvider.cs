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

using log4net.Util;

namespace log4net.Core
{
	/// <summary>
	/// A SecurityContext used by log4net when interacting with protected resources
	/// </summary>
	/// <author>Nicko Cadell</author>
	public class SecurityContextProvider
	{
		/// <summary>
		/// The default provider
		/// </summary>
		private static SecurityContextProvider s_defaultProvider = null;

		/// <summary>
		/// Gets or sets the default SecurityContextProvider
		/// </summary>
		/// <value>
		/// The default SecurityContextProvider
		/// </value>
		/// <remarks>
		/// <para>
		/// ........... HOW LOOKUP PROVIDER
		/// </para>
		/// </remarks>
		public static SecurityContextProvider DefaultProvider
		{
			get 
			{
				if (s_defaultProvider == null)
				{
					lock(typeof(SecurityContextProvider))
					{
						if (s_defaultProvider == null)
						{
							// Lookup the default provider
							s_defaultProvider = CreateDefaultProvider();
						}
					}
				}
				return s_defaultProvider;
			}
			set 
			{
				s_defaultProvider = value;
			}
		}

		private static SecurityContextProvider CreateDefaultProvider()
		{
			return new SecurityContextProvider();
		}

		/// <summary>
		/// Protected default constructor to allow subclassing
		/// </summary>
		protected SecurityContextProvider()
		{
		}

		/// <summary>
		/// Create a SecurityContext for a consumer
		/// </summary>
		/// <param name="consumer">The consumer requesting the SecurityContext</param>
		/// <returns>An impersonation context</returns>
		/// <remarks>
		/// <para>
		/// The default implementation is to return a <see cref="NullSecurityContext"/>.
		/// </para>
		/// </remarks>
		public virtual SecurityContext CreateSecurityContext(object consumer)
		{
			return NullSecurityContext.Instance;
		}
	}
}
