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
using System.Collections;
using System.IO;
using System.Text;

using log4net.Core;
using log4net.Layout.Pattern;
using log4net.Util;

namespace log4net.Layout
{
	/// <summary>
	/// A flexible layout configurable with pattern string.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The goal of this class is to <see cref="PatternLayout.Format"/> a 
	/// <see cref="LoggingEvent"/> as a string. The results
	/// depend on the <i>conversion pattern</i>.
	/// </para>
	/// <para>
	/// The conversion pattern is closely related to the conversion
	/// pattern of the printf function in C. A conversion pattern is
	/// composed of literal text and format control expressions called
	/// <i>conversion specifiers</i>.
	/// </para>
	/// <para>
	/// <i>You are free to insert any literal text within the conversion
	/// pattern.</i>
	/// </para>
	/// <para>
	/// Each conversion specifier starts with a percent sign (%) and is
	/// followed by optional <i>format modifiers</i> and a <i>conversion
	/// character</i>. The conversion character specifies the type of
	/// data, e.g. logger, level, date, thread name. The format
	/// modifiers control such things as field width, padding, left and
	/// right justification. The following is a simple example.
	/// </para>
	/// <para>
	/// Let the conversion pattern be <b>"%-5level [%thread]: %message%newline"</b> and assume
	/// that the log4net environment was set to use a PatternLayout. Then the
	/// statements
	/// </para>
	/// <code>
	/// ILog log = LogManager.GetLogger(typeof(TestApp));
	/// log.Debug("Message 1");
	/// log.Warn("Message 2");   
	/// </code>
	/// <para>would yield the output</para>
	/// <code>
	/// DEBUG [main]: Message 1
	/// WARN  [main]: Message 2  
	/// </code>
	/// <para>
	/// Note that there is no explicit separator between text and
	/// conversion specifiers. The pattern parser knows when it has reached
	/// the end of a conversion specifier when it reads a conversion
	/// character. In the example above the conversion specifier
	/// <b>%-5level</b> means the level of the logging event should be left
	/// justified to a width of five characters.
	/// </para>
	/// <para>
	/// The recognized conversion characters are :
	/// </para>
	/// <list type="table">
	///     <listheader>
	///         <term>Conversion Character</term>
	///         <description>Effect</description>
	///     </listheader>
	///     <item>
	///         <term>a</term>
	///         <description>Equivalent to <b>appdomain</b></description>
	///     </item>
	///     <item>
	///         <term>appdomain</term>
	///         <description>
	///				Used to output the frienly name of the AppDomain where the 
	///				logging event was generated. 
	///         </description>
	///     </item>
	///     <item>
	///         <term>c</term>
	///         <description>Equivalent to <b>logger</b></description>
	///     </item>
	///     <item>
	///         <term>C</term>
	///         <description>Equivalent to <b>type</b></description>
	///     </item>
	///     <item>
	///         <term>class</term>
	///         <description>Equivalent to <b>type</b></description>
	///     </item>
	///     <item>
	///         <term>d</term>
	///         <description>Equivalent to <b>date</b></description>
	///     </item>
	///     <item>
	///			<term>date</term> 
	///			<description>
	/// 			<para>
	/// 			Used to output the date of the logging event. The date conversion 
	/// 			specifier may be followed by a <i>date format specifier</i> enclosed 
	/// 			between braces. For example, <b>%date{HH:mm:ss,fff}</b> or
	/// 			<b>%date{dd MMM yyyy HH:mm:ss,fff}</b>.  If no date format specifier is 
	/// 			given then ISO8601 format is
	/// 			assumed (<see cref="log4net.DateFormatter.Iso8601DateFormatter"/>).
	/// 			</para>
	/// 			<para>
	/// 			The date format specifier admits the same syntax as the
	/// 			time pattern string of the <see cref="DateTime.ToString"/>.
	/// 			</para>
	/// 			<para>
	/// 			For better results it is recommended to use the log4net date
	/// 			formatters. These can be specified using one of the strings
	/// 			"ABSOLUTE", "DATE" and "ISO8601" for specifying 
	/// 			<see cref="log4net.DateFormatter.AbsoluteTimeDateFormatter"/>, 
	/// 			<see cref="log4net.DateFormatter.DateTimeDateFormatter"/> and respectively 
	/// 			<see cref="log4net.DateFormatter.Iso8601DateFormatter"/>. For example, 
	/// 			<b>%date{ISO8601}</b> or <b>%date{ABSOLUTE}</b>.
	/// 			</para>
	/// 			<para>
	/// 			These dedicated date formatters perform significantly
	/// 			better than <see cref="DateTime.ToString(string)"/>.
	/// 			</para>
	///			</description>
	///		</item>
	///     <item>
	///         <term>F</term>
	///         <description>Equivalent to <b>file</b></description>
	///     </item>
	///		<item>
	///			<term>file</term>
	///			<description>
	///				<para>
	///				Used to output the file name where the logging request was
	///				issued.
	///				</para>
	///				<para>
	///				<b>WARNING</b> Generating caller location information is
	///				extremely slow. It's use should be avoided unless execution speed
	///				is not an issue.
	///				</para>
	/// 			<para>
	/// 			See the note below on the availablity of caller location information.
	/// 			</para>
	///			</description>
	///		</item>
	///		<item>
	///			<term>identity</term>
	///			<description>
	///				<para>
	///				Used to output the user name for the currently active user
	///				(Principal.Identity.Name).
	///				</para>
	///				<para>
	///				<b>WARNING</b> Generating caller information is
	///				extremely slow. It's use should be avoided unless execution speed
	///				is not an issue.
	///				</para>
	///			</description>
	///		</item>
	///     <item>
	///         <term>l</term>
	///         <description>Equivalent to <b>location</b></description>
	///     </item>
	///     <item>
	///         <term>L</term>
	///         <description>Equivalent to <b>line</b></description>
	///     </item>
	///		<item>
	///			<term>location</term>
	///			<description>
	/// 			<para>
	/// 			Used to output location information of the caller which generated
	/// 			the logging event.
	/// 			</para>
	/// 			<para>
	/// 			The location information depends on the CLI implementation but
	/// 			usually consists of the fully qualified name of the calling
	/// 			method followed by the callers source the file name and line
	/// 			number between parentheses.
	/// 			</para>
	/// 			<para>
	/// 			The location information can be very useful. However, it's
	/// 			generation is <b>extremely</b> slow. It's use should be avoided
	/// 			unless execution speed is not an issue.
	/// 			</para>
	/// 			<para>
	/// 			See the note below on the availablity of caller location information.
	/// 			</para>
	///			</description>
	///		</item>
	///		<item>
	///			<term>level</term>
	///			<description>
	/// 			<para>
	/// 			Used to output the level of the logging event.
	/// 			</para>
	///			</description>
	///		</item>
	///		<item>
	///			<term>line</term>
	///			<description>
	///				<para>
	///				Used to output the line number from where the logging request
	///				was issued.
	///				</para>
	///				<para>
	///				<b>WARNING</b> Generating caller location information is
	///				extremely slow. It's use should be avoided unless execution speed
	///				is not an issue.
	///				</para>
	/// 			<para>
	/// 			See the note below on the availablity of caller location information.
	/// 			</para>
	///			</description>
	///		</item>
	///     <item>
	///         <term>logger</term>
	///         <description>
	///             <para>
	///				Used to output the logger of the logging event. The
	/// 			logger conversion specifier can be optionally followed by
	/// 			<i>precision specifier</i>, that is a decimal constant in
	/// 			brackets.
	///             </para>
	/// 			<para>
	/// 			If a precision specifier is given, then only the corresponding
	/// 			number of right most components of the logger name will be
	/// 			printed. By default the logger name is printed in full.
	/// 			</para>
	/// 			<para>
	/// 			For example, for the logger name "a.b.c" the pattern
	/// 			<b>%logger{2}</b> will output "b.c".
	/// 			</para>
	///         </description>
	///     </item>
	///     <item>
	///         <term>m</term>
	///         <description>Equivalent to <b>message</b></description>
	///     </item>
	///     <item>
	///         <term>M</term>
	///         <description>Equivalent to <b>method</b></description>
	///     </item>
	///		<item>
	///			<term>message</term>
	///			<description>
	/// 			<para>
	/// 			Used to output the application supplied message associated with 
	/// 			the logging event.
	/// 			</para>
	///			</description>
	///		</item>
	///		<item>
	///			<term>mdc</term>
	///			<description>
	/// 			<para>
	/// 			Used to output the MDC (mapped diagnostic context) associated
	/// 			with the thread that generated the logging event. The key to lookup
	/// 			must be specified within braces and directly following the
	/// 			pattern specifier, e.g. <c>%X{user}</c> would include the value
	/// 			from the MDC that is keyed by the string 'user'. Each MDC value
	/// 			that is to be included in the log must be specified separately.
	/// 			</para>
	///			</description>
	///		</item>
	///		<item>
	///			<term>method</term>
	///			<description>
	///				<para>
	///				Used to output the method name where the logging request was
	///				issued.
	///				</para>
	///				<para>
	///				<b>WARNING</b> Generating caller location information is
	///				extremely slow. It's use should be avoided unless execution speed
	///				is not an issue.
	///				</para>
	/// 			<para>
	/// 			See the note below on the availablity of caller location information.
	/// 			</para>
	///			</description>
	///		</item>
	///     <item>
	///         <term>n</term>
	///         <description>Equivalent to <b>newline</b></description>
	///     </item>
	///		<item>
	///			<term>newline</term>
	///			<description>
	/// 			<para>
	/// 			Outputs the platform dependent line separator character or
	/// 			characters.
	/// 			</para>
	/// 			<para>
	/// 			This conversion character offers the same performance as using 
	/// 			non-portable line separator strings such as	"\n", or "\r\n". 
	/// 			Thus, it is the preferred way of specifying a line separator.
	/// 			</para> 
	///			</description>
	///		</item>
	///		<item>
	///			<term>ndc</term>
	///			<description>
	/// 			<para>
	/// 			Used to output the NDC (nested diagnostic context) associated
	/// 			with the thread that generated the logging event.
	/// 			</para>
	///			</description>
	///		</item>
	///     <item>
	///         <term>p</term>
	///         <description>Equivalent to <b>level</b></description>
	///     </item>
	///     <item>
	///         <term>P</term>
	///         <description>Equivalent to <b>property</b></description>
	///     </item>
	///		<item>
	///			<term>property</term>
	///			<description>
	/// 			<para>
	/// 			Used to output the an event specific property. The key to 
	/// 			lookup must be specified within braces and directly following the
	/// 			pattern specifier, e.g. <b>%property{user}</b> would include the value
	/// 			from the property that is keyed by the string 'user'. Each property value
	/// 			that is to be included in the log must be specified separately.
	/// 			Properties are added to events by loggers or appenders. By default
	/// 			no properties are defined.
	/// 			</para>
	///			</description>
	///		</item>
	///     <item>
	///         <term>r</term>
	///         <description>Equivalent to <b>timestamp</b></description>
	///     </item>
	///     <item>
	///         <term>t</term>
	///         <description>Equivalent to <b>thread</b></description>
	///     </item>
	///		<item>
	///			<term>timestamp</term>
	///			<description>
	/// 			<para>
	/// 			Used to output the number of milliseconds elapsed since the start
	/// 			of the application until the creation of the logging event.
	/// 			</para>
	///			</description>
	///		</item>
	///		<item>
	///			<term>thread</term>
	///			<description>
	/// 			<para>
	/// 			Used to output the name of the thread that generated the
	/// 			logging event. Uses the thread number if no name is available.
	/// 			</para>
	///			</description>
	///		</item>
	///     <item>
	///			<term>type</term> 
	///			<description>
	/// 			<para>
	/// 			Used to output the fully qualified type name of the caller
	/// 			issuing the logging request. This conversion specifier
	/// 			can be optionally followed by <i>precision specifier</i>, that
	/// 			is a decimal constant in brackets.
	/// 			</para>
	/// 			<para>
	/// 			If a precision specifier is given, then only the corresponding
	/// 			number of right most components of the class name will be
	/// 			printed. By default the class name is output in fully qualified form.
	/// 			</para>
	/// 			<para>
	/// 			For example, for the class name "log4net.Layout.PatternLayout", the
	/// 			pattern <b>%type{1}</b> will output "PatternLayout".
	/// 			</para>
	/// 			<para>
	/// 			<b>WARNING</b> Generating the caller class information is
	/// 			slow. Thus, it's use should be avoided unless execution speed is
	/// 			not an issue.
	/// 			</para>
	/// 			<para>
	/// 			See the note below on the availablity of caller location information.
	/// 			</para>
	///			</description>
	///     </item>
	///     <item>
	///         <term>u</term>
	///         <description>Equivalent to <b>identity</b></description>
	///     </item>
	///		<item>
	///			<term>username</term>
	///			<description>
	///				<para>
	///				Used to output the WindowsIdentity for the currently
	///				active user.
	///				</para>
	///				<para>
	///				<b>WARNING</b> Generating caller WindowsIdentity information is
	///				extremely slow. It's use should be avoided unless execution speed
	///				is not an issue.
	///				</para>
	///			</description>
	///		</item>
	///     <item>
	///         <term>w</term>
	///         <description>Equivalent to <b>username</b></description>
	///     </item>
	///     <item>
	///         <term>x</term>
	///         <description>Equivalent to <b>ndc</b></description>
	///     </item>
	///     <item>
	///         <term>X</term>
	///         <description>Equivalent to <b>mdc</b></description>
	///     </item>
	///		<item>
	///			<term>%</term>
	///			<description>
	/// 			<para>
	/// 			The sequence %% outputs a single percent sign.
	/// 			</para>
	///			</description>
	///		</item>
	/// </list>
	/// <para>
	/// The single letter patterns are deprecated in favour of the 
	/// longer more descriptive patterns.
	/// </para>
	/// <para>
	/// By default the relevant information is output as is. However,
	/// with the aid of format modifiers it is possible to change the
	/// minimum field width, the maximum field width and justification.
	/// </para>
	/// <para>
	/// The optional format modifier is placed between the percent sign
	/// and the conversion character.
	/// </para>
	/// <para>
	/// The first optional format modifier is the <i>left justification
	/// flag</i> which is just the minus (-) character. Then comes the
	/// optional <i>minimum field width</i> modifier. This is a decimal
	/// constant that represents the minimum number of characters to
	/// output. If the data item requires fewer characters, it is padded on
	/// either the left or the right until the minimum width is
	/// reached. The default is to pad on the left (right justify) but you
	/// can specify right padding with the left justification flag. The
	/// padding character is space. If the data item is larger than the
	/// minimum field width, the field is expanded to accommodate the
	/// data. The value is never truncated.
	/// </para>
	/// <para>
	/// This behaviour can be changed using the <i>maximum field
	/// width</i> modifier which is designated by a period followed by a
	/// decimal constant. If the data item is longer than the maximum
	/// field, then the extra characters are removed from the
	/// <i>beginning</i> of the data item and not from the end. For
	/// example, it the maximum field width is eight and the data item is
	/// ten characters long, then the first two characters of the data item
	/// are dropped. This behaviour deviates from the printf function in C
	/// where truncation is done from the end.
	/// </para>
	/// <para>
	/// Below are various format modifier examples for the logger
	/// conversion specifier.
	/// </para>
	/// <div class="tablediv">
	///		<table class="dtTABLE" cellspacing="0">
	///			<tr>
	///				<th>Format modifier</th>
	///				<th>left justify</th>
	///				<th>minimum width</th>
	///				<th>maximum width</th>
	///				<th>comment</th>
	///			</tr>
	///			<tr>
	///				<td align="center">%20logger</td>
	///				<td align="center">false</td>
	///				<td align="center">20</td>
	///				<td align="center">none</td>
	///				<td>
	///					<para>
	///					Left pad with spaces if the logger name is less than 20
	///					characters long.
	///					</para>
	///				</td>
	///			</tr>
	///			<tr>
	///				<td align="center">%-20logger</td>
	///				<td align="center">true</td>
	///				<td align="center">20</td>
	///				<td align="center">none</td>
	///				<td>
	///					<para>
	///					Right pad with spaces if the logger 
	///					name is less than 20 characters long.
	///					</para>
	///				</td>
	///			</tr>
	///			<tr>
	///				<td align="center">%.30logger</td>
	///				<td align="center">NA</td>
	///				<td align="center">none</td>
	///				<td align="center">30</td>
	///				<td>
	///					<para>
	///					Truncate from the beginning if the logger 
	///					name is longer than 30 characters.
	///					</para>
	///				</td>
	///			</tr>
	///			<tr>
	///				<td align="center"><nobr>%20.30logger</nobr></td>
	///				<td align="center">false</td>
	///				<td align="center">20</td>
	///				<td align="center">30</td>
	///				<td>
	///					<para>
	///					Left pad with spaces if the logger name is shorter than 20
	///					characters. However, if logger name is longer than 30 characters,
	///					then truncate from the beginning.
	///					</para>
	///				</td>
	///			</tr>
	///			<tr>
	///				<td align="center">%-20.30logger</td>
	///				<td align="center">true</td>
	///				<td align="center">20</td>
	///				<td align="center">30</td>
	///				<td>
	///					<para>
	///					Right pad with spaces if the logger name is shorter than 20
	///					characters. However, if logger name is longer than 30 characters,
	///					then truncate from the beginning.
	///					</para>
	///				</td>
	///			</tr>
	///		</table>
	///	</div>
	///	<para>
	///	<b>Note about caller location information.</b><br />
	///	The following patterns <c>%type %file %line %method %location %class %C %F %L %l %M</c> 
	///	all generate caller location information.
	/// Location information uses the <c>System.Diagnostics.StackTrace</c> class to generate
	/// a call stack. The caller's information is then extracted from this stack.
	/// </para>
	/// <para>
	/// The <c>System.Diagnostics.StackTrace</c> class is not supported on the 
	/// .NET Compact Framework 1.0 therefore caller location information is not
	/// available on that framework.
	/// </para>
	/// <para>
	/// The <c>System.Diagnostics.StackTrace</c> class has this to say about Release builds:
	/// </para>
	/// <para>
	/// "StackTrace information will be most informative with Debug build configurations. 
	/// By default, Debug builds include debug symbols, while Release builds do not. The 
	/// debug symbols contain most of the file, method name, line number, and column 
	/// information used in constructing StackFrame and StackTrace objects. StackTrace 
	/// might not report as many method calls as expected, due to code transformations 
	/// that occur during optimization."
	/// </para>
	/// <para>
	/// This means that in a Release build the caller information may be incomplete or may 
	/// not exist at all! Therefore caller location information cannot be relied upon in a Release build.
	/// </para>
	/// </remarks>
	/// <example>
	/// This is essentially the TTCC layout
	/// <code><b>%timestamp [%thread] %level %logger %ndc - %message%newline</b></code>
	/// </example>
	/// <example>
	/// Similar to the TTCC layout except that the relative time is
	/// right padded if less than 6 digits, thread name is right padded if
	/// less than 15 characters and truncated if longer and the logger
	/// name is left padded if shorter than 30 characters and truncated if
	/// longer.
	/// <code><b>%-6timestamp [%15.15thread] %-5level %30.30logger %ndc - %message%newline</b></code>
	/// </example>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	/// <author>Douglas de la Torre</author>
	/// <author>Daniel Cazzulino</author>
	public class PatternLayout : PatternLayoutShim
	{
		#region Constants

