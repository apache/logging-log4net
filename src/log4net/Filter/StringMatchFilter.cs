#region Apache License
//
// Licensed to the Apache Software Foundation (ASF) under one or more 
// contributor license agreements. See the NOTICE file distributed with
// this work for additional information regarding copyright ownership. 
// The ASF licenses this file to you under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with 
// the License. You may obtain a copy of the License at
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
using System.Text.RegularExpressions;

using log4net.Core;

namespace log4net.Filter
{
  /// <summary>
  /// Simple filter to match a string in the rendered message.
  /// </summary>
  /// <author>Nicko Cadell</author>
  /// <author>Gert Driesen</author>
  public class StringMatchFilter : FilterSkeleton
  {
    /// <summary>
    /// A regex object to match (generated from m_stringRegexToMatch)
    /// </summary>
    protected Regex? m_regexToMatch;

    /// <summary>
    /// Initialize and precompile the Regex if required
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
    public override void ActivateOptions() 
    {
      if (RegexToMatch is not null)
      {
        m_regexToMatch = new Regex(RegexToMatch, RegexOptions.Compiled);
      }
    }

    /// <summary>
    /// <see cref="FilterDecision.Accept"/> when matching <see cref="StringToMatch"/> or <see cref="RegexToMatch"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="AcceptOnMatch"/> property is a flag that determines
    /// the behavior when a matching <see cref="Level"/> is found. If the
    /// flag is set to true then the filter will <see cref="FilterDecision.Accept"/> the 
    /// logging event, otherwise it will <see cref="FilterDecision.Neutral"/> the event.
    /// </para>
    /// <para>
    /// The default is <c>true</c> i.e. to <see cref="FilterDecision.Accept"/> the event.
    /// </para>
    /// </remarks>
    public bool AcceptOnMatch { get; set; } = true;

    /// <summary>
    /// Sets the static string to match
    /// </summary>
    /// <remarks>
    /// <para>
    /// The string that will be substring matched against
    /// the rendered message. If the message contains this
    /// string then the filter will match. If a match is found then
    /// the result depends on the value of <see cref="AcceptOnMatch"/>.
    /// </para>
    /// <para>
    /// One of <see cref="StringToMatch"/> or <see cref="RegexToMatch"/>
    /// must be specified.
    /// </para>
    /// </remarks>
    public string? StringToMatch { get; set; }

    /// <summary>
    /// Sets the regular expression to match
    /// </summary>
    /// <remarks>
    /// <para>
    /// The regular expression pattern that will be matched against
    /// the rendered message. If the message matches this
    /// pattern then the filter will match. If a match is found then
    /// the result depends on the value of <see cref="AcceptOnMatch"/>.
    /// </para>
    /// <para>
    /// One of <see cref="StringToMatch"/> or <see cref="RegexToMatch"/>
    /// must be specified.
    /// </para>
    /// </remarks>
    public string? RegexToMatch { get; set; }

    /// <summary>
    /// Check if this filter should allow the event to be logged
    /// </summary>
    /// <param name="loggingEvent">the event being logged</param>
    /// <returns>see remarks</returns>
    /// <remarks>
    /// <para>
    /// The rendered message is matched against the <see cref="StringToMatch"/>.
    /// If the <see cref="StringToMatch"/> occurs as a substring within
    /// the message then a match will have occurred. If no match occurs
    /// this function will return <see cref="FilterDecision.Neutral"/>
    /// allowing other filters to check the event. If a match occurs then
    /// the value of <see cref="AcceptOnMatch"/> is checked. If it is
    /// true then <see cref="FilterDecision.Accept"/> is returned otherwise
    /// <see cref="FilterDecision.Deny"/> is returned.
    /// </para>
    /// </remarks>
    public override FilterDecision Decide(LoggingEvent loggingEvent) 
    {
      if (loggingEvent is null)
      {
        throw new ArgumentNullException(nameof(loggingEvent));
      }

      string? msg = loggingEvent.RenderedMessage;

      // Check if we have been setup to filter
      if (msg is null || (StringToMatch is null && m_regexToMatch is null))
      {
        // We cannot filter so allow the filter chain
        // to continue processing
        return FilterDecision.Neutral;
      }
    
      // Firstly check if we are matching using a regex
      if (m_regexToMatch is not null)
      {
        // Check the regex
        if (m_regexToMatch.Match(msg).Success == false)
        {
          // No match, continue processing
          return FilterDecision.Neutral;
        } 

        // we've got a match
        if (AcceptOnMatch) 
        {
          return FilterDecision.Accept;
        } 
        return FilterDecision.Deny;
      }
      else if (StringToMatch is not null)
      {
        // Check substring match
        if (msg.IndexOf(StringToMatch) == -1) 
        {
          // No match, continue processing
          return FilterDecision.Neutral;
        } 

        // we've got a match
        if (AcceptOnMatch) 
        {
          return FilterDecision.Accept;
        } 
        return FilterDecision.Deny;
      }

      return FilterDecision.Neutral;
    }
  }
}
