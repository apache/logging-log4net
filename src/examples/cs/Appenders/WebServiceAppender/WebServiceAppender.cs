using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Web.Services.Description;

using log4net.Core;
using log4net.Layout;
using log4net.Util;

namespace log4net.Appender
{
    /// <summary>
    /// Appender that logs to a web service.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="WebServiceAppender"/> appends logging events to a web service. 
    /// The appender can be configured to specify the web service's URL by setting 
    /// the <see cref="serviceUrl"/> property. 
    /// The service name can be specified by setting the <see cref="serviceName"/>
    /// property.
    /// The method name can be specified by setting the <see cref="methodName"/>
    /// property. 
    /// </para>
    /// <para>
    /// The web service can take any number of parameters.  All parameters are accepted
    /// as strings. Parameters are added using the <see cref="AddParameter"/>
    /// method. This adds a single <see cref="WebServiceAppenderParameter"/> to the
    /// list of parameters. The <see cref="WebServiceAppenderParameter"/>
    /// type may be subclassed if required to provide specific
    /// functionality. The <see cref="WebServiceAppenderParameter"/> specifies
    /// the parameter name and how the value should be generated using a <see cref="ILayout"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// An example configuration to log to a simple web service:
    /// <code lang="XML" escaped="true">
    /// <appender name="WebServiceAppender" type="log4net.Appender.WebServiceAppender" >
    ///   <serviceUrl value="http://example.com/LogService.asmx" />
    ///	  <serviceName value="LogService" />
	///	  <methodName value="LogError" />
    ///   <parameter>
    ///     <parameterName value="Date" />
    ///     <layout type="log4net.Layout.RawTimeStampLayout"/>
    ///   </parameter>
    ///   <parameter>
    ///     <parameterName value="Level" />
    ///     <layout type="log4net.Layout.PatternLayout" value="%level" />
    ///   </parameter>
    ///   <parameter>
    ///     <parameterName value="Message" />
    ///     <layout type="log4net.Layout.PatternLayout" value="%message" />
    ///   </parameter>
    ///   <parameter>
	///		<parameterName value="Exception"/>
	///		<layout type="log4net.Layout.ExceptionLayout"/>
	///	  </parameter>
    /// </appender>
    /// </code>
    /// </example>
    /// <author>Brandon Wood</author>
    public class WebServiceAppender : AppenderSkeleton
    {
        #region Public Instance Constructors

        /// <summary> 
        /// Initializes a new instance of the <see cref="WebServiceAppender" /> class.
        /// </summary>
        /// <remarks>
        /// Public default constructor to initialize a new instance of this class.
        /// </remarks>
        public WebServiceAppender()
        {
            this.m_parameters = new List<WebServiceAppenderParameter>();
        }

        #endregion // Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// The web service URL.
        /// </summary>
        public string ServiceUrl
        {
            get { return this.m_serviceUrl; }
            set { this.m_serviceUrl = value; }
        }

        /// <summary>
        /// The web service name.
        /// </summary>
        public string ServiceName
        {
            get { return this.m_serviceName; }
            set { this.m_serviceName = value; }
        }

        /// <summary>
        /// The web service method name.
        /// </summary>
        public string MethodName
        {
            get { return this.m_methodName; }
            set { this.m_methodName = value; }
        }

        #endregion // Public Instance Properties

        #region Override implementation of AppenderSkeleton

        /// <summary>
        /// Sends the logging event to a web service.
        /// </summary>
        /// <param name="loggingEvent">The event to log.</param>
        /// <remarks>
        /// <para>
        /// Sends the logging event to a web service.
        /// </para>
        /// </remarks>
        override protected void Append(LoggingEvent loggingEvent)
        {
            InitializeWebServiceConnection();

            if (this.m_webServiceParameters != null && this.m_webServiceParameters.Length > 0)
            {
                string[] args = new string[this.m_webServiceParameters.Length];
                int paramIndex = 0;

                // use paramInfo array to supply web service parameter values in the correct order
                foreach (ParameterInfo paramInfo in this.m_webServiceParameters)
                {
                    // default all parameters to empty string
                    args[paramIndex] = string.Empty;

                    foreach (WebServiceAppenderParameter param in this.m_parameters)
                    {
                        if (paramInfo.Name.ToUpper() == param.ParameterName.ToUpper())
                        {
                            args[paramIndex] = param.FormatValue(loggingEvent);
                            break;
                        }
                    }

                    paramIndex++;
                }

                this.m_webMethodInfo.Invoke(this.m_webServiceInstance, args);
            }
        }

        #endregion // Override implementation of AppenderSkeleton

        #region Public Instance Methods

        /// <summary>
        /// Adds a parameter to send to the web service.
        /// </summary>
        /// <param name="parameter">The parameter to add to the web service method call.</param>
        /// <remarks>
        /// <para>
        /// Adds a parameter to send to the web service method call.
        /// </para>
        /// </remarks>
        public void AddParameter(WebServiceAppenderParameter parameter)
        {
            m_parameters.Add(parameter);
        }        

        #endregion // Public Instance Methods

        #region Private Instance Methods

