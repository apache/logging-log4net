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
