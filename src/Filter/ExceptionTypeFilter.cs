using log4net.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace log4net.Filter
{
    public class ExceptionTypeFilter : FilterSkeleton
    {
        public ExceptionTypeFilter()
        {
            this.AcceptOnMatch = true;
        }

        /// <summary>
        /// Set the exception type name to filter
        /// </summary>
        public string ExceptionTypeName { get; set; }

        /// <summary>
        /// <see cref="FilterDecision.Accept"/> when matching <see cref="ExceptionTypeName"/>
        /// Default value is true.
        /// </summary>
        public bool AcceptOnMatch { get; set; }

        public override FilterDecision Decide(LoggingEvent loggingEvent)
        {
            var myExceptionType = Type.GetType(this.ExceptionTypeName, false);

            if (this.ExceptionTypeName != null
                && myExceptionType != null
                && loggingEvent.ExceptionObject != null)
            {

                var myIsMatched = myExceptionType.IsInstanceOfType(loggingEvent.ExceptionObject);

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
