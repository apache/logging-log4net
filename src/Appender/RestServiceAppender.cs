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
using System.Collections.Generic;
using System.Net.Http;

using log4net.Core;
using log4net.Layout;
using Newtonsoft.Json;

namespace log4net.Appender
{
    /// <summary>
    /// Appender that logs to a Rest Service provided via configuration it's designed base of AdoNetAppender.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="RestServiceAppender"/> appends logging events to a rest endpoint
    /// The appender can be configured to specify the serviice endpoint string by setting  <see cref="LoggingEndpoint"/> property.
    /// The rest service content type cen be specified by  <see cref="ContentType"/> property.
    /// The Rest method can be specified via <see cref="HttpMethod"/> property.
    /// Also this appender has a propert named as <see cref="IncludeAllFields"/>. This fields checks with the appender and it's serialized that 
    /// all event details with JSonConvert and append it the rest call.
    /// </para>
    /// </remarks>
    /// <example>
    /// An example of a request Json
    /// <code lang="JSON">
    /// {
    ///     "log_level":"INFO",
    ///     "logger":"RestLogger",
    ///     "message":"Log Mesaji 0",
    ///     "userid":"Custom Parameter With Thread Context"
    /// }
    /// </code>
    /// </example>
    /// <example>
    /// An example configuration to log generate like above json format.
    /// <code lang="XML" escaped="true">
    /// <appender name="RestServiceAppender_RestEndpoint" type="log4net.Appender.RestServiceAppender" >
    ///   <LoggingEndpoint value="http://dummyendpoint.log4net.apache.org/DummyLoggingServiceEndpoint" />
    ///   <ContentType value="applicationJson" />
    ///   <HttpMethod value="POST" />
    ///   <IncludeAllFields value="false" />
    ///      <parameter>
    ///        <parameterName value="log_level" />
    ///        <layout type="log4net.Layout.PatternLayout">
    ///          <conversionPattern value="%level" />
    ///        </layout>
    ///      </parameter>
    ///      <parameter>
    ///        <parameterName value="logger" />
    ///        <layout type="log4net.Layout.PatternLayout">
    ///          <conversionPattern value="%logger" />
    ///        </layout>
    ///      </parameter>
    ///      <parameter>
    ///        <parameterName value="message" />
    ///        <layout type="log4net.Layout.PatternLayout">
    ///          <conversionPattern value="%message" />
    ///        </layout>
    ///      </parameter> 
    ///      <parameter>
    ///        <parameterName value="CustomParameter" />
    ///        <layout type="log4net.Layout.PatternLayout">
    ///          <conversionPattern value="%property{userid}" />
    ///        </layout>
    ///      </parameter>
    /// </appender>
    /// </code>
    /// </example>
    /// <author>Ertugrul Kara</author>
    public class RestServiceAppender : IAppender, IBulkAppender, IOptionHandler, IErrorHandler
    {
        private HttpClient _httpClient;

        #region Public Instance Constructors

        /// <summary> 
        /// Initializes a new instance of the <see cref="RestServiceAppender" /> class.
        /// </summary>
        /// <remarks>
        /// Public default constructor to initialize a new instance of this class. It will use only Unit Testing purpose
        /// </remarks>
        public RestServiceAppender(HttpClient httpClient)
        {
            _httpClient = httpClient;

            m_parameters = new ArrayList();
        }

        /// <summary> 
        /// Initializes a new instance of the <see cref="RestServiceAppender" /> class.
        /// </summary>
        /// <remarks>
        /// Public default constructor to initialize a new instance of this class.
        /// </remarks>
        public RestServiceAppender()
            :this(new HttpClient())
        {
           
        }

        #endregion // Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the Logging endpoint to sending log requests.
        /// </summary>
        /// <value>
        /// http or https based Url formatted end point should be define 
        /// </value>
        public string LoggingEndpoint { get; set; }


