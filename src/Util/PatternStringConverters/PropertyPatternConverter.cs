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
using System.Collections;

using log4net.Core;
using log4net.Util;
using log4net.Repository;

namespace log4net.Util.PatternStringConverters
{
	/// <summary>
	/// Property pattern converter
	/// </summary>
	/// <remarks>
	/// <para>
	/// This pattern converter reads the thread and global properties.
	/// The thread properties take priority over global properties.
	/// See <see cref="ThreadContext.Properties"/> for details of the 
	/// thread properties. See <see cref="GlobalContext.Properties"/> for
	/// details of the global properties.
	/// </para>
	/// <para>
	/// If the <see cref="PatternConverter.Option"/> is specified then that will be used to
	/// lookup a single property. If no <see cref="PatternConverter.Option"/> is specified
	/// then all properties will be dumped as a list of key value pairs.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	internal sealed class PropertyPatternConverter : PatternConverter 
	{
		/// <summary>
		/// Write the property or specified properties as required
		/// </summary>
		/// <param name="writer"><see cref="TextWriter" /> that will receive the formatted result.</param>
		/// <param name="state">reserved</param>
		/// <returns>the result of converting the pattern</returns>
		override protected void Convert(TextWriter writer, object state) 
		{
			CompositeProperties compositeProperties = new CompositeProperties();

			compositeProperties.Add(ThreadContext.Properties.GetProperties());
			// TODO: Add Repository Properties
			compositeProperties.Add(GlobalContext.Properties.GetReadOnlyProperties());

			if (Option != null)
			{
				// Write the value for the specified key
				WriteObject(writer, null, compositeProperties[Option]);
			}
			else
			{
				// Write all the key value pairs
				WriteDictionary(writer, null, compositeProperties.Flatten());
			}
		}
	}
}
