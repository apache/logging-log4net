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

using log4net.Util;
using log4net.Core;

namespace log4net.Util.PatternStringConverters
{
	/// <summary>
	/// Pattern converter for literal instances in the pattern
	/// </summary>
	/// <author>Nicko Cadell</author>
	internal sealed class NewLinePatternConverter : LiteralPatternConverter, IOptionHandler
	{
		#region Implementation of IOptionHandler

		public void ActivateOptions()
		{
			if (string.Compare(Option, "DOS", true, System.Globalization.CultureInfo.InvariantCulture) == 0)
			{
				Option = "\r\n";
			}
			else if (string.Compare(Option, "UNIX", true, System.Globalization.CultureInfo.InvariantCulture) == 0)
			{
				Option = "\n";
			}
			else
			{
				Option = SystemInfo.NewLine;
			}
		}

		#endregion
	}
}