        /// <summary>
        /// Gets or sets the logging endpoint accepted content type property. I recommend use for application/json
        /// </summary>
        /// <value>
        /// The rest service carry content with that format.
        /// </value>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the logging endpoint accepted HttpMethod
        /// </summary>
        /// <value>
        /// Values can only be POST or PUT etc. Because rest services should carry service data via rest body, 
        /// and body formatting options limited for rest services.
        /// </value>
        public string HttpMethod { get; set; }

        /// <summary>
        /// Name of appender.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// That property says to appender, include all event data to rest service for advanced logging
        /// </summary>
        /// <value>
        /// If value set as true, loggingEvent will serialize end put the rest request, othervise it will only contains parameters. Defaults set for false
        /// </value>
        public bool IncludeAllFields { get; set; }

        #endregion // Public Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Adds a parameter to the command.
        /// </summary>
        /// <param name="parameter">The parameter to add to the command.</param>
        /// <remarks>
        /// <para>
        /// Adds a parameter to the ordered list of command parameters.
        /// </para>
        /// </remarks>
        public void AddParameter(RestAppenderParameter parameter)
        {
            m_parameters.Add(parameter);
        }

        #endregion // Public Instance Methods

        #region Private Instance Methods


        private string BindParameters(LoggingEvent loggingEvent)
        {
            var restFields = new Dictionary<string, object>();

            foreach (RestAppenderParameter item in m_parameters)
                item.FormatValue(restFields, loggingEvent);

            if (IncludeAllFields)
                restFields.Add("DetailFields", JsonConvert.SerializeObject(loggingEvent));

            return JsonConvert.SerializeObject(restFields);
        }

    

        /// <summary>
        /// Call Rest Service from here.
        /// This method send log events to rest endpoint. Message content send as string parameter for here
        /// </summary>
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

        #endregion //Private Instance Methods

        #region Implementation of IAppender

        /// <summary>
        /// Close method is not implemented because rest queries not contains any file or stream object require to close
        /// </summary>
        public void Close()
        {

        }

        /// <summary>
        /// Send LoggngEvents to Rest Queries
        /// BindParameter generates Request Json body based on logging event paremeter
        /// </summary>
        /// <param name="loggingEvent">the LoggingEvent that contains log details</param>
        public void DoAppend(LoggingEvent loggingEvent)
        {
            SendLogs(BindParameters(loggingEvent));
        }

        #endregion //Implementation of IAppender

        #region Implementation of IBulkAppender

        /// <summary>
        /// Send LoggngEvents to Rest Queries
        /// BindParameter generates Request Json body based on logging event paremeter. This method gets an array of logging event and sends via rest service in loop
        /// </summary>
        /// <param name="loggingEvents">the LoggingEvent that contains log details as array for bulk appender. </param>
        public void DoAppend(LoggingEvent[] loggingEvents)
        {
            foreach (var item in loggingEvents)
            {
                SendLogs(BindParameters(item));
            }
        }

        #endregion //Implementation of IBulkAppender

        #region Implementation of IOptionHandler

        /// <summary>
        /// Initialize the appender based on the options set
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
        public void ActivateOptions()
        {
            _httpClient.BaseAddress = new Uri(LoggingEndpoint);

        }

        #endregion //  Implementation of IOptionHandler

        #region Implementation of IErrorHandler


        /// <summary>
        /// Internal Error Handler Implementation <see cref="IErrorHandler"/>
        /// </summary>
        /// <param name="message">Message from internal error handler</param>
        /// <param name="e">Logging Exception parameter from internal error</param>
        /// <param name="errorCode">Error Code from internal error</param>
        public void Error(string message, Exception e, ErrorCode errorCode)
        {
            Console.WriteLine($"{message} - {e.StackTrace} - {errorCode}");
        }
         
        /// <summary>
        /// Internal Error Handler Implementation Overload. This logs internal error via overload method
        /// </summary>
        /// <param name="message">Message from internal error handler</param>
        /// <param name="e">Logging exception parameter from internal error</param>
        /// <remarks>
        /// <para>
        /// Overload method has an additional parameter for <see cref="ErrorCode" /> is set as <see cref="ErrorCode.GenericFailure"/>
        /// </para>
        /// </remarks>
        public void Error(string message, Exception e)
        {
            Error(message, e, ErrorCode.GenericFailure);
        }

