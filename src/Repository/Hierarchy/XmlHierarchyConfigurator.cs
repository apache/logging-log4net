#region Copyright & License
//
// Copyright 2001-2004 The Apache Software Foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
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
using System.Reflection;
using System.Xml;

using log4net.Appender;
using log4net.Layout;
using log4net.Filter;
using log4net.Util;
using log4net.Core;
using log4net.ObjectRenderer;

namespace log4net.Repository.Hierarchy
{
	/// <summary>
	/// Initializes the log4net environment using an Xml tree.
	/// </summary>
	/// <remarks>
	/// Configures a <see cref="Hierarchy"/> using an Xml tree.
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class XmlHierarchyConfigurator
	{
		private enum ConfigUpdateMode {Merge, Overwrite};

		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlHierarchyConfigurator" /> class
		/// with the specified <see cref="Hierarchy" />.
		/// </summary>
		/// <param name="hierarchy">The hierarchy to build.</param>
		public XmlHierarchyConfigurator(Hierarchy hierarchy) 
		{
			m_hierarchy = hierarchy;
			m_appenderBag = new Hashtable();
		}

		#endregion Public Instance Constructors

		#region Public Instance Methods

		/// <summary>
		/// Configures the log4net framework by parsing a DOM tree of XML elements.
		/// </summary>
		/// <param name="element">The root element to parse.</param>
		public void Configure(XmlElement element) 
		{
			if (element == null || m_hierarchy == null)
			{
				return;
			}

			string rootElementName = element.LocalName;

			if (rootElementName != CONFIGURATION_TAG)
			{
				LogLog.Error("XmlConfigurator: Xml element is - not a <" + CONFIGURATION_TAG + "> element.");
				return;
			}

			if (!LogLog.InternalDebugging)
			{
				// Look for a debug attribute to enable internal debug
				string debugAttribute = element.GetAttribute(INTERNAL_DEBUG_ATTR);
				LogLog.Debug("XmlConfigurator: "+INTERNAL_DEBUG_ATTR+" attribute [" + debugAttribute + "].");

				if (debugAttribute.Length>0 && debugAttribute != "null") 
				{	  
					LogLog.InternalDebugging = OptionConverter.ToBoolean(debugAttribute, true);
				}
				else 
				{
					LogLog.Debug("XmlConfigurator: Ignoring " + INTERNAL_DEBUG_ATTR + " attribute.");
				}

				string confDebug = element.GetAttribute(CONFIG_DEBUG_ATTR);
				if (confDebug.Length>0 && confDebug != "null")
				{	  
					LogLog.Warn("XmlConfigurator: The \"" + CONFIG_DEBUG_ATTR + "\" attribute is deprecated.");
					LogLog.Warn("XmlConfigurator: Use the \"" + INTERNAL_DEBUG_ATTR + "\" attribute instead.");
					LogLog.InternalDebugging = OptionConverter.ToBoolean(confDebug, true);
				}
			}

			// Default mode is merge
			ConfigUpdateMode configUpdateMode = ConfigUpdateMode.Merge;

			// Look for the config update attribute
			string configUpdateModeAttribute = element.GetAttribute(CONFIG_UPDATE_MODE_ATTR);
			if (configUpdateModeAttribute != null && configUpdateModeAttribute.Length > 0)
			{
				// Parse the attribute
				try
				{
					configUpdateMode = (ConfigUpdateMode)OptionConverter.ConvertStringTo(typeof(ConfigUpdateMode), configUpdateModeAttribute);
				}
				catch
				{
					LogLog.Error("XmlConfigurator: Invalid " + CONFIG_UPDATE_MODE_ATTR + " attribute value [" + configUpdateModeAttribute + "]");
				}
			}

#if (!NETCF)
			LogLog.Debug("XmlConfigurator: Configuration update mode [" + configUpdateMode.ToString(CultureInfo.InvariantCulture) + "].");
#else
			LogLog.Debug("XmlConfigurator: Configuration update mode [" + configUpdateMode.ToString() + "].");
#endif

			// Only reset configuration if overwrite flag specified
			if (configUpdateMode == ConfigUpdateMode.Overwrite)
			{
				// Reset to original unset configuration
				m_hierarchy.ResetConfiguration();
				LogLog.Debug("XmlConfigurator: Configuration reset before reading config.");
			}

			/* Building Appender objects, placing them in a local namespace
			   for future reference */

			/* Process all the top level elements */

			foreach (XmlNode currentNode in element.ChildNodes)
			{
				if (currentNode.NodeType == XmlNodeType.Element) 
				{
					XmlElement currentElement = (XmlElement)currentNode;

					if (currentElement.LocalName == LOGGER_TAG)
					{
						ParseLogger(currentElement);
					} 
					else if (currentElement.LocalName == CATEGORY_TAG)
					{
						// TODO: deprecated use of category
						ParseLogger(currentElement);
					} 
					else if (currentElement.LocalName == ROOT_TAG)
					{
						ParseRoot(currentElement);
					} 
					else if (currentElement.LocalName == RENDERER_TAG)
					{
						ParseRenderer(currentElement);
					}
					else if (currentElement.LocalName == APPENDER_TAG)
					{
						// We ignore appenders in this pass. They will
						// be found and loaded if they are referenced.
					}
					else
					{
						// Read the param tags and set properties on the hierarchy
						SetParameter(currentElement, m_hierarchy);
					}
				}
			}

			// Lastly set the hierarchy threshold
			string thresholdStr = element.GetAttribute(THRESHOLD_ATTR);
			LogLog.Debug("XmlConfigurator: Hierarchy Threshold [" + thresholdStr + "]");
			if (thresholdStr.Length > 0 && thresholdStr != "null") 
			{
				Level thresholdLevel = (Level) ConvertStringTo(typeof(Level), thresholdStr);
				if (thresholdLevel != null)
				{
					m_hierarchy.Threshold = thresholdLevel;
				}
				else
				{
					LogLog.Warn("XmlConfigurator: Unable to set hierarchy threshold using value [" + thresholdStr + "] (with acceptable conversion types)");
				}
			}

			// Done reading config
		}

		#endregion Public Instance Methods

		#region Protected Instance Methods

		/// <summary>
		/// Parse appenders by IDREF.
		/// </summary>
		/// <param name="appenderRef">The appender ref element.</param>
		/// <returns>The instance of the appender that the ref refers to.</returns>
		protected IAppender FindAppenderByReference(XmlElement appenderRef) 
		{	
			string appenderName = appenderRef.GetAttribute(REF_ATTR);

			IAppender appender = (IAppender)m_appenderBag[appenderName];
			if (appender != null) 
			{
				return appender;
			} 
			else 
			{
				// Find the element with that id
				XmlElement element = null;

				if (appenderName != null && appenderName.Length > 0)
				{
					foreach (XmlNode node in appenderRef.OwnerDocument.GetElementsByTagName(APPENDER_TAG))
					{
						if (((XmlElement)node).GetAttribute("name") == appenderName)
						{
							element = (XmlElement)node;
							break;
						}
					}
				}

				if (element == null) 
				{
					LogLog.Error("XmlConfigurator: No appender named [" + appenderName + "] could be found."); 
					return null;
				} 
				else
				{
					appender = ParseAppender(element);
					if (appender != null)
					{
						m_appenderBag[appenderName] = appender;
					}
					return appender;
				}
			} 
		}

		/// <summary>
		/// Parses an appender element.
		/// </summary>
		/// <param name="appenderElement">The appender element.</param>
		/// <returns>The appender instance or <c>null</c> when parsing failed.</returns>
		protected IAppender ParseAppender(XmlElement appenderElement) 
		{
			string appenderName = appenderElement.GetAttribute(NAME_ATTR);
			string typeName = appenderElement.GetAttribute(TYPE_ATTR);

			LogLog.Debug("XmlConfigurator: Loading Appender [" + appenderName + "] type: [" + typeName + "]");
			try 
			{
				IAppender appender = (IAppender)SystemInfo.GetTypeFromString(typeName, true, true).GetConstructor(SystemInfo.EmptyTypes).Invoke(BindingFlags.Public | BindingFlags.Instance, null, new object[0], CultureInfo.InvariantCulture);
				appender.Name = appenderName;

				foreach (XmlNode currentNode in appenderElement.ChildNodes)
				{
					/* We're only interested in Elements */
					if (currentNode.NodeType == XmlNodeType.Element) 
					{
						XmlElement currentElement = (XmlElement)currentNode;

						// Look for the appender ref tag
						if (currentElement.LocalName == APPENDER_REF_TAG)
						{
							string refName = currentElement.GetAttribute(REF_ATTR);
							if (appender is IAppenderAttachable) 
							{
								IAppenderAttachable aa = (IAppenderAttachable) appender;
								LogLog.Debug("XmlConfigurator: Attaching appender named [" + refName + "] to appender named [" + appender.Name + "].");
								IAppender a = FindAppenderByReference(currentElement);
								if (a != null)
								{
									aa.AddAppender(a);
								}
							} 
							else 
							{
								LogLog.Error("XmlConfigurator: Requesting attachment of appender named ["+refName+ "] to appender named [" + appender.Name + "] which does not implement log4net.Core.IAppenderAttachable.");
							}
						}
						else
						{
							// For all other tags we use standard set param method
							SetParameter(currentElement, appender);
						}
					}
				}
				if (appender is IOptionHandler) 
				{
					((IOptionHandler) appender).ActivateOptions();
				}

				LogLog.Debug("XmlConfigurator: Created Appender [" + appenderName + "]");	
				return appender;
			}
				/* Yes, it's ugly.  But all of these exceptions point to the same problem: we can't create an Appender */
			catch (Exception ex) 
			{
				LogLog.Error("XmlConfigurator: Could not create Appender [" + appenderName + "] of type [" + typeName + "]. Reported error follows.", ex);
				return null;
			}
		}

		/// <summary>
		/// Parses a logger element.
		/// </summary>
		/// <param name="loggerElement">The logger element.</param>
		protected void ParseLogger(XmlElement loggerElement) 
		{
			// Create a new log4net.Logger object from the <logger> element.
			string loggerName = loggerElement.GetAttribute(NAME_ATTR);

			LogLog.Debug("XmlConfigurator: Retrieving an instance of log4net.Repository.Logger for logger [" + loggerName + "].");
			Logger log = m_hierarchy.GetLogger(loggerName) as Logger;

			// Setting up a logger needs to be an atomic operation, in order
			// to protect potential log operations while logger
			// configuration is in progress.
			lock(log) 
			{
				bool additivity = OptionConverter.ToBoolean(loggerElement.GetAttribute(ADDITIVITY_ATTR), true);
	
				LogLog.Debug("XmlConfigurator: Setting [" + log.Name + "] additivity to [" + additivity + "].");
				log.Additivity = additivity;
				ParseChildrenOfLoggerElement(loggerElement, log, false);
			}
		}

		/// <summary>
		/// Parses the root logger element.
		/// </summary>
		/// <param name="rootElement">The root element.</param>
		protected  void ParseRoot(XmlElement rootElement) 
		{
			Logger root = m_hierarchy.Root;
			// logger configuration needs to be atomic
			lock(root) 
			{	
				ParseChildrenOfLoggerElement(rootElement, root, true);
			}
		}

		/// <summary>
		/// Parses the children of a logger element.
		/// </summary>
		/// <param name="catElement">The category element.</param>
		/// <param name="log">The logger instance.</param>
		/// <param name="isRoot">Flag to indicate if the logger is the root logger.</param>
		protected void ParseChildrenOfLoggerElement(XmlElement catElement, Logger log, bool isRoot) 
		{
			// Remove all existing appenders from log. They will be
			// reconstructed if need be.
			log.RemoveAllAppenders();

			foreach (XmlNode currentNode in catElement.ChildNodes)
			{
				if (currentNode.NodeType == XmlNodeType.Element) 
				{
					XmlElement currentElement = (XmlElement) currentNode;
	
					if (currentElement.LocalName == APPENDER_REF_TAG)
					{
						IAppender appender = FindAppenderByReference(currentElement);
						string refName =  currentElement.GetAttribute(REF_ATTR);
						if (appender != null)
						{
							LogLog.Debug("XmlConfigurator: Adding appender named [" + refName + "] to logger [" + log.Name + "].");
							log.AddAppender(appender);
						}
						else 
						{
							LogLog.Error("XmlConfigurator: Appender named [" + refName + "] not found.");
						}
					} 
					else if (currentElement.LocalName == LEVEL_TAG || currentElement.LocalName == PRIORITY_TAG) 
					{
						ParseLevel(currentElement, log, isRoot);	
					} 
					else
					{
						SetParameter(currentElement, log);
					}
				}
			}
			if (log is IOptionHandler) 
			{
				((IOptionHandler) log).ActivateOptions();
			}
		}

		/// <summary>
		/// Parses an object renderer.
		/// </summary>
		/// <param name="element">The renderer element.</param>
		protected void ParseRenderer(XmlElement element) 
		{
			string renderingClassName = element.GetAttribute(RENDERING_TYPE_ATTR);
			string renderedClassName = element.GetAttribute(RENDERED_TYPE_ATTR);

			LogLog.Debug("XmlConfigurator: Rendering class [" + renderingClassName + "], Rendered class [" + renderedClassName + "].");
			IObjectRenderer renderer = (IObjectRenderer)OptionConverter.InstantiateByClassName(renderingClassName, typeof(IObjectRenderer), null);
			if (renderer == null) 
			{
				LogLog.Error("XmlConfigurator: Could not instantiate renderer [" + renderingClassName + "].");
				return;
			} 
			else 
			{
				try 
				{
					m_hierarchy.RendererMap.Put(SystemInfo.GetTypeFromString(renderedClassName, true, true), renderer);
				} 
				catch(Exception e) 
				{
					LogLog.Error("XmlConfigurator: Could not find class [" + renderedClassName + "].", e);
				}
			}
		}

		/// <summary>
		/// Parses a level element.
		/// </summary>
		/// <param name="element">The level element.</param>
		/// <param name="log">The logger object to set the level on.</param>
		/// <param name="isRoot">Flag to indicate if the logger is the root logger.</param>
		protected void ParseLevel(XmlElement element, Logger log, bool isRoot) 
		{
			string loggerName = log.Name;
			if (isRoot) 
			{
				loggerName = "root";
			}

			string levelStr = element.GetAttribute(VALUE_ATTR);
			LogLog.Debug("XmlConfigurator: Logger [" + loggerName + "] Level string is [" + levelStr + "].");
	
			if (INHERITED == levelStr) 
			{
				if (isRoot) 
				{
					LogLog.Error("XmlConfigurator: Root level cannot be inherited. Ignoring directive.");
				} 
				else 
				{
					LogLog.Debug("XmlConfigurator: Logger [" + loggerName + "] level set to inherit from parent.");	
					log.Level = null;
				}
			} 
			else 
			{
				log.Level = log.Hierarchy.LevelMap[levelStr];
				if (log.Level == null)
				{
					LogLog.Error("XmlConfigurator: Undefined level [" + levelStr + "] on Logger [" + loggerName + "].");
				}
				else
				{
					LogLog.Debug("XmlConfigurator: Logger [" + loggerName + "] level set to [name=\"" + log.Level.Name + "\",value=" + log.Level.Value + "].");	
				}
			}
		}

		/// <summary>
		/// Sets a parameter on an object.
		/// </summary>
		/// <remarks>
		/// The parameter name must correspond to a writable property
		/// on the object. The value of the parameter is a string,
		/// therefore this function will attempt to set a string
		/// property first. If unable to set a string property it
		/// will inspect the property and its argument type. It will
		/// attempt to call a static method called 'Parse' on the
		/// type of the property. This method will take a single
		/// string argument and return a value that can be used to
		/// set the property.
		/// </remarks>
		/// <param name="element">The parameter element.</param>
		/// <param name="target">The object to set the parameter on.</param>
		protected void SetParameter(XmlElement element, object target) 
		{
			// Get the property name
			string name = element.GetAttribute(NAME_ATTR);

			// If the name attribute does not exist then use the name of the element
			if (element.LocalName != PARAM_TAG || name == null || name.Length == 0)
			{
				name = element.LocalName;
			}

			// Look for the property on the target object
			Type targetType = target.GetType();
			Type propertyType = null;

			PropertyInfo propInfo = null;
			MethodInfo methInfo = null;

			// Try to find a writable property
			propInfo = targetType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
			if (propInfo != null && propInfo.CanWrite)
			{
				// found a property
				propertyType = propInfo.PropertyType;
			}
			else
			{
				propInfo = null;

				// look for a method with the signature Add<property>(type)
				methInfo = FindMethodInfo(targetType, name);

				if (methInfo != null)
				{
					propertyType = methInfo.GetParameters()[0].ParameterType;
				}
			}

			if (propertyType == null)
			{
				LogLog.Error("XmlConfigurator: Cannot find Property [" + name + "] to set object on [" + target.ToString() + "]");
			}
			else
			{
				if (element.GetAttributeNode(VALUE_ATTR) != null)
				{
					string propertyValue = element.GetAttribute(VALUE_ATTR);

					// Fixup embedded non-printable chars
					propertyValue = OptionConverter.ConvertSpecialChars(propertyValue);

#if !NETCF	
					try
					{
						// Expand environment variables in the string.
						propertyValue = OptionConverter.SubstituteVariables(propertyValue, Environment.GetEnvironmentVariables());
					}
					catch(System.Security.SecurityException)
					{
						// This security exception will occur if the caller does not have 
						// unrestricted environment permission. If this occurs the expansion 
						// will be skipped with the following warning message.
						LogLog.Debug("XmlConfigurator: Security exception while trying to expand environment variables. Error Ignored. No Expansion.");
					}
#endif

					Type parsedObjectConversionTargetType = null;

					// Check if a specific subtype is specified on the element using the 'type' attribute
					string subTypeString = element.GetAttribute(TYPE_ATTR);
					if (subTypeString != null && subTypeString.Length > 0)
					{
						// Read the explicit subtype
						try
						{
							Type subType = SystemInfo.GetTypeFromString(subTypeString, true, true);

							LogLog.Debug("XmlConfigurator: Parameter ["+name+"] specified subtype ["+subType.FullName+"]");

							if (!propertyType.IsAssignableFrom(subType))
							{
								// Check if there is an appropriate type converter
								if (OptionConverter.CanConvertTypeTo(subType, propertyType))
								{
									// Must reconvert to real property type
									parsedObjectConversionTargetType = propertyType;

									// Use sub type as intermediary type
									propertyType = subType;
								}
								else
								{
									LogLog.Error("XmlConfigurator: Subtype ["+subType.FullName+"] set on ["+name+"] is not a subclass of property type ["+propertyType.FullName+"] and there are no acceptable type conversions.");
								}
							}
							else
							{
								// The subtype specified is found and is actually a subtype of the property
								// type, therefore we can switch to using this type.
								propertyType = subType;
							}
						}
						catch(Exception ex)
						{
							LogLog.Error("XmlConfigurator: Failed to find type ["+subTypeString+"] set on ["+name+"]", ex);
						}
					}

					// Now try to convert the string value to an acceptable type
					// to pass to this property.

					object convertedValue = ConvertStringTo(propertyType, propertyValue);
					
					// Check if we need to do an additional conversion
					if (convertedValue != null && parsedObjectConversionTargetType != null)
					{
						LogLog.Debug("XmlConfigurator: Performing additional conversion of value from [" + convertedValue.GetType().Name + "] to [" + parsedObjectConversionTargetType.Name + "]");
						convertedValue = OptionConverter.ConvertTypeTo(convertedValue, parsedObjectConversionTargetType);
					}

					if (convertedValue != null)
					{
						if (propInfo != null)
						{
							// Got a converted result
							LogLog.Debug("XmlConfigurator: Setting Property [" + propInfo.Name + "] to " + convertedValue.GetType().Name + " value [" + convertedValue.ToString() + "]");

							// Pass to the property
							propInfo.SetValue(target, convertedValue, BindingFlags.SetProperty, null, null, CultureInfo.InvariantCulture);
						}
						else if (methInfo != null)
						{
							// Got a converted result
							LogLog.Debug("XmlConfigurator: Setting Collection Property [" + methInfo.Name + "] to " + convertedValue.GetType().Name + " value [" + convertedValue.ToString() + "]");

							// Pass to the property
							methInfo.Invoke(target, BindingFlags.InvokeMethod, null, new object[] {convertedValue}, CultureInfo.InvariantCulture);
						}
					}
					else
					{
						LogLog.Warn("XmlConfigurator: Unable to set property [" + name + "] on object [" + target + "] using value [" + propertyValue + "] (with acceptable conversion types)");
					}
				}
				else
				{
					// No value specified
					Type defaultObjectType = null;
					if (propertyType.IsClass && !propertyType.IsAbstract)
					{
						defaultObjectType = propertyType;
					}

					object createdObject = CreateObjectFromXml(element, defaultObjectType, propertyType);

					if (createdObject == null)
					{
						LogLog.Error("XmlConfigurator: Failed to create object to set param: "+name);
					}
					else
					{
						if (propInfo != null)
						{
							// Got a converted result
							LogLog.Debug("XmlConfigurator: Setting Property ["+ propInfo.Name +"] to object ["+ createdObject +"]");

							// Pass to the property
							propInfo.SetValue(target, createdObject, BindingFlags.SetProperty, null, null, CultureInfo.InvariantCulture);
						}
						else if (methInfo != null)
						{
							// Got a converted result
							LogLog.Debug("XmlConfigurator: Setting Collection Property ["+ methInfo.Name +"] to object ["+ createdObject +"]");

							// Pass to the property
							methInfo.Invoke(target, BindingFlags.InvokeMethod, null, new object[] {createdObject}, CultureInfo.InvariantCulture);
						}
					}
				}
			}
		}

		/// <summary>
		/// Look for a method on the targetType that matches the name supplied
		/// </summary>
		/// <param name="targetType">the type that has the method</param>
		/// <param name="name">the name of the method</param>
		/// <returns>the method info found</returns>
		/// <remarks>
		/// The method must be a public instance method on the targetType.
		/// The method must be named <c>name</c> or "Add" followed by <c>name</c>.
		/// The method must take a single parameter.
		/// </remarks>
		private MethodInfo FindMethodInfo(Type targetType, string name)
		{
			string requiredMethodNameA = name;
			string requiredMethodNameB = "Add" + name;

			MethodInfo[] methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public);

			foreach(MethodInfo methInfo in methods)
			{
				if (methInfo.IsPublic && !methInfo.IsStatic)
				{
					if (string.Compare(methInfo.Name, requiredMethodNameA, true, System.Globalization.CultureInfo.InvariantCulture) == 0 ||
						string.Compare(methInfo.Name, requiredMethodNameB, true, System.Globalization.CultureInfo.InvariantCulture) == 0)
					{
						// Found matching method name

						// Look for version with one arg only
						System.Reflection.ParameterInfo[] methParams = methInfo.GetParameters();
						if (methParams.Length == 1)
						{
							return methInfo;
						}
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Converts a string value to a target type.
		/// </summary>
		/// <param name="type">The type of object to convert the string to.</param>
		/// <param name="value">The string value to use as the value of the object.</param>
		/// <returns>
		/// An object of type <paramref name="type"/> with value <paramref name="value"/> or 
		/// <c>null</c> when the conversion could not be performed.
		/// </returns>
		protected object ConvertStringTo(Type type, string value)
		{
			// Hack to allow use of Level in property
			if (type.IsAssignableFrom(typeof(Level)))
			{
				// Property wants a level
				Level levelValue = m_hierarchy.LevelMap[value];

				if (levelValue == null)
				{
					LogLog.Error("XmlConfigurator: Unknown Level Specified ["+ value +"]");
				}

				return levelValue;
			}
			return OptionConverter.ConvertStringTo(type, value);
		}

		/// <summary>
		/// Creates an object as specified in XML.
		/// </summary>
		/// <param name="element">The XML element that contains the definition of the object.</param>
		/// <param name="defaultTargetType">The object type to use if not explicitly specified.</param>
		/// <param name="typeConstraint">The type that the returned object must be or must inherit from.</param>
		/// <returns>The object or <c>null</c></returns>
		protected object CreateObjectFromXml(XmlElement element, Type defaultTargetType, Type typeConstraint) 
		{
			Type objectType = null;

			// Get the object type
			string objectTypeString = element.GetAttribute(TYPE_ATTR);
			if (objectTypeString == null || objectTypeString.Length == 0)
			{
				if (defaultTargetType == null)
				{
					LogLog.Error("XmlConfigurator: Object type not specified. Cannot create object.");
					return null;
				}
				else
				{
					// Use the default object type
					objectType = defaultTargetType;
				}
			}
			else
			{
				// Read the explicit object type
				try
				{
					objectType = SystemInfo.GetTypeFromString(objectTypeString, true, true);
				}
				catch(Exception ex)
				{
					LogLog.Error("XmlConfigurator: Failed to find type ["+objectTypeString+"]", ex);
					return null;
				}
			}

			bool requiresConversion = false;

			// Got the object type. Check that it meets the typeConstraint
			if (typeConstraint != null)
			{
				if (!typeConstraint.IsAssignableFrom(objectType))
				{
					// Check if there is an appropriate type converter
					if (OptionConverter.CanConvertTypeTo(objectType, typeConstraint))
					{
						requiresConversion = true;
					}
					else
					{
						LogLog.Error("XmlConfigurator: Object type ["+objectType.FullName+"] is not assignable to type ["+typeConstraint.FullName+"]. There are no acceptable type conversions.");
						return null;
					}
				}
			}

			// Look for the default constructor
			ConstructorInfo constInfo = objectType.GetConstructor(SystemInfo.EmptyTypes);
			if (constInfo == null)
			{
				LogLog.Error("XmlConfigurator: Failed to find default constructor for type [" + objectType.FullName + "]");
				return null;
			}

			// Call the constructor
			object createdObject = constInfo.Invoke(BindingFlags.Public | BindingFlags.Instance, null, new object[0], CultureInfo.InvariantCulture);

			// Set any params on object
			foreach (XmlNode currentNode in element.ChildNodes)
			{
				if (currentNode.NodeType == XmlNodeType.Element) 
				{
					SetParameter((XmlElement)currentNode, createdObject);
				}
			}

			// Check if we need to call ActivateOptions
			if (createdObject is IOptionHandler)
			{
				((IOptionHandler) createdObject).ActivateOptions();
			}

			// Ok object should be initialized

			if (requiresConversion)
			{
				// Convert the object type
				return OptionConverter.ConvertTypeTo(createdObject, typeConstraint);
			}
			else
			{
				// The object is of the correct type
				return createdObject;
			}
		}

		#endregion Protected Instance Methods

		#region Private Static Fields

		// String constants used while parsing the XML data
		private const string CONFIGURATION_TAG			= "log4net";
		private const string RENDERER_TAG				= "renderer";
		private const string APPENDER_TAG 				= "appender";
		private const string APPENDER_REF_TAG 			= "appender-ref";  
		private const string PARAM_TAG					= "param";

		// TODO: Deprecate use of category tags
		private const string CATEGORY_TAG				= "category";
		// TODO: Deprecate use of priority tag
		private const string PRIORITY_TAG				= "priority";

		private const string LOGGER_TAG					= "logger";
		private const string NAME_ATTR					= "name";
		private const string TYPE_ATTR					= "type";
		private const string VALUE_ATTR					= "value";
		private const string ROOT_TAG					= "root";
		private const string LEVEL_TAG					= "level";
		private const string REF_ATTR					= "ref";
		private const string ADDITIVITY_ATTR			= "additivity";  
		private const string THRESHOLD_ATTR				= "threshold";
		private const string CONFIG_DEBUG_ATTR			= "configDebug";
		private const string INTERNAL_DEBUG_ATTR		= "debug";
		private const string CONFIG_UPDATE_MODE_ATTR	= "update";
		private const string RENDERING_TYPE_ATTR		= "renderingClass";
		private const string RENDERED_TYPE_ATTR			= "renderedClass";

		// flag used on the level element
		private const string INHERITED = "inherited";

		#endregion Private Static Fields

		#region Private Instance Fields

		/// <summary>
		/// key: appenderName, value: appender.
		/// </summary>
		private Hashtable m_appenderBag;

		/// <summary>
		/// The Hierarchy being configured.
		/// </summary>
		private readonly Hierarchy m_hierarchy;

		#endregion Private Instance Fields
	}
}
