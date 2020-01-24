using log4net.Core;
using log4net.Layout;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;

namespace log4net.Appender
{
    public class RestServiceAppender : IAppender, IBulkAppender, IOptionHandler, IErrorHandler
    {
        private HttpClient _httpClient;


        public RestServiceAppender(HttpClient httpClient)
        {
            _httpClient = httpClient;

            m_parameters = new ArrayList();
        }

        public RestServiceAppender()
            :this(new HttpClient())
        {
           
        }

        public string LoggingEndpoint { get; set; }
        public string ContentType { get; set; }
        public string HttpMethod { get; set; }
        public string Name { get; set; }
        public bool IncludeAllFields { get; set; }

        public void AddParameter(RestAppenderParameter parameter)
        {
            m_parameters.Add(parameter);
        }

        private string BindParameters(LoggingEvent loggingEvent)
        {
            var restFields = new Dictionary<string, object>();

            foreach (RestAppenderParameter item in m_parameters)
                item.FormatValue(restFields, loggingEvent);

            if (IncludeAllFields)
                restFields.Add("DetailFields", JsonConvert.SerializeObject(loggingEvent));

            return JsonConvert.SerializeObject(restFields);
        }


        private void SendLogs(string messageContent)
        {
            try
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage();

                requestMessage.Method = new System.Net.Http.HttpMethod(HttpMethod);

                requestMessage.Content = new StringContent(messageContent);

                var result = _httpClient.SendAsync(requestMessage).Result;
            }
            catch (Exception ex)
            {
                // Sadly, your connection is bad or rest endpoint does not exist
                Error($"Cannot Send Error To {LoggingEndpoint}", ex);
            }

        }

        public void Close()
        {

        }

        public void DoAppend(LoggingEvent loggingEvent)
        {
            SendLogs(BindParameters(loggingEvent));
        }

        public void DoAppend(LoggingEvent[] loggingEvents)
        {
            foreach (var item in loggingEvents)
            {
                SendLogs(BindParameters(item));
            }
        }

        public void ActivateOptions()
        {
            _httpClient.BaseAddress = new Uri(LoggingEndpoint);

            foreach (RestAppenderParameter item in m_parameters)
            {
                Console.WriteLine(item.ParameterName);
                Console.WriteLine(item.Layout);
            }
        }

        public void Error(string message, Exception e, ErrorCode errorCode)
        {
            Console.WriteLine($"{message} - {e.StackTrace} - {errorCode}");
        }

        public void Error(string message, Exception e)
        {
            Error(message, e, ErrorCode.GenericFailure);
        }

        public void Error(string message)
        {
            this.Error(message, null, ErrorCode.GenericFailure);
        }

        protected ArrayList m_parameters;

    }



    public class RestAppenderParameter
    {

        public RestAppenderParameter()
        {

        }

        public string ParameterName
        {
            get;
            set;
        }

        public IRawLayout Layout
        {
            get;
            set;
        }

        public virtual void FormatValue(Dictionary<string, object> values, LoggingEvent loggingEvent)
        {
            object formattedValue = Layout.Format(loggingEvent);

            values.Add(ParameterName, formattedValue);
        }


    }
}
