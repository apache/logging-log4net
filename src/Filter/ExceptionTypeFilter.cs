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
