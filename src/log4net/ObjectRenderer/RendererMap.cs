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
using System.Collections.Concurrent;
using log4net.Util;

namespace log4net.ObjectRenderer;

/// <summary>
/// Maps types to <see cref="IObjectRenderer"/> instances for types that require custom
/// rendering.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="FindAndRender(object)"/> method is used to render an
/// <c>object</c> using the appropriate renderers defined in this map,
/// using a default renderer if no custom renderer is defined for a type.
/// </para>
/// </remarks>
/// <author>Nicko Cadell</author>
/// <author>Gert Driesen</author>
public class RendererMap
{
  private static readonly Type _declaringType = typeof(RendererMap);

  private readonly ConcurrentDictionary<Type, IObjectRenderer> _map = [];
  private readonly ConcurrentDictionary<Type, IObjectRenderer> _cache = [];

  /// <summary>
  /// Renders <paramref name="obj"/> using the appropriate renderer.
  /// </summary>
  /// <param name="obj">the object to render to a string</param>
  /// <returns>The object rendered as a string.</returns>
  /// <remarks>
  /// <para>
  /// This is a convenience method used to render an object to a string.
  /// The alternative method <see cref="FindAndRender(object,TextWriter)"/>
  /// should be used when streaming output to a <see cref="TextWriter"/>.
  /// </para>
  /// </remarks>
  public string FindAndRender(object? obj)
  {
    // Optimisation for strings
    if (obj is string strData)
    {
      return strData;
    }

    using StringWriter stringWriter = new(System.Globalization.CultureInfo.InvariantCulture);
    FindAndRender(obj, stringWriter);
    return stringWriter.ToString();
  }

  /// <summary>
  /// Render <paramref name="obj"/> using the appropriate renderer.
  /// </summary>
  /// <param name="obj">the object to render to a string</param>
  /// <param name="writer">The writer to render to</param>
  /// <remarks>
  /// <para>
  /// Find the appropriate renderer for the type of the
  /// <paramref name="obj"/> parameter. This is accomplished by calling the
  /// <see cref="Get(Type)"/> method. Once a renderer is found, it is
  /// applied on the object <paramref name="obj"/> and the result is returned
  /// as a <see cref="string"/>.
  /// </para>
  /// </remarks>
  public void FindAndRender(object? obj, TextWriter writer)
  {
    writer.EnsureNotNull();
    if (obj is null)
    {
      writer.Write(SystemInfo.NullText);
    }
    else
    {
      // Optimisation for strings
      if (obj is string str)
      {
        writer.Write(str);
      }
      else
      {
        // Lookup the renderer for the specific type
        try
        {
          Get(obj.GetType()).RenderObject(this, obj, writer);
        }
        catch (Exception e) when (!e.IsFatal())
        {
          // Exception rendering the object
          LogLog.Error(_declaringType, $"Exception while rendering object of type [{obj.GetType().FullName}]", e);

          // return default message
          string objectTypeName = obj.GetType().FullName ?? string.Empty;

          writer.Write($"<log4net.Error>Exception rendering object type [{objectTypeName}]");

          string? exceptionText = null;
          try
          {
            exceptionText = e.ToString();
          }
          catch (Exception inner) when (!inner.IsFatal())
          {
            // Ignore exception
          }

          writer.Write($"<stackTrace>{exceptionText}</stackTrace>");
          writer.Write("</log4net.Error>");
        }
      }
    }
  }

  /// <summary>
  /// Gets the renderer for the specified object type.
  /// </summary>
  /// <param name="obj">The object for which to look up the renderer.</param>
  /// <returns>the renderer for <paramref name="obj"/></returns>
  /// <remarks>
  /// <param>
  /// Gets the renderer for the specified object type.
  /// </param>
  /// <param>
  /// Syntactic sugar method that calls <see cref="Get(Type)"/> 
  /// with the type of the object parameter.
  /// </param>
  /// </remarks>
  public IObjectRenderer? Get(object? obj)
  {
    if (obj is null)
    {
      return null;
    }

    return Get(obj.GetType());
  }

  /// <summary>
  /// Gets the renderer for the specified type
  /// </summary>
  /// <param name="type">the type to look up the renderer for</param>
  /// <returns>The renderer for the specified type, or <see cref="DefaultRenderer"/> if no specific renderer has been defined.</returns>
  public IObjectRenderer Get(Type type)
  {
    // Check cache
    if (!_cache.TryGetValue(type.EnsureNotNull(), out IObjectRenderer? result))
    {
      for (Type? cur = type; cur is not null; cur = cur.BaseType)
      {
        // Search the type's interfaces
        result = SearchTypeAndInterfaces(cur);
        if (result is not null)
        {
          break;
        }
      }

      // if not set then use the default renderer
      result ??= DefaultRenderer;

      // Add to cache
      _cache.TryAdd(type, result);
    }

    return result;
  }

  /// <summary>
  /// Recursively searches interfaces.
  /// </summary>
  /// <param name="type">The type for which to look up the renderer.</param>
  /// <returns>The renderer for the specified type, or <c>null</c> if not found.</returns>
  private IObjectRenderer? SearchTypeAndInterfaces(Type type)
  {
    if (_map.TryGetValue(type, out IObjectRenderer? r))
    {
      return r;
    }

    foreach (Type t in type.GetInterfaces())
    {
      r = SearchTypeAndInterfaces(t);
      if (r is not null)
      {
        return r;
      }
    }
    return null;
  }

  /// <summary>
  /// Gets the default renderer instance
  /// </summary>
  public static IObjectRenderer DefaultRenderer { get; } = new DefaultRenderer();

  /// <summary>
  /// Clears the map of custom renderers. The <see cref="DefaultRenderer"/>
  /// is not removed.
  /// </summary>
  public void Clear()
  {
    _map.Clear();
    _cache.Clear();
  }

  /// <summary>
  /// Registers an <see cref="IObjectRenderer"/> for <paramref name="typeToRender"/>. 
  /// </summary>
  /// <param name="typeToRender">The type that will be rendered by <paramref name="renderer"/>.</param>
  /// <param name="renderer">The renderer for <paramref name="typeToRender"/>.</param>
  public void Put(Type typeToRender, IObjectRenderer renderer)
  {
    _cache.Clear();
    _map[typeToRender.EnsureNotNull()] = renderer.EnsureNotNull();
  }
}
