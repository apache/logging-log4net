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
using System.Text;
using System.IO;

using log4net.Util;

namespace log4net.Util.PatternStringConverters
{
	/// <summary>
	/// Converter to include event identity
	/// </summary>
	/// <author>Nicko Cadell</author>
	internal sealed class IdentityPatternConverter : PatternConverter 
	{
		/// <summary>
		/// Convert the pattern to the rendered message
		/// </summary>
		/// <param name="writer">the writer to write to</param>
		/// <param name="state">null, state is not set</param>
		override protected void Convert(TextWriter writer, object state) 
		{
#if (NETCF || SSCLI)
			// On compact framework there's no notion of current thread principals
			writer.Write( "NOT AVAILABLE" );
#else
			try
			{
				if (System.Threading.Thread.CurrentPrincipal != null && 
					System.Threading.Thread.CurrentPrincipal.Identity != null &&
					System.Threading.Thread.CurrentPrincipal.Identity.Name != null)
				{
					writer.Write( System.Threading.Thread.CurrentPrincipal.Identity.Name );
				}
			}
			catch(System.Security.SecurityException)
			{
				// This security exception will occur if the caller does not have 
				// some undefined set of SecurityPermission flags.
				LogLog.Debug("IdentityPatternConverter: Security exception while trying to get current thread principal. Error Ignored.");

				writer.Write( "NOT AVAILABLE" );
			}
#endif
		}
	}
}
