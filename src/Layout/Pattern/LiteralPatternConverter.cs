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
		/// Override the formatting behaviour to ignore the FormattingInfo
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
			throw new InvalidOperationException("Should never get here because of the overriden Format method");
		}
	}
}
