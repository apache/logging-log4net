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

using log4net.Core;

namespace log4net.Layout.Pattern
{
	/// <summary>
	/// Mapped Diagnostic pattern converter
	/// </summary>
	/// <author>Nicko Cadell</author>
	internal sealed class MdcPatternConverter : PatternLayoutConverter 
	{
		/// <summary>
		/// To the conversion
		/// </summary>
		/// <param name="writer"><see cref="TextWriter" /> that will receive the formatted result.</param>
		/// <param name="loggingEvent">the event being logged</param>
		/// <returns>the result of converting the pattern</returns>
		override protected void Convert(TextWriter writer, LoggingEvent loggingEvent)
		{
			if (Option != null)
			{
				// Write the value for the specified key
				WriteObject(writer, loggingEvent.Repository, loggingEvent.MappedContext[Option]);
			}
			else
			{
				// Write all the key value pairs
				WriteDictionary(writer, loggingEvent.Repository, loggingEvent.MappedContext);
			}
		}
	}
}
