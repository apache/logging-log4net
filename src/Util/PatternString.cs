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
using System.Collections;
using System.IO;
using System.Text;

using log4net.Util;
using log4net.Util.PatternStringConverters;
using log4net.Core;

namespace log4net.Util
{
	/// <summary>
	/// This class implements a patterned string.
	/// </summary>
	/// <remarks>
	/// This string has embeded patterns that are resolved and expanded
	/// when the string is formatted.
	/// </remarks>
	/// <author>Nicko Cadell</author>
	public class PatternString : IOptionHandler
	{
		#region Static Fields

		/// <summary>
		/// Internal map of converter identifiers to converter types.
		/// </summary>
		/// <remarks>
		/// This static map is overriden by the m_converterRegistry instance map
		/// </remarks>
		private static Hashtable s_globalRulesRegistry;

		#endregion Static Fields

		#region Member Variables
    
		/// <summary>
		/// the pattern
		/// </summary>
		private string m_pattern;
  
		/// <summary>
		/// the head of the pattern converter chain
		/// </summary>
		private PatternConverter m_head;

		#endregion

		#region Static Constructor

		/// <summary>
		/// Initialise the global registry
		/// </summary>
		static PatternString()
		{
			s_globalRulesRegistry = new Hashtable(35);

			s_globalRulesRegistry.Add("appdomain", typeof(AppDomainPatternConverter));
			s_globalRulesRegistry.Add("date", typeof(DatePatternConverter));
#if !NETCF
			s_globalRulesRegistry.Add("env", typeof(EnvironmentPatternConverter));
#endif
			s_globalRulesRegistry.Add("identity", typeof(IdentityPatternConverter));
			s_globalRulesRegistry.Add("literal", typeof(LiteralPatternConverter));
			s_globalRulesRegistry.Add("newline", typeof(NewLinePatternConverter));
			s_globalRulesRegistry.Add("processid", typeof(ProcessIdPatternConverter));
			s_globalRulesRegistry.Add("username", typeof(UserNamePatternConverter));
		}

		#endregion Static Constructor

		#region Constructors

		/// <summary>
		/// Constructs a PatternString
		/// </summary>
		public PatternString()
		{
		}

		/// <summary>
		/// Constructs a PatternString using the supplied conversion pattern
		/// </summary>
		/// <param name="pattern">the pattern to use</param>
		public PatternString(string pattern) 
		{
			m_pattern = pattern;
			m_head = CreatePatternParser((pattern == null) ? "" : pattern).Parse();
		}

		#endregion
  
		/// <summary>
		/// The <b>ConversionPattern</b> option. This is the string which
		/// controls formatting and consists of a mix of literal content and
		/// conversion specifiers.
		/// </summary>
		public string ConversionPattern
		{
			get { return m_pattern;	}
			set
			{
				m_pattern = value;
				m_head = CreatePatternParser(m_pattern).Parse();
			}
		}

		/// <summary>
		/// Returns PatternParser used to parse the conversion string. Subclasses
		/// may override this to return a subclass of PatternParser which recognize
		/// custom conversion characters.
		/// </summary>
		/// <param name="pattern">the pattern to parse</param>
		/// <returns></returns>
		virtual protected PatternParser CreatePatternParser(string pattern) 
		{
			PatternParser patternParser = new PatternParser(pattern);

			// Add all the builtin patterns
			foreach(DictionaryEntry entry in s_globalRulesRegistry)
			{
				patternParser.ConverterRegistry.Add(entry.Key, entry.Value);
			}

			return patternParser;
		}
  
		#region Implementation of IOptionHandler

		/// <summary>
		/// Does not do anything as options become effective immediately.
		/// </summary>
		virtual public void ActivateOptions() 
		{
			// nothing to do.
		}

		#endregion

		/// <summary>
		/// Produces a formatted string as specified by the conversion pattern.
		/// </summary>
		/// <param name="writer">The TextWriter to write the formatted event to</param>
		public void Format(TextWriter writer) 
		{
			PatternConverter c = m_head;

			// loop through the chain of pattern converters
			while(c != null) 
			{
				c.Format(writer, null);
				c = c.Next;
			}
		}

		/// <summary>
		/// Format the pattern as a string
		/// </summary>
		/// <returns>the pattern formatted as a string</returns>
		public string Format() 
		{
			StringWriter writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
			Format(writer);
			return writer.ToString();
		}

	}
}