		/// <summary>
		/// Default pattern string for log output. 
		/// Currently set to the string <b>"%message%newline"</b> 
		/// which just prints the application supplied	message. 
		/// </summary>
		public const string DefaultConversionPattern ="%message%newline";

		/// <summary>
		/// A conversion pattern equivalent to the TTCCLayout.
		/// </summary>
		/// <remarks>
		/// A conversion pattern equivalent to the TTCCLayout. Which stood for Time, Thread, Category, and Context.
		/// Now this is Time, Thread, Logger, and Nested Context.
		/// Current value is <b>%timestamp [%thread] %level %logger %ndc - %message%newline</b>.
		/// </remarks>
		public const string TtlnConversionPattern = "%timestamp [%thread] %level %logger %ndc - %message%newline";

		#endregion

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
		static PatternLayout()
		{
			s_globalRulesRegistry = new Hashtable(35);

			s_globalRulesRegistry.Add("literal", typeof(LiteralPatternConverter));
			s_globalRulesRegistry.Add("newline", typeof(NewLinePatternConverter));
			s_globalRulesRegistry.Add("n", typeof(NewLinePatternConverter));

			s_globalRulesRegistry.Add("c", typeof(LoggerPatternConverter));
			s_globalRulesRegistry.Add("logger", typeof(LoggerPatternConverter));

			s_globalRulesRegistry.Add("C", typeof(TypeNamePatternConverter));
			s_globalRulesRegistry.Add("class", typeof(TypeNamePatternConverter));
			s_globalRulesRegistry.Add("type", typeof(TypeNamePatternConverter));

			s_globalRulesRegistry.Add("d", typeof(DatePatternConverter));
			s_globalRulesRegistry.Add("date", typeof(DatePatternConverter));

			s_globalRulesRegistry.Add("F", typeof(FileLocationPatternConverter));
			s_globalRulesRegistry.Add("file", typeof(FileLocationPatternConverter));

			s_globalRulesRegistry.Add("l", typeof(FullLocationPatternConverter));
			s_globalRulesRegistry.Add("location", typeof(FullLocationPatternConverter));

			s_globalRulesRegistry.Add("L", typeof(LineLocationPatternConverter));
			s_globalRulesRegistry.Add("line", typeof(LineLocationPatternConverter));

			s_globalRulesRegistry.Add("m", typeof(MessagePatternConverter));
			s_globalRulesRegistry.Add("message", typeof(MessagePatternConverter));

			s_globalRulesRegistry.Add("M", typeof(MethodLocationPatternConverter));
			s_globalRulesRegistry.Add("method", typeof(MethodLocationPatternConverter));

			s_globalRulesRegistry.Add("p", typeof(LevelPatternConverter));
			s_globalRulesRegistry.Add("level", typeof(LevelPatternConverter));

			s_globalRulesRegistry.Add("P", typeof(PropertyPatternConverter));
			s_globalRulesRegistry.Add("property", typeof(PropertyPatternConverter));

			s_globalRulesRegistry.Add("r", typeof(RelativeTimePatternConverter));
			s_globalRulesRegistry.Add("timestamp", typeof(RelativeTimePatternConverter));

			s_globalRulesRegistry.Add("t", typeof(ThreadPatternConverter));
			s_globalRulesRegistry.Add("thread", typeof(ThreadPatternConverter));

			s_globalRulesRegistry.Add("x", typeof(NdcPatternConverter));
			s_globalRulesRegistry.Add("ndc", typeof(NdcPatternConverter));

			s_globalRulesRegistry.Add("X", typeof(MdcPatternConverter));
			s_globalRulesRegistry.Add("mdc", typeof(MdcPatternConverter));

			s_globalRulesRegistry.Add("a", typeof(AppDomainPatternConverter));
			s_globalRulesRegistry.Add("appdomain", typeof(AppDomainPatternConverter));

			s_globalRulesRegistry.Add("u", typeof(IdentityPatternConverter));
			s_globalRulesRegistry.Add("identity", typeof(IdentityPatternConverter));

			s_globalRulesRegistry.Add("w", typeof(UserNamePatternConverter));
			s_globalRulesRegistry.Add("username", typeof(UserNamePatternConverter));
		}

