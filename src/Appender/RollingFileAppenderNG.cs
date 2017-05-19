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
using System.Collections;
using System.Globalization;
using System.IO;

using log4net.Util;
using log4net.Core;
using log4net.Appender.Rolling;

namespace log4net.Appender
{
    /// <summary>
    /// Appender that rolls log files based on size or date or both.
    /// </summary>
    /// <author>Dominik Psenner</author>
    public class RollingFileAppenderNG : FileAppender
    {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RollingFileAppenderNG" /> class.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Default constructor.
        /// </para>
        /// </remarks>
        public RollingFileAppenderNG()
        {
            // for now set up the cron rolling condition and the index rolling strategy by default
            RollingCondition = new CronRollingCondition("*", "*", "*", "*", "*");
            RollingStrategy = new IndexRollingStrategy();
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets the strategy to decide whether or not a file should be rolled over.
        /// </summary>
        public IRollingCondition RollingCondition
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the strategy to roll a file.
        /// </summary>
        public IRollingStrategy RollingStrategy
        {
            get;
            private set;
        }

        #endregion Public Instance Properties

        #region Override implementation of FileAppender 

        /// <summary>
        /// Sets the quiet writer being used.
        /// </summary>
        /// <remarks>
        /// This method can be overridden by sub classes.
        /// </remarks>
        /// <param name="writer">the writer to set</param>
        override protected void SetQWForFiles(TextWriter writer)
        {
            QuietWriter = new CountingQuietTextWriter(writer, ErrorHandler);
        }

        /// <summary>
        /// Write out a logging event.
        /// </summary>
        /// <param name="loggingEvent">the event to write to file.</param>
        /// <remarks>
        /// <para>
        /// Handles append time behavior for RollingFileAppenderNG.
        /// </para>
        /// </remarks>
        override protected void Append(LoggingEvent loggingEvent)
        {
            RollFileTrigger();
            base.Append(loggingEvent);
        }

        /// <summary>
        /// Write out an array of logging events.
        /// </summary>
        /// <param name="loggingEvents">the events to write to file.</param>
        /// <remarks>
        /// <para>
        /// Handles append time behavior for RollingFileAppenderNG.
        /// </para>
        /// </remarks>
        override protected void Append(LoggingEvent[] loggingEvents)
        {
            RollFileTrigger();
            base.Append(loggingEvents);
        }

        /// <summary>
        /// Performs any required rolling before outputting the next event
        /// </summary>
        /// <remarks>
        /// <para>
        /// Handles append time behavior for RollingFileAppenderNG. It checks first
        /// whether the conditions to roll the file are met and if so asks the roll
        /// file strategy to do the roll operation.
        /// </para>
        /// </remarks>
        protected void RollFileTrigger()
        {
            if (RollingCondition == null)
            {
                // TODO throw exception
            }
            if (RollingStrategy == null)
            {
                // TODO throw exception
            }

            // check if the rolling conditions are met
            if (RollingCondition.IsMet(File))
            {
                // let the strategy do all the required rolling
                RollingStrategy.Roll(File);
            }
        }
        #endregion
    }
}
