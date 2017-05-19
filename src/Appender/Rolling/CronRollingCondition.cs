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
using System.IO;

namespace log4net.Appender.Rolling
{
    /// <summary>
    /// A implementation of the <see cref="IRollingCondition"/> interface that rolls
    /// the file cronologically.
    /// </summary>
    /// <author>Dominik Psenner</author>
    public class CronRollingCondition : IRollingCondition
    {
        #region Public Instance Constructors

        public CronRollingCondition()
            : this("*", "*", "*", "*", "*")
        {
        }

        public CronRollingCondition(string dow, string month, string day, string hour, string minute)
        {
            Dow = TryParse(dow);
            Month = TryParse(month);
            Day = TryParse(day);
            Hour = TryParse(hour);
            Minute = TryParse(minute);
        }

        #endregion

        #region Protected Instance Properties

        protected Tuple<int?, MatchType> Dow { get; private set; }
        protected Tuple<int?, MatchType> Month { get; private set; }
        protected Tuple<int?, MatchType> Day { get; private set; }
        protected Tuple<int?, MatchType> Hour { get; private set; }
        protected Tuple<int?, MatchType> Minute { get; private set; }

        #endregion

        #region Private Instance Properties

        private ulong last_rolled = 0;

        #endregion

        #region Protected Inner Classes

        protected enum MatchType
        {
            Nothing,
            Exact,
            Remainder,
        }

        #endregion

        #region Protected Static Methods

        /// <summary>
        /// This method parses a string to match any of these:
        /// i
        /// *
        /// */i
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        static protected Tuple<int?, MatchType> TryParse(string input)
        {
            // trim input and strip empty spaces
            string inputParsed = input.Trim().Replace(" ", "").Replace("\t", "");

            // match a remainder: */c
            if (inputParsed.StartsWith("*/") || inputParsed.StartsWith(@"*\"))
            {
                // strip first two chars
                inputParsed = inputParsed.Substring(2);
                // parse the remainder
                int i = -1;
                if (int.TryParse(inputParsed, out i))
                {
                    return Tuple.Create<int?, MatchType>(i, MatchType.Remainder);
                }
            }
            else if (inputParsed.StartsWith("*")) // match anything: *
            {
                return Tuple.Create<int?, MatchType>(null, MatchType.Nothing);
            }
            else // match one specific numer: i
            {
                int i = -1;
                if (int.TryParse(inputParsed, out i))
                {
                    return Tuple.Create<int?, MatchType>(i, MatchType.Exact);
                }
            }

            // throw exception by default
            throw new FormatException(string.Format("The input string '{0}' could not be parsed to a valid format.", input));
        }

        #endregion

        #region Implementation of IRollingCondition

        public bool IsMet(string file)
        {
            return IsMet(DateTime.Now);
        }

        private static ulong GetUniqueIndex(DateTime now)
        {
            ulong result = (ulong)now.DayOfWeek;
            result <<= 3;
            result += (ulong)now.Month;
            result <<= 4;
            result += (ulong)now.Day;
            result <<= 5;
            result += (ulong)now.Hour;
            result <<= 5;
            result += (ulong)now.Minute;
            return result;
        }

        #endregion

        #region Public Methods

        public bool IsMet(DateTime now)
        {
            Console.WriteLine("test {0}", now);
            // check only every minute
            // we can skip the check as we checked this minute already 
            // and if we don't we may run into the situation to roll a file twice
            if (GetUniqueIndex(now) == last_rolled)
            {
                Console.WriteLine("  skipped");
                return false;
            }
            if (!IsMet(Dow, (int)now.DayOfWeek))
            {
                return false;
            }
            if (!IsMet(Month, now.Month))
            {
                return false;
            }
            if (!IsMet(Day, now.Day))
            {
                return false;
            }
            if (!IsMet(Hour, now.Hour))
            {
                return false;
            }
            if (!IsMet(Minute, now.Minute))
            {
                return false;
            }

            last_rolled = GetUniqueIndex(now);
            return true;
        }

        #endregion

        #region Private Methods

        private bool IsMet(Tuple<int?, MatchType> match, int item)
        {
            switch (match.Item2)
            {
                case MatchType.Exact:
                    Console.WriteLine("  {0} != {1} == {2}", match.Item1.Value, item, match.Item1.Value != item);
                    if (match.Item1.Value != item)
                    {
                        return false;
                    }
                    break;
                case MatchType.Remainder:
                    // special case: */0, the division through 0 is undefined; this match should pass
                    if (match.Item1.Value == 0)
                    {
                        Console.WriteLine("{0} % 0 == 0", item);
                        return false;
                    }
                    else
                    {
                        Console.WriteLine("  {0} % {1} == {2}", item, match.Item1.Value, item % match.Item1.Value);
                        if (item % match.Item1.Value != 0)
                        {
                            return false;
                        }
                    }
                    break;
            }
            return true;
        }

        #endregion

        /// <summary>
        /// Converts the given rolling condition to a nicely formatted string representation.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{0} {1} {2} {3} {4}", Dow, Month, Day, Hour, Minute);
        }
    }
}