		#endregion Static Constructor

		#region Constructors

		/// <summary>
		/// Constructs a PatternLayout using the DefaultConversionPattern
		/// </summary>
		/// <remarks>
		/// The default pattern just produces the application supplied message.
		/// </remarks>
		public PatternLayout() : this(DefaultConversionPattern)
		{
		}

		/// <summary>
		/// Constructs a PatternLayout using the supplied conversion pattern
		/// </summary>
		/// <param name="pattern">the pattern to use</param>
		public PatternLayout(string pattern) 
		{
			// By default we do not process the exception
			SetIgnoresException(true);

			m_pattern = pattern;
			m_head = CreatePatternParser((pattern == null) ? DefaultConversionPattern : pattern).Parse();
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
		override public void ActivateOptions() 
		{
			// nothing to do.
		}

		#endregion

		/// <summary>
		/// The <see cref="LayoutSkeleton.IgnoresException"/> value for this layout
		/// </summary>
		/// <remarks>
		/// The default value is <c>true</c>, i.e. that this layout ignores the exception
		/// </remarks>
		new public bool IgnoresException
		{
			get { return base.IgnoresException; }
			set { base.SetIgnoresException(value); }
		}

		#region Override implementation of LayoutSkeleton

		/// <summary>
		/// Produces a formatted string as specified by the conversion pattern.
		/// </summary>
		/// <param name="loggingEvent">the event being logged</param>
		/// <param name="writer">The TextWriter to write the formatted event to</param>
		override public void Format(TextWriter writer, LoggingEvent loggingEvent) 
		{
			if (loggingEvent == null)
			{
				throw new ArgumentNullException("loggingEvent");
			}

			PatternConverter c = m_head;

			// loop through the chain of pattern converters
			while(c != null) 
			{
				c.Format(writer, loggingEvent);
				c = c.Next;
			}
		}

		#endregion
	}

	/// <summary>
	/// Implementation shim class used by the PatternLayout
	/// </summary>
	public abstract class PatternLayoutShim : LayoutSkeleton
	{
		//
		// This class is used to allow the PatternLayout to
		// provide a new implementation of the IgnoresException
		// property that has a setter as well as the getter.
		// This class stores the value and overrides the base class
		// required property.
		//

		#region Member Variables
    
		/// <summary>
		/// Store IgnoresException state
		/// </summary>
		private bool m_ignoresException;

		#endregion

		#region Constructors

		/// <summary>
		/// </summary>
		/// <remarks>
		/// </remarks>
		protected PatternLayoutShim()
		{
		}

		#endregion
  
		#region Override implementation of LayoutSkeleton

		/// <summary>
		/// The <see cref="IgnoresException"/> value
		/// </summary>
		override public bool IgnoresException
		{
			get { return m_ignoresException; }
		}

		#endregion

		/// <summary>
		/// Set the <see cref="IgnoresException"/> value
		/// </summary>
		/// <param name="value">the value to set</param>
		protected void SetIgnoresException(bool value)
		{
			m_ignoresException = value;
		}
	}
}
