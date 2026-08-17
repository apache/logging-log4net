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
using log4net.Util;

namespace log4net.Filter;

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
  // ReSharper disable once InconsistentNaming
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
      m_regexToMatch = new(RegexToMatch, RegexOptions.Compiled,
        _matchTimeoutMillis == 0
          ? Regex.InfiniteMatchTimeout
          : TimeSpan.FromMilliseconds(_matchTimeoutMillis));
    }
  }

  /// <summary>
  /// Gets or sets the time, in milliseconds, that matching <see cref="RegexToMatch"/> against a
  /// single event may take before the match is abandoned.
  /// </summary>
  /// <value>
  /// A positive number of milliseconds, or 0 to let a match run for as long as it takes.
  /// </value>
  /// <remarks>
  /// <para>
  /// A regular expression that backtracks can take a very long time on some inputs. The pattern is
  /// matched while the appender lock is held, so an unbounded match would stall everything logging
  /// through the appender, and matching is therefore given a deadline. A match that reaches it is
  /// treated as no match, leaving the rest of the filter chain to decide.
  /// </para>
  /// <para>
  /// The pattern comes from configuration and is trusted, so this is a guard against a pattern that
  /// turns out to be expensive rather than protection against untrusted input.
  /// </para>
  /// <para>
  /// The default value is 1000 (one second). Setting the value to 0 restores unbounded matching and
  /// is not recommended. Changing it takes effect when <see cref="ActivateOptions"/> is called.
  /// </para>
  /// </remarks>
  /// <exception cref="ArgumentOutOfRangeException">The value specified is negative.</exception>
  public int MatchTimeoutMillis
  {
    get => _matchTimeoutMillis;
    set
    {
      if (value < 0)
      {
        throw SystemInfo.CreateArgumentOutOfRangeException(nameof(value), value,
          "The value specified for MatchTimeoutMillis is negative.");
      }
      _matchTimeoutMillis = value;
    }
  }

  private int _matchTimeoutMillis = 1000;
  private bool _matchTimeoutReported;

  /// <summary>
  /// Matches <paramref name="value"/> against <see cref="m_regexToMatch"/>.
  /// </summary>
  /// <param name="value">The text to match.</param>
  /// <returns>
  /// <see langword="true"/> when the pattern matches, and <see langword="false"/> when it does not
  /// or when matching took longer than <see cref="MatchTimeoutMillis"/>.
  /// </returns>
  protected bool IsRegexMatch(string value)
  {
    try
    {
      return m_regexToMatch!.IsMatch(value);
    }
    catch (RegexMatchTimeoutException)
    {
      if (!_matchTimeoutReported)
      {
        // Once per filter. The condition repeats for every event that reaches it, and a warning
        // per event would be a denial of service of its own.
        _matchTimeoutReported = true;
        LogLog.Warn(_declaringType,
          $"Matching the pattern [{RegexToMatch}] took longer than {MatchTimeoutMillis}ms and was abandoned, so the event was not filtered by it. "
          + "A pattern that backtracks can take arbitrarily long on some inputs; consider rewriting it or raising MatchTimeoutMillis.");
      }
      return false;
    }
  }

  /// <summary>
  /// The fully qualified type of the <see cref="StringMatchFilter"/> class.
  /// </summary>
  private static readonly Type _declaringType = typeof(StringMatchFilter);

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
  /// The default is <see langword="true"/> i.e. to <see cref="FilterDecision.Accept"/> the event.
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
    string? msg = loggingEvent.EnsureNotNull().RenderedMessage;

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
      if (!IsRegexMatch(msg))
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
