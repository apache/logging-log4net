#region Copyright & License
//
// Copyright 2001-2005 The Apache Software Foundation
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
	/// <para>This string has embedded patterns that are resolved and expanded
	/// when the string is formatted.</para>
	/// <para>This class functions similarly to the <see cref="log4net.Layout.PatternLayout"/>
	/// in that it accepts a pattern and renders it to a string. Unlike the 
	/// <see cref="log4net.Layout.PatternLayout"/> however the <c>PatternString</c>
	/// does does not render properties of a specific <see cref="LoggingEvent"/> but
	/// of the process in general.</para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	public class PatternString : IOptionHandler
	{
		#region Static Fields

		/// <summary>
		/// Internal map of converter identifiers to converter types.
		/// </summary>
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

		/// <summary>
		/// patterns defined on this PatternString only
		/// </summary>
		private Hashtable m_instanceRulesRegistry = new Hashtable();

		#endregion

		#region Static Constructor

		/// <summary>
		/// Initialize the global registry
		/// </summary>
		static PatternString()
		{
			s_globalRulesRegistry = new Hashtable(10);

			s_globalRulesRegistry.Add("appdomain", typeof(AppDomainPatternConverter));
			s_globalRulesRegistry.Add("date", typeof(DatePatternConverter));
#if !NETCF
			s_globalRulesRegistry.Add("env", typeof(EnvironmentPatternConverter));
#endif
			s_globalRulesRegistry.Add("identity", typeof(IdentityPatternConverter));
			s_globalRulesRegistry.Add("literal", typeof(LiteralPatternConverter));
			s_globalRulesRegistry.Add("newline", typeof(NewLinePatternConverter));
			s_globalRulesRegistry.Add("processid", typeof(ProcessIdPatternConverter));
			s_globalRulesRegistry.Add("random", typeof(RandomStringPatternConverter));
			s_globalRulesRegistry.Add("username", typeof(UserNamePatternConverter));
			s_globalRulesRegistry.Add("property", typeof(PropertyPatternConverter));

			s_globalRulesRegistry.Add("utcdate", typeof(UtcDatePatternConverter));
			s_globalRulesRegistry.Add("utcDate", typeof(UtcDatePatternConverter));
			s_globalRulesRegistry.Add("UtcDate", typeof(UtcDatePatternConverter));
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
		/// Constructs a PatternString
		/// </summary>
		/// <param name="pattern">The pattern to use with this PatternString</param>
		public PatternString(string pattern)
		{
			m_pattern = pattern;
			ActivateOptions();
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
			set { m_pattern = value; }
		}

		#region Implementation of IOptionHandler

		/// <summary>
		/// Initialize object options
		/// </summary>
		/// <remarks>
		/// <para>
		/// This is part of the <see cref="IOptionHandler"/> delayed object
		/// activation scheme. The <see cref="ActivateOptions"/> method must 
		/// be called on this object after the configuration properties have
		/// been set. Until <see cref="ActivateOptions"/> is called this
		/// object is in an undefined state and must not be used. 
		/// </para>
		/// <para>
		/// If any of the configuration properties are modified then 
		/// <see cref="ActivateOptions"/> must be called again.
		/// </para>
		/// </remarks>
		virtual public void ActivateOptions() 
		{
			m_head = CreatePatternParser(m_pattern).Parse();
		}

		#endregion

		/// <summary>
		/// Returns PatternParser used to parse the conversion string. Subclasses
		/// may override this to return a subclass of PatternParser which recognize
		/// custom conversion characters.
		/// </summary>
		/// <param name="pattern">the pattern to parse</param>
		/// <returns></returns>
		private PatternParser CreatePatternParser(string pattern) 
		{
			PatternParser patternParser = new PatternParser(pattern);

			// Add all the builtin patterns
			foreach(DictionaryEntry entry in s_globalRulesRegistry)
			{
				patternParser.PatternConverters.Add(entry.Key, entry.Value);
			}
			// Add the instance patterns
			foreach(DictionaryEntry entry in m_instanceRulesRegistry)
			{
				patternParser.PatternConverters[entry.Key] = entry.Value;
			}

			return patternParser;
		}
  
		/// <summary>
		/// Produces a formatted string as specified by the conversion pattern.
		/// </summary>
		/// <param name="writer">The TextWriter to write the formatted event to</param>
		public void Format(TextWriter writer) 
		{
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}

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

		/// <summary>
		/// Add a converter to this PatternString
		/// </summary>
		/// <param name="converterInfo">the converter info</param>
		/// <remarks>
		/// This version of the method is used by the configurator.
		/// Programmatic users should use the alternative <see cref="AddConverter(string,Type)"/> method.
		/// </remarks>
		public void AddConverter(ConverterInfo converterInfo)
		{
			AddConverter(converterInfo.Name, converterInfo.Type);
		}

		/// <summary>
		/// Add a converter to this PatternString
		/// </summary>
		/// <param name="name">the name of the conversion pattern for this converter</param>
		/// <param name="type">the type of the converter</param>
		public void AddConverter(string name, Type type)
		{
			if (name == null) throw new ArgumentNullException("name");
			if (type == null) throw new ArgumentNullException("type");

			if (!typeof(PatternConverter).IsAssignableFrom(type))
			{
				throw new ArgumentException("The converter type specified ["+type+"] must be a subclass of log4net.Util.PatternConverter", "type");
			}
			m_instanceRulesRegistry[name] = type;
		}

		/// <summary>
		/// Wrapper class used to map converter names to converter types
		/// </summary>
		public sealed class ConverterInfo
		{
			private string m_name;
			private Type m_type;

			/// <summary>
			/// default constructor
			/// </summary>
			public ConverterInfo()
			{
			}

			/// <summary>
			/// Gets or sets the name of the conversion pattern
			/// </summary>
			public string Name
			{
				get { return m_name; }
				set { m_name = value; }
			}

			/// <summary>
			/// Gets or sets the type of the converter
			/// </summary>
			public Type Type
			{
				get { return m_type; }
				set { m_type = value; }
			}
		}
	}
}
