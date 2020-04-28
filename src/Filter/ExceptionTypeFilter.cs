using log4net.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace log4net.Filter
{
    /// <summary>
    /// Filter the exception type name
    /// </summary>
    public class ExceptionTypeFilter : FilterSkeleton
    {

        private bool m_acceptOnMatch = true;
        private string m_exceptionTypeName;

        /// <summary>
        /// Set the exception type name to filter
        /// </summary>
        public string ExceptionTypeName
        {
            get
            {
                return this.m_exceptionTypeName;
            }
            set
            {
                this.m_exceptionTypeName = value;
            }
        }

        /// <summary>
        /// <see cref="FilterDecision.Accept"/> when matching <see cref="ExceptionTypeName"/>
        /// Default value is true.
        /// </summary>
        public bool AcceptOnMatch
        {
            get
            {
                return this.m_acceptOnMatch;
            }
            set
            {
                this.m_acceptOnMatch = value;
            }
        }

        /// <summary>
        /// Check if the event should be logged.
        /// </summary>
        /// <param name="loggingEvent">the logging event to check</param>
        /// <returns>see remarks</returns>
        /// <remarks>
        /// <para>
        /// If the <see cref="Level"/> of the logging event is outside the range
        /// matched by this filter then <see cref="FilterDecision.Deny"/>
        /// is returned. If the <see cref="Level"/> is matched then the value of
        /// <see cref="AcceptOnMatch"/> is checked. If it is true then
        /// <see cref="FilterDecision.Accept"/> is returned, otherwise
        /// <see cref="FilterDecision.Neutral"/> is returned.
        /// </para>
        /// </remarks>
        public override FilterDecision Decide(LoggingEvent loggingEvent)
        {
            if (String.IsNullOrEmpty(this.ExceptionTypeName))
                return FilterDecision.Neutral;

            Type myExceptionType = Type.GetType(this.ExceptionTypeName, false);

            if (this.ExceptionTypeName != null
                && myExceptionType != null
                && loggingEvent.ExceptionObject != null)
            {
                bool myIsMatched = myExceptionType.IsAssignableFrom(loggingEvent.ExceptionObject.GetType());

                if (this.AcceptOnMatch)
                {
                    if (myIsMatched)
                        return FilterDecision.Accept;
                }
                else
                {
                    if (myIsMatched)
                        return FilterDecision.Deny;
                }
            }

            return FilterDecision.Neutral;
        }
    }
}