        /// <summary>
        /// Connects to the web service.
        /// </summary>
        private void InitializeWebServiceConnection()
        {
            ServiceDescription description;

            try
            {
                // connect to the web service
                System.Net.WebClient client = new System.Net.WebClient();
                System.IO.Stream stream = client.OpenRead(this.m_serviceUrl + "?wsdl");

                // read the WSDL file describing the service.
                description = ServiceDescription.Read(stream);
            }
            catch (Exception ex)
            {
                ErrorHandler.Error("Error reading the web service definition.", ex);
                return;
            }

            // Initialize a service description importer.
            ServiceDescriptionImporter importer = new ServiceDescriptionImporter();
            importer.ProtocolName = "Soap12"; // Use SOAP 1.2.
            importer.AddServiceDescription(description, null, null);
            // Generate a proxy client.
            importer.Style = ServiceDescriptionImportStyle.Client;
            // Generate properties to represent primitive values.
            importer.CodeGenerationOptions = System.Xml.Serialization.CodeGenerationOptions.GenerateProperties;

            // Initialize a Code-DOM tree into which we will import the service.
            CodeNamespace codeNamespace = new CodeNamespace();
            CodeCompileUnit compileUnit = new CodeCompileUnit();
            compileUnit.Namespaces.Add(codeNamespace);

            // Import the service into the Code-DOM tree. This creates proxy code that uses the service.
            ServiceDescriptionImportWarnings warning = importer.Import(codeNamespace, compileUnit);

            if (warning != 0)
            {
                ErrorHandler.Error("Error importing the web service definition.");
                return;
            }

            // Generate the proxy code
            CodeDomProvider domProvider = CodeDomProvider.CreateProvider("CSharp");

            // Compile the assembly proxy with the appropriate references
            string[] assemblyReferences = new string[5] { "System.dll", "System.Web.Services.dll", "System.Web.dll", "System.Xml.dll", "System.Data.dll" };
            CompilerParameters compilerParams = new CompilerParameters(assemblyReferences);
            CompilerResults compilerResults = domProvider.CompileAssemblyFromDom(compilerParams, compileUnit);

            if (compilerResults.Errors.Count > 0)
            {
                StringBuilder errorMessages = new StringBuilder();
                foreach (CompilerError error in compilerResults.Errors)
                {
                    errorMessages.AppendFormat("{0}\n", error.ErrorText);
                }

                ErrorHandler.Error("Error compiling web service:\n" + errorMessages.ToString());
                return;
            }

            try
            {
                // get instance of web service
                m_webServiceInstance = compilerResults.CompiledAssembly.CreateInstance(this.m_serviceName);
                m_webMethodInfo = m_webServiceInstance.GetType().GetMethod(this.m_methodName);
                this.m_webServiceParameters = m_webMethodInfo.GetParameters();
            }
            catch (Exception ex)
            {
                ErrorHandler.Error("Error creating web service instance.", ex);
                return;
            }
        }

        #endregion // Private Instance Methods

        #region Protected Instance Fields

        /// <summary>
        /// The list of <see cref="WebServiceAppenderParameter"/> objects.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The list of <see cref="WebServiceAppenderParameter"/> objects.
        /// </para>
        /// </remarks>
        protected List<WebServiceAppenderParameter> m_parameters;

        /// <summary>
        /// <see cref="System.Reflection.ParameterInfo"/> array containing details of the web service parameters.
        /// </summary>
        protected ParameterInfo[] m_webServiceParameters;

        /// <summary>
        /// Object containing the web service instance.
        /// </summary>
        protected object m_webServiceInstance;

        /// <summary>
        /// <see cref="System.Reflection.MethodInfo"/> containing details of the web service method.
        /// </summary>
        protected MethodInfo m_webMethodInfo;

        #endregion // Protected Instance Fields

        #region Private Instance Fields

        /// <summary>
        /// Web service URL.
        /// </summary>
        private string m_serviceUrl;

        /// <summary>
        /// Web service name.
        /// </summary>
        private string m_serviceName;

        /// <summary>
        /// Web service method name.
        /// </summary>
        private string m_methodName;

        #endregion // Private Instance Fields
    }

    public class WebServiceAppenderParameter
    {
        #region Public Instance Methods

        /// <summary>
		/// Renders the logging event and set the parameter value in the command.
		/// </summary>
		/// <param name="command">The command containing the parameter.</param>
		/// <param name="loggingEvent">The event to be rendered.</param>
		/// <remarks>
		/// <para>
		/// Renders the logging event using this parameters layout
		/// object. Sets the value of the parameter on the command object.
		/// </para>
		/// </remarks>
		virtual public string FormatValue(LoggingEvent loggingEvent)
		{
			// Format the value
			object formattedValue = Layout.Format(loggingEvent);

			// If the value is null then return an empty string
			if (formattedValue == null)
			{
				formattedValue = string.Empty;
			}

			return formattedValue.ToString();
		}

		#endregion // Public Instance Methods

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
        /// must match up to a named parameter of the web service method.
        /// </para>
        /// </remarks>
        public string ParameterName
        {
            get { return m_parameterName; }
            set { m_parameterName = value; }
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
			get { return m_layout; }
			set { m_layout = value; }
		}

		#endregion // Public Instance Properties

        #region Private Instance Fields

        /// <summary>
        /// The name of this parameter.
        /// </summary>
        private string m_parameterName;

        /// <summary>
        /// The <see cref="IRawLayout"/> to use to render the
        /// logging event into an object for this parameter.
        /// </summary>
        private IRawLayout m_layout;

        #endregion // Private Instance Fields
    }
}