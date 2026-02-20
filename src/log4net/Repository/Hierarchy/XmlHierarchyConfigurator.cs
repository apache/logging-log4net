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
using System.Globalization;
using System.Reflection;
using System.Xml;

using log4net.Appender;
using log4net.Util;
using log4net.Core;
using log4net.ObjectRenderer;
using System.Linq;

namespace log4net.Repository.Hierarchy;

/// <summary>
/// Initializes the log4net environment using an XML DOM.
/// </summary>
/// <param name="hierarchy">The hierarchy to build.</param>
/// <author>Nicko Cadell</author>
/// <author>Gert Driesen</author>
public class XmlHierarchyConfigurator(Hierarchy hierarchy)
{
  private enum ConfigUpdateMode
  {
    Merge,
    Overwrite
  }

  /// <summary>
  /// Configure the hierarchy by parsing a DOM tree of XML elements.
  /// </summary>
  /// <param name="element">The root element to parse.</param>
  /// <remarks>
  /// <para>
  /// Configure the hierarchy by parsing a DOM tree of XML elements.
  /// </para>
  /// </remarks>
  public void Configure(XmlElement? element)
  {
    if (element is null)
    {
      return;
    }

    string rootElementName = element.LocalName;

    if (rootElementName != ConfigurationTag)
    {
      LogLog.Error(_declaringType, $"Xml element is - not a <{ConfigurationTag}> element.");
      return;
    }

    if (!LogLog.EmitInternalMessages)
    {
      // Look for a emitDebug attribute to enable internal debug
      string emitDebugAttribute = element.GetAttribute(EmitInternalDebugAttr);
      LogLog.Debug(_declaringType, $"{EmitInternalDebugAttr} attribute [{emitDebugAttribute}].");

      if (emitDebugAttribute.Length > 0 && emitDebugAttribute != "null")
      {
        LogLog.EmitInternalMessages = OptionConverter.ToBoolean(emitDebugAttribute, true);
      }
      else
      {
        LogLog.Debug(_declaringType, $"Ignoring {EmitInternalDebugAttr} attribute.");
      }
    }

    if (!LogLog.InternalDebugging)
    {
      // Look for a debug attribute to enable internal debug
      string debugAttribute = element.GetAttribute(InternalDebugAttr);
      LogLog.Debug(_declaringType, $"{InternalDebugAttr} attribute [{debugAttribute}].");

      if (debugAttribute.Length > 0 && debugAttribute != "null")
      {
        LogLog.InternalDebugging = OptionConverter.ToBoolean(debugAttribute, true);
      }
      else
      {
        LogLog.Debug(_declaringType, $"Ignoring {InternalDebugAttr} attribute.");
      }

      string confDebug = element.GetAttribute(ConfigDebugAttr);
      if (confDebug.Length > 0 && confDebug != "null")
      {
        LogLog.Warn(_declaringType, $"The \"{ConfigDebugAttr}\" attribute is deprecated.");
        LogLog.Warn(_declaringType, $"Use the \"{InternalDebugAttr}\" attribute instead.");
        LogLog.InternalDebugging = OptionConverter.ToBoolean(confDebug, true);
      }
    }

    // Default mode is merge
    ConfigUpdateMode? configUpdateMode = ConfigUpdateMode.Merge;

    // Look for the config update attribute
    string? configUpdateModeAttribute = element.GetAttribute(ConfigUpdateModeAttr);
    if (!string.IsNullOrEmpty(configUpdateModeAttribute))
    {
      // Parse the attribute
      try
      {
        if (OptionConverter.ConvertStringTo(typeof(ConfigUpdateMode), configUpdateModeAttribute) is ConfigUpdateMode mode)
        {
          configUpdateMode = mode;
        }
        else
        {
          LogLog.Error(_declaringType, $"Invalid {ConfigUpdateModeAttr} attribute value [{configUpdateModeAttribute}]");
        }
      }
      catch (Exception e) when (!e.IsFatal())
      {
        LogLog.Error(_declaringType, $"Invalid {ConfigUpdateModeAttr} attribute value [{configUpdateModeAttribute}]", e);
      }
    }

    // IMPL: The IFormatProvider argument to Enum.ToString() is deprecated in .NET 2.0
    LogLog.Debug(_declaringType, $"Configuration update mode [{configUpdateMode}].");

    // Only reset configuration if overwrite flag specified
    if (configUpdateMode == ConfigUpdateMode.Overwrite)
    {
      // Reset to original unset configuration
      hierarchy.ResetConfiguration();
      LogLog.Debug(_declaringType, "Configuration reset before reading config.");
    }

    /* Building Appender objects, placing them in a local namespace
       for future reference */

    /* Process all the top level elements */

    foreach (XmlNode currentNode in element.ChildNodes)
    {
      if (currentNode.NodeType == XmlNodeType.Element)
      {
        XmlElement currentElement = (XmlElement)currentNode;

        if (currentElement.LocalName == LoggerTag)
        {
          ParseLogger(currentElement);
        }
        else if (currentElement.LocalName == CategoryTag)
        {
          // TODO: deprecated use of category
          ParseLogger(currentElement);
        }
        else if (currentElement.LocalName == RootTag)
        {
          ParseRoot(currentElement);
        }
        else if (currentElement.LocalName == RendererTag)
        {
          ParseRenderer(currentElement);
        }
        else if (currentElement.LocalName == AppenderTag)
        {
          // We ignore appenders in this pass. They will
          // be found and loaded if they are referenced.
        }
        else
        {
          // Read the param tags and set properties on the hierarchy
          SetParameter(currentElement, hierarchy);
        }
      }
    }

    // Lastly set the hierarchy threshold
    string thresholdStr = element.GetAttribute(ThresholdAttr);
    LogLog.Debug(_declaringType, $"Hierarchy Threshold [{thresholdStr}]");
    if (thresholdStr.Length > 0 && thresholdStr != "null")
    {
      if (ConvertStringTo(typeof(Level), thresholdStr) is Level thresholdLevel)
      {
        hierarchy.Threshold = thresholdLevel;
      }
      else
      {
        LogLog.Warn(_declaringType, $"Unable to set hierarchy threshold using value [{thresholdStr}] (with acceptable conversion types)");
      }
    }

    // Done reading config
  }

  /// <summary>
  /// Parse appenders by IDREF.
  /// </summary>
  /// <param name="appenderRef">The appender ref element.</param>
  /// <returns>The instance of the appender that the ref refers to.</returns>
  /// <remarks>
  /// <para>
  /// Parse an XML element that represents an appender and return 
  /// the appender.
  /// </para>
  /// </remarks>
  protected IAppender? FindAppenderByReference(XmlElement appenderRef)
  {
    string? appenderName = appenderRef.EnsureNotNull().GetAttribute(RefAttr);

    if (_appenderBag.TryGetValue(appenderName, out IAppender? appender))
    {
      return appender;
    }

    // Find the element with that id
    XmlElement? element = null;

    if (!string.IsNullOrEmpty(appenderName) && appenderRef.OwnerDocument is not null)
    {
      foreach (XmlElement curAppenderElement in appenderRef.OwnerDocument.GetElementsByTagName(AppenderTag))
      {
        if (curAppenderElement.GetAttribute("name") == appenderName)
        {
          element = curAppenderElement;
          break;
        }
      }
    }

    if (element is null)
    {
      LogLog.Error(_declaringType, $"XmlHierarchyConfigurator: No appender named [{appenderName}] could be found.");
      return null;
    }

    appender = ParseAppender(element);
    if (appender is not null)
    {
      _appenderBag[appenderName] = appender;
    }
    return appender;
  }

  /// <summary>
  /// Parses an appender element.
  /// </summary>
  /// <param name="appenderElement">The appender element.</param>
  /// <returns>The appender instance or <see langword="null"/> when parsing failed.</returns>
  /// <remarks>
  /// <para>
  /// Parse an XML element that represents an appender and return
  /// the appender instance.
  /// </para>
  /// </remarks>
  protected IAppender? ParseAppender(XmlElement appenderElement)
  {
    string appenderName = appenderElement.EnsureNotNull().GetAttribute(NameAttr);
    string typeName = appenderElement.GetAttribute(TypeAttr);

    LogLog.Debug(_declaringType, $"Loading Appender [{appenderName}] type: [{typeName}]");
    try
    {
      IAppender appender = Activator.CreateInstance(
        SystemInfo.GetTypeFromString(typeName, true, true).EnsureNotNull()).EnsureIs<IAppender>();
      appender.Name = appenderName;

      foreach (XmlNode currentNode in appenderElement.ChildNodes)
      {
        // We're only interested in Elements
        if (currentNode.NodeType == XmlNodeType.Element)
        {
          XmlElement currentElement = (XmlElement)currentNode;

          // Look for the appender ref tag
          if (currentElement.LocalName == AppenderRefTag)
          {
            string refName = currentElement.GetAttribute(RefAttr);

            if (appender is IAppenderAttachable appenderContainer)
            {
              LogLog.Debug(_declaringType, $"Attaching appender named [{refName}] to appender named [{appender.Name}].");

              if (FindAppenderByReference(currentElement) is IAppender referencedAppender)
              {
                appenderContainer.AddAppender(referencedAppender);
              }
            }
            else
            {
              LogLog.Error(_declaringType, $"Requesting attachment of appender named [{refName}] to appender named [{appender.Name}] which does not implement log4net.Core.IAppenderAttachable.");
            }
          }
          else
          {
            // For all other tags we use standard set param method
            SetParameter(currentElement, appender);
          }
        }
      }

      if (appender is IOptionHandler optionHandler)
      {
        optionHandler.ActivateOptions();
      }

      LogLog.Debug(_declaringType, $"Created Appender [{appenderName}]");
      return appender;
    }
    catch (Exception e) when (!e.IsFatal())
    {
      // Yes, it's ugly.  But all exceptions point to the same problem: we can't create an Appender

      LogLog.Error(_declaringType, $"Could not create Appender [{appenderName}] of type [{typeName}]. Reported error follows.", e);
      return null;
    }
  }

  /// <summary>
  /// Parses a logger element.
  /// </summary>
  /// <param name="loggerElement">The logger element.</param>
  /// <remarks>
  /// <para>
  /// Parse an XML element that represents a logger.
  /// </para>
  /// </remarks>
  protected void ParseLogger(XmlElement loggerElement)
  {
    // Create a new log4net.Logger object from the <logger> element.
    string loggerName = loggerElement.EnsureNotNull().GetAttribute(NameAttr);

    LogLog.Debug(_declaringType, $"Retrieving an instance of log4net.Repository.Logger for logger [{loggerName}].");

    // Setting up a logger needs to be an atomic operation, in order
    // to protect potential log operations while logger
    // configuration is in progress.
    if (hierarchy.GetLogger(loggerName) is Logger log)
    {
      lock (log)
      {
        bool additivity = OptionConverter.ToBoolean(loggerElement.GetAttribute(AdditivityAttr), true);

        LogLog.Debug(_declaringType, $"Setting [{log.Name}] additivity to [{additivity}].");
        log.Additivity = additivity;
        ParseChildrenOfLoggerElement(loggerElement, log, false);
      }
    }
  }

  /// <summary>
  /// Parses the root logger element.
  /// </summary>
  /// <param name="rootElement">The root element.</param>
  /// <remarks>
  /// <para>
  /// Parse an XML element that represents the root logger.
  /// </para>
  /// </remarks>
  protected void ParseRoot(XmlElement rootElement)
  {
    Logger root = hierarchy.Root;
    // logger configuration needs to be atomic
    lock (root)
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
  /// <remarks>
  /// <para>
  /// Parse the child elements of a &lt;logger&gt; element.
  /// </para>
  /// </remarks>
  protected void ParseChildrenOfLoggerElement(XmlElement catElement, Logger log, bool isRoot)
  {
    // Remove all existing appenders from log. They will be
    // reconstructed if need be.
    log.EnsureNotNull().RemoveAllAppenders();

    foreach (XmlNode currentNode in catElement.EnsureNotNull().ChildNodes)
    {
      if (currentNode.NodeType == XmlNodeType.Element)
      {
        XmlElement currentElement = (XmlElement)currentNode;

        if (currentElement.LocalName == AppenderRefTag)
        {
          string refName = currentElement.GetAttribute(RefAttr);
          if (FindAppenderByReference(currentElement) is IAppender appender)
          {
            LogLog.Debug(_declaringType, $"Adding appender named [{refName}] to logger [{log.Name}].");
            log.AddAppender(appender);
          }
          else
          {
            LogLog.Error(_declaringType, $"Appender named [{refName}] not found.");
          }
        }
        else if (currentElement.LocalName is LevelTag or PriorityTag)
        {
          ParseLevel(currentElement, log, isRoot);
        }
        else
        {
          SetParameter(currentElement, log);
        }
      }
    }

    if (log is IOptionHandler optionHandler)
    {
      optionHandler.ActivateOptions();
    }
  }

  /// <summary>
  /// Parses an object renderer.
  /// </summary>
  /// <param name="element">The renderer element.</param>
  /// <remarks>
  /// <para>
  /// Parse an XML element that represents a renderer.
  /// </para>
  /// </remarks>
  protected void ParseRenderer(XmlElement element)
  {
    string renderingClassName = element.EnsureNotNull().GetAttribute(RenderingTypeAttr);
    string renderedClassName = element.GetAttribute(RenderedTypeAttr);

    LogLog.Debug(_declaringType, $"Rendering class [{renderingClassName}], Rendered class [{renderedClassName}].");
    if (OptionConverter.InstantiateByClassName(renderingClassName, typeof(IObjectRenderer), null) is not IObjectRenderer renderer)
    {
      LogLog.Error(_declaringType, $"Could not instantiate renderer [{renderingClassName}].");
      return;
    }

    try
    {
      hierarchy.RendererMap.Put(SystemInfo.GetTypeFromString(renderedClassName, true, true)!, renderer);
    }
    catch (Exception e) when (!e.IsFatal())
    {
      LogLog.Error(_declaringType, $"Could not find class [{renderedClassName}].", e);
    }
  }

  /// <summary>
  /// Parses a level element.
  /// </summary>
  /// <param name="element">The level element.</param>
  /// <param name="log">The logger object to set the level on.</param>
  /// <param name="isRoot">Flag to indicate if the logger is the root logger.</param>
  /// <remarks>
  /// <para>
  /// Parse an XML element that represents a level.
  /// </para>
  /// </remarks>
  protected static void ParseLevel(XmlElement element, Logger log, bool isRoot)
  {
    string loggerName = log.EnsureNotNull().Name;
    if (isRoot)
    {
      loggerName = "root";
    }

    string levelStr = element.EnsureNotNull().GetAttribute(ValueAttr);
    LogLog.Debug(_declaringType, $"Logger [{loggerName}] Level string is [{levelStr}].");

    if (Inherited == levelStr)
    {
      if (isRoot)
      {
        LogLog.Error(_declaringType, "Root level cannot be inherited. Ignoring directive.");
      }
      else
      {
        LogLog.Debug(_declaringType, $"Logger [{loggerName}] level set to inherit from parent.");
        log.Level = null;
      }
    }
    else
    {
      log.Level = log.Hierarchy?.LevelMap[levelStr];
      if (log.Level is null)
      {
        LogLog.Error(_declaringType, $"Undefined level [{levelStr}] on Logger [{loggerName}].");
      }
      else
      {
        LogLog.Debug(_declaringType, $"Logger [{loggerName}] level set to [name=\"{log.Level.Name}\",value={log.Level.Value}].");
      }
    }
  }

  /// <summary>
  /// Sets a parameter on an object.
  /// </summary>
  /// <param name="element">The parameter element.</param>
  /// <param name="target">The object to set the parameter on.</param>
  /// <remarks>
  /// The parameter name must correspond to a writable property
  /// on the object. The value of the parameter is a string,
  /// therefore this function will attempt to set a string
  /// property first. If unable to set a string property it
  /// will inspect the property and its argument type. It will
  /// attempt to call a static method called <c>Parse</c> on the
  /// type of the property. This method will take a single
  /// string argument and return a value that can be used to
  /// set the property.
  /// </remarks>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
  protected void SetParameter(XmlElement element, object target)
  {
    // Get the property name
    string name = element.EnsureNotNull().GetAttribute(NameAttr);

    // If the name attribute does not exist then use the name of the element
    if (element.LocalName != ParamTag || name.Length == 0)
    {
      name = element.LocalName;
    }

    // Look for the property on the target object
    Type targetType = target.EnsureNotNull().GetType();
    Type? propertyType = null;

    MethodInfo? methInfo = null;

    // Try to find a writable property
    PropertyInfo? propInfo = targetType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
    if (propInfo is not null && propInfo.CanWrite)
    {
      // found a property
      propertyType = propInfo.PropertyType;
    }
    else
    {
      propInfo = null;

      // look for a method with the signature Add<property>(type)
      methInfo = FindMethodInfo(targetType, name);
      if (methInfo is not null)
      {
        propertyType = methInfo.GetParameters()[0].ParameterType;
      }
    }

    if (propertyType is null)
    {
      LogLog.Error(_declaringType, $"XmlHierarchyConfigurator: Cannot find Property [{name}] to set object on [{target}]");
    }
    else
    {
      string? propertyValue = null;

      if (element.GetAttributeNode(ValueAttr) is not null)
      {
        propertyValue = element.GetAttribute(ValueAttr);
      }
      else if (element.HasChildNodes)
      {
        // Concatenate the CDATA and Text nodes together
        foreach (XmlNode childNode in element.ChildNodes)
        {
          if (childNode.NodeType is XmlNodeType.CDATA or XmlNodeType.Text)
          {
            if (propertyValue is null)
            {
              propertyValue = childNode.InnerText;
            }
            else
            {
              propertyValue += childNode.InnerText;
            }
          }
        }
      }

      if (propertyValue is not null)
      {
        try
        {
          // Expand environment variables in the string.
          IDictionary environmentVariables = Environment.GetEnvironmentVariables();
          if (HasCaseInsensitiveEnvironment)
          {
            environmentVariables = CreateCaseInsensitiveWrapper(environmentVariables);
          }
          propertyValue = OptionConverter.SubstituteVariables(propertyValue, environmentVariables);
        }
        catch (System.Security.SecurityException)
        {
          // This security exception will occur if the caller does not have 
          // unrestricted environment permission. If this occurs the expansion 
          // will be skipped with the following warning message.
          LogLog.Debug(_declaringType, "Security exception while trying to expand environment variables. Error Ignored. No Expansion.");
        }

        Type? parsedObjectConversionTargetType = null;

        // Check if a specific subtype is specified on the element using the 'type' attribute
        string subTypeString = element.GetAttribute(TypeAttr);
        if (subTypeString.Length > 0)
        {
          // Read the explicit subtype
          try
          {
            Type subType = SystemInfo.GetTypeFromString(subTypeString, true, true)!;

            LogLog.Debug(_declaringType, $"Parameter [{name}] specified subtype [{subType.FullName}]");

            if (!propertyType.IsAssignableFrom(subType))
            {
              // Check if there is an appropriate type converter
              if (OptionConverter.CanConvertTypeTo(subType, propertyType))
              {
                // Must re-convert to the real property type
                parsedObjectConversionTargetType = propertyType;

                // Use subtype as intermediary type
                propertyType = subType;
              }
              else
              {
                LogLog.Error(_declaringType, $"subtype [{subType.FullName}] set on [{name}] is not a subclass of property type [{propertyType.FullName}] and there are no acceptable type conversions.");
              }
            }
            else
            {
              // The subtype specified is found and is actually a subtype of the property
              // type, therefore we can switch to using this type.
              propertyType = subType;
            }
          }
          catch (Exception e) when (!e.IsFatal())
          {
            LogLog.Error(_declaringType, $"Failed to find type [{subTypeString}] set on [{name}]", e);
          }
        }

        // Now try to convert the string value to an acceptable type
        // to pass to this property.

        object? convertedValue = ConvertStringTo(propertyType, propertyValue);

        // Check if we need to do an additional conversion
        if (convertedValue is not null && parsedObjectConversionTargetType is not null)
        {
          LogLog.Debug(_declaringType, $"Performing additional conversion of value from [{convertedValue.GetType().Name}] to [{parsedObjectConversionTargetType.Name}]");
          convertedValue = OptionConverter.ConvertTypeTo(convertedValue, parsedObjectConversionTargetType);
        }

        if (convertedValue is not null)
        {
          if (propInfo is not null)
          {
            // Got a converted result
            LogLog.Debug(_declaringType, $"Setting Property [{propInfo.Name}] to {convertedValue.GetType().Name} value [{convertedValue}]");

            try
            {
              // Pass to the property
              propInfo.SetValue(target, convertedValue, BindingFlags.SetProperty, null, null, CultureInfo.InvariantCulture);
            }
            catch (TargetInvocationException targetInvocationEx)
            {
              LogLog.Error(_declaringType, $"Failed to set parameter [{propInfo.Name}] on object [{target}] using value [{convertedValue}]", targetInvocationEx.InnerException);
            }
          }
          else
          {
            // Got a converted result
            LogLog.Debug(_declaringType, $"Setting Collection Property [{methInfo!.Name}] to {convertedValue.GetType().Name} value [{convertedValue}]");

            try
            {
              // Pass to the property
              methInfo.Invoke(target, BindingFlags.InvokeMethod, null, [convertedValue], CultureInfo.InvariantCulture);
            }
            catch (TargetInvocationException targetInvocationEx)
            {
              LogLog.Error(_declaringType, $"Failed to set parameter [{name}] on object [{target}] using value [{convertedValue}]", targetInvocationEx.InnerException);
            }
          }
        }
        else
        {
          LogLog.Warn(_declaringType, $"Unable to set property [{name}] on object [{target}] using value [{propertyValue}] (with acceptable conversion types)");
        }
      }
      else
      {
        object? createdObject;

        if (propertyType == typeof(string) && !HasAttributesOrElements(element))
        {
          // If the property is a string and the element is empty (no attributes
          // or child elements) then we special case the object value to an empty string.
          // This is necessary because while the String is a class it does not have
          // a default constructor that creates an empty string, which is the behavior
          // we are trying to simulate and would be expected from CreateObjectFromXml
          createdObject = string.Empty;
        }
        else
        {
          // No value specified
          Type? defaultObjectType = null;
          if (IsTypeConstructible(propertyType))
          {
            defaultObjectType = propertyType;
          }

          createdObject = CreateObjectFromXml(element, defaultObjectType, propertyType);
        }

        if (createdObject is null)
        {
          LogLog.Error(_declaringType, $"Failed to create object to set param: {name}");
        }
        else
        {
          if (propInfo is not null)
          {
            // Got a converted result
            LogLog.Debug(_declaringType, $"Setting Property [{propInfo.Name}] to object [{createdObject}]");

            try
            {
              // Pass to the property
              propInfo.SetValue(target, createdObject, BindingFlags.SetProperty, null, null, CultureInfo.InvariantCulture);
            }
            catch (TargetInvocationException targetInvocationEx)
            {
              LogLog.Error(_declaringType, $"Failed to set parameter [{propInfo.Name}] on object [{target}] using value [{createdObject}]", targetInvocationEx.InnerException);
            }
          }
          else
          {
            // Got a converted result
            LogLog.Debug(_declaringType, $"Setting Collection Property [{methInfo!.Name}] to object [{createdObject}]");

            try
            {
              // Pass to the property
              methInfo.Invoke(target, BindingFlags.InvokeMethod, null, [createdObject], CultureInfo.InvariantCulture);
            }
            catch (TargetInvocationException targetInvocationEx)
            {
              LogLog.Error(_declaringType, $"Failed to set parameter [{methInfo.Name}] on object [{target}] using value [{createdObject}]", targetInvocationEx.InnerException);
            }
          }
        }
      }
    }
  }

  /// <summary>
  /// Test if an element has no attributes or child elements
  /// </summary>
  /// <param name="element">the element to inspect</param>
  /// <returns><see langword="true"/> if the element has any attributes or child elements, <see langword="false"/> otherwise</returns>
  private static bool HasAttributesOrElements(XmlElement element)
    => element.ChildNodes.OfType<XmlNode>().Any(node => node.NodeType is XmlNodeType.Attribute or XmlNodeType.Element);

  /// <summary>
  /// Test if a <see cref="Type"/> is constructible with <c>Activator.CreateInstance</c>.
  /// </summary>
  /// <param name="type">the type to inspect</param>
  /// <returns><see langword="true"/> if the type is creatable using a default constructor, <see langword="false"/> otherwise</returns>
  private static bool IsTypeConstructible(Type type)
  {
    return type.IsClass && !type.IsAbstract
      && type.GetConstructor(Type.EmptyTypes) is ConstructorInfo defaultConstructor
      && !defaultConstructor.IsAbstract && !defaultConstructor.IsPrivate;
  }

  /// <summary>
  /// Look for a method on the <paramref name="targetType"/> that matches the <paramref name="name"/> supplied
  /// </summary>
  /// <param name="targetType">the type that has the method</param>
  /// <param name="name">the name of the method</param>
  /// <returns>the method info found</returns>
  /// <remarks>
  /// <para>
  /// The method must be a public instance method on the <paramref name="targetType"/>.
  /// The method must be named <paramref name="name"/> or "Add" followed by <paramref name="name"/>.
  /// The method must take a single parameter.
  /// </para>
  /// </remarks>
  private static MethodInfo? FindMethodInfo(Type targetType, string name)
  {
    string requiredMethodNameA = name;
    string requiredMethodNameB = "Add" + name;

    MethodInfo[] methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    foreach (MethodInfo methInfo in methods)
    {
      if (!methInfo.IsStatic)
      {
        string methodInfoName = methInfo.Name;

        if (SystemInfo.EqualsIgnoringCase(methodInfoName, requiredMethodNameA) ||
            SystemInfo.EqualsIgnoringCase(methodInfoName, requiredMethodNameB))
        {
          // Found matching method name

          // Look for version with one arg only
          ParameterInfo[] methParams = methInfo.GetParameters();
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
  /// <para>
  /// An object of type <paramref name="type"/> with value <paramref name="value"/> or 
  /// <see langword="null"/> when the conversion could not be performed.
  /// </para>
  /// </returns>
  protected object? ConvertStringTo(Type type, string value)
  {
    // Hack to allow use of Level in property
    if (typeof(Level) == type)
    {
      // Property wants a level
      Level? levelValue = hierarchy.LevelMap[value];

      if (levelValue is null)
      {
        LogLog.Error(_declaringType, $"XmlHierarchyConfigurator: Unknown Level Specified [{value}]");
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
  /// <returns>The object or <see langword="null"/></returns>
  /// <remarks>
  /// <para>
  /// Parse an XML element and create an object instance based on the configuration
  /// data.
  /// </para>
  /// <para>
  /// The type of the instance may be specified in the XML. If not
  /// specified then the <paramref name="defaultTargetType"/> is used
  /// as the type. However the type is specified it must support the
  /// <paramref name="typeConstraint"/> type.
  /// </para>
  /// </remarks>
  protected object? CreateObjectFromXml(XmlElement element, Type? defaultTargetType, Type? typeConstraint)
  {
    Type? objectType;

    // Get the object type
    string objectTypeString = element.EnsureNotNull().GetAttribute(TypeAttr);
    if (objectTypeString.Length == 0)
    {
      if (defaultTargetType is null)
      {
        LogLog.Error(_declaringType, $"Object type not specified. Cannot create object of type [{typeConstraint?.FullName}]. Missing Value or Type.");
        return null;
      }

      // Use the default object type
      objectType = defaultTargetType;
    }
    else
    {
      // Read the explicit object type
      try
      {
        objectType = SystemInfo.GetTypeFromString(objectTypeString, true, true);
      }
      catch (Exception e) when (!e.IsFatal())
      {
        LogLog.Error(_declaringType, $"Failed to find type [{objectTypeString}]", e);
        return null;
      }
    }

    bool requiresConversion = false;

    // Got the object type. Check that it meets the typeConstraint
    if (typeConstraint is not null)
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
          LogLog.Error(_declaringType, $"Object type [{objectType?.FullName}] is not assignable to type [{typeConstraint.FullName}]. There are no acceptable type conversions.");
          return null;
        }
      }
    }

    // Create using the default constructor
    object? createdObject;
    try
    {
      createdObject = Activator.CreateInstance(objectType!).EnsureNotNull();
    }
    catch (Exception e) when (!e.IsFatal())
    {
      LogLog.Error(_declaringType, $"XmlHierarchyConfigurator: Failed to construct object of type [{objectType?.FullName}]", e);
      return null;
    }

    // Set any params on object
    foreach (XmlNode currentNode in element.ChildNodes)
    {
      if (currentNode.NodeType == XmlNodeType.Element)
      {
        SetParameter((XmlElement)currentNode, createdObject);
      }
    }

    // Check if we need to call ActivateOptions
    if (createdObject is IOptionHandler optionHandler)
    {
      optionHandler.ActivateOptions();
    }

    // Ok object should be initialized

    if (requiresConversion)
    {
      // Convert the object type
      return OptionConverter.ConvertTypeTo(createdObject, typeConstraint!);
    }

    // The object is of the correct type
    return createdObject;
  }

  private static bool HasCaseInsensitiveEnvironment
  {
    get
    {
      PlatformID platform = Environment.OSVersion.Platform;
      return platform is not PlatformID.Unix and not PlatformID.MacOSX;
    }
  }

  private static Hashtable CreateCaseInsensitiveWrapper(IDictionary dict)
  {
    Hashtable hash = SystemInfo.CreateCaseInsensitiveHashtable();
    foreach (DictionaryEntry entry in dict)
    {
      hash[entry.Key] = entry.Value;
    }
    return hash;
  }

  // String constants used while parsing the XML data
  private const string ConfigurationTag = "log4net";
  private const string RendererTag = "renderer";
  private const string AppenderTag = "appender";
  private const string AppenderRefTag = "appender-ref";
  private const string ParamTag = "param";

  // TODO: Deprecate use of category tags
  private const string CategoryTag = "category";
  // TODO: Deprecate use of priority tag
  private const string PriorityTag = "priority";

  private const string LoggerTag = "logger";
  private const string NameAttr = "name";
  private const string TypeAttr = "type";
  private const string ValueAttr = "value";
  private const string RootTag = "root";
  private const string LevelTag = "level";
  private const string RefAttr = "ref";
  private const string AdditivityAttr = "additivity";
  private const string ThresholdAttr = "threshold";
  private const string ConfigDebugAttr = "configDebug";
  private const string InternalDebugAttr = "debug";
  private const string EmitInternalDebugAttr = "emitDebug";
  private const string ConfigUpdateModeAttr = "update";
  private const string RenderingTypeAttr = "renderingClass";
  private const string RenderedTypeAttr = "renderedClass";

  // flag used on the level element
  private const string Inherited = "inherited";

  /// <summary>
  /// key: appenderName, value: appender.
  /// </summary>
  private readonly Dictionary<string, IAppender> _appenderBag = new(StringComparer.Ordinal);

  /// <summary>
  /// The fully qualified type of the XmlHierarchyConfigurator class.
  /// </summary>
  /// <remarks>
  /// Used by the internal logger to record the Type of the
  /// log message.
  /// </remarks>
  private static readonly Type _declaringType = typeof(XmlHierarchyConfigurator);
}