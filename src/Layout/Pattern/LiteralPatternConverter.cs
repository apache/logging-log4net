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
using log4net.Util;

namespace log4net.Layout.Pattern
{
	/// <summary>
	/// Pattern converter for literal instances in the pattern
	/// </summary>
	/// <author>Nicko Cadell</author>
	internal class LiteralPatternConverter : PatternLayoutConverter 
	{
		/// <summary>
		/// the next patter converter in the chain
		/// </summary>
		public override PatternConverter SetNext(PatternConverter pc)
		{
			if (pc is LiteralPatternConverter)
			{
				// Combine the two adjacent literals together
				Option = Option + ((LiteralPatternConverter)pc).Option;

				// We are the next converter now
				return this;
			}

			return base.SetNext(pc);
		}

		/// <summary>
		/// Override the formatting behavior to ignore the FormattingInfo
		/// because we have a literal instead.
		/// </summary>
		/// <param name="writer">the writer to write to</param>
		/// <param name="state">the event being logged</param>
		override public void Format(TextWriter writer, object state) 
		{
			writer.Write(Option);
		}
	
		/// <summary>
		/// Convert this pattern into the rendered message
		/// </summary>
		/// <param name="writer"><see cref="TextWriter" /> that will receive the formatted result.</param>
		/// <param name="loggingEvent">the event being logged</param>
		/// <returns>the literal</returns>
		override protected void Convert(TextWriter writer, LoggingEvent loggingEvent) 
		{
			throw new InvalidOperationException("Should never get here because of the overridden Format method");
		}
	}
}