        /// <summary>
        /// Internal Error Handler Implementation Overload. This logs internal error via overload method
        /// </summary>
        /// <param name="message">Message from internal error handler</param>
        /// <param name="e">Logging exception parameter from internal error</param>
        /// <remarks>
        /// <para>
        /// Overload method has an additional parameter for <see cref="Exception"/> it set as Null 
        /// Overload method has an additional parameter for <see cref="ErrorCode" /> is set as <see cref="ErrorCode.GenericFailure"/>
        /// </para>
        /// </remarks>
        public void Error(string message)
        {
            this.Error(message, null, ErrorCode.GenericFailure);
        }


        #endregion //Implementation of IErrorHandler

        #region Protected Instance Fields

        /// <summary>
        /// The list of <see cref="RestAppenderParameter"/> objects.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The list of <see cref="RestAppenderParameter"/> objects.
        /// </para>
        /// </remarks>
        protected ArrayList m_parameters;

        #endregion //  Protected Instance Fields

        #region Private Static Fields

        /// <summary>
        /// The fully qualified type of the RestServiceAppender class.
        /// </summary>
        /// <remarks>
        /// Used by the internal logger to record the Type of the
        /// log message.
        /// </remarks>
        private readonly static Type declaringType = typeof(RestServiceAppender);

        #endregion Private Static Fields

    }


    /// <summary>
    /// Parameter type used by the <see cref="RestAppenderParameter"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides the basic Dictionary Object that contains Key,Value pairs for logging events.
    /// </para>
    /// <para>This type can be subclassed to provide database specific
    /// functionality. The one method that is called externally
    /// <see cref="FormatValue"/>.
    /// </para>
    /// </remarks>
    public class RestAppenderParameter
    {

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RestAppenderParameter" /> class.
        /// </summary>
        /// <remarks>
        /// Default constructor for the RestAppenderParameter class.
        /// </remarks>
        public RestAppenderParameter()
        {

        }

        #endregion // Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the name of this parameter.
        /// </summary>
        /// <value>
        /// The name of this parameter.
        /// </value>
        /// <remarks>
        /// <para>
        /// The name of this parameter. The parameter name
        /// must match up to a json property of rest body
        /// </para>
        /// </remarks>
        public string ParameterName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the <see cref="IRawLayout"/> to use to 
        /// render the logging event into an object for this 
        /// parameter.
        /// </summary>
        /// <value>
        /// The <see cref="IRawLayout"/> used to render the
        /// logging event into an object for this parameter.
        /// </value>
        /// <remarks>
        /// <para>
        /// The <see cref="IRawLayout"/> that renders the value for this
        /// parameter.
        /// </para>
        /// <para>
        /// The <see cref="RawLayoutConverter"/> can be used to adapt
        /// any <see cref="ILayout"/> into a <see cref="IRawLayout"/>
        /// for use in the property.
        /// </para>
        /// </remarks>
        public IRawLayout Layout
        {
            get;
            set;
        }

        #endregion // Public Instance Properties

        #region Public Instance Methods
        /// <summary>
        /// Renders the logging event and set the parameter value of rest body object
        /// </summary>
        /// <param name="values">The values containing the rest body parameters.</param>
        /// <param name="loggingEvent">The event to be rendered.</param>
        /// <remarks>
        /// <para>
        /// Renders the logging event using this parameters layout
        /// object. Sets the value of the parameter on the values of the rest body object.
        /// </para>
        /// </remarks>
        public virtual void FormatValue(Dictionary<string, object> values, LoggingEvent loggingEvent)
        {
            object formattedValue = Layout.Format(loggingEvent);

            values.Add(ParameterName, formattedValue);
        }
        #endregion // Public Instance Methods

    }
}
