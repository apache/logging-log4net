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
using System.IO;

namespace log4net.ObjectRenderer
{
	/// <summary>
	/// Map class objects to an <see cref="IObjectRenderer"/>.
	/// </summary>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class RendererMap
	{
		#region Member Variables

		private System.Collections.Hashtable m_map;
		private static IObjectRenderer s_defaultRenderer = new DefaultRenderer();

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		public RendererMap() 
		{
			m_map = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable());
		}

		#endregion

		/// <summary>
		/// Render <paramref name="obj"/> using the appropriate renderer.
		/// </summary>
		/// <remarks>
		/// <para>Find the appropriate renderer for the type of the
		/// <paramref name="obj"/> parameter. This is accomplished by calling the
		/// <see cref="Get(Type)"/> method. Once a renderer is found, it is
		/// applied on the object <paramref name="obj"/> and the result is returned
		/// as a <see cref="string"/>.</para>
		/// </remarks>
		/// <param name="obj">the object to render to a string</param>
		/// <param name="writer">The writer to render to</param>
		public void FindAndRender(object obj, TextWriter writer) 
		{
			if (obj == null)
			{
				writer.Write("(null)");
			}
			else 
			{
				try
				{
					Get(obj.GetType()).RenderObject(this, obj, writer);
				}
				catch(Exception ex)
				{
					// Exception rendering the object
					log4net.Util.LogLog.Error("RendererMap: Exception while rendering object of type ["+obj.GetType().FullName+"]", ex);

					// return default message
					string objectTypeName = "";
					if (obj != null && obj.GetType() != null)
					{
						objectTypeName = obj.GetType().FullName;
					}

					writer.Write("<log4net.Error>Exception rendering object type ["+objectTypeName+"]");
					if (ex != null)
					{
						string exceptionText = null;

						try
						{
							exceptionText = ex.ToString();
						}
						catch
						{
							// Ignore exception
						}

						writer.Write("<stackTrace>" + exceptionText + "</stackTrace>");
					}
					writer.Write("</log4net.Error>");
				}
			}
		}

		/// <summary>
		/// Gets the renderer for the specified object type
		/// </summary>
		/// <remarks>
		/// <param>Gets the renderer for the specified object type</param>
		/// 
		/// <param>Syntactic sugar method that calls <see cref="Get(Type)"/> 
		/// with the type of the object parameter.</param>
		/// </remarks>
		/// <param name="obj">the object to lookup the renderer for</param>
		/// <returns>the renderer for <paramref name="obj"/></returns>
		public IObjectRenderer Get(Object obj) 
		{
			if (obj == null) 
			{
				return null;
			}
			else
			{
				return Get(obj.GetType());
			}
		}
  
		/// <summary>
		/// Gets the renderer for the specified type
		/// </summary>
		/// <param name="type">the type to lookup the renderer for</param>
		/// <returns>the renderer for the specified type</returns>
		public IObjectRenderer Get(Type type) 
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}

			IObjectRenderer result = null;

			for(Type cur = type; cur != null; cur = cur.BaseType)
			{
				// Look for the specific type in the map
				result = (IObjectRenderer)m_map[cur];
				if (result != null) 
				{
					break;
				}

				// Search the type's interfaces
				result = SearchInterfaces(cur);
				if (result != null)
				{
					break;
				}
			}

			// if not set then use the default renderer
			if (result == null)
			{
				result = s_defaultRenderer;
			}

			return result;
		}  

		/// <summary>
		/// Internal function to recursively search interfaces
		/// </summary>
		/// <param name="type">the type to lookup the renderer for</param>
		/// <returns>the renderer for the specified type</returns>
		private IObjectRenderer SearchInterfaces(Type type) 
		{
			IObjectRenderer r = (IObjectRenderer)m_map[type];
			if (r != null) 
			{
				return r;
			} 
			else 
			{
				foreach(Type t in type.GetInterfaces())
				{
					r = SearchInterfaces(t);
					if (r != null)
					{
						return r; 
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Get the default renderer instance
		/// </summary>
		/// <returns>the default renderer</returns>
		public IObjectRenderer DefaultRenderer
		{
			get { return s_defaultRenderer; }
		}

		/// <summary>
		/// Clear the map of renderers
		/// </summary>
		public void Clear() 
		{
			m_map.Clear();
		}

		/// <summary>
		/// Register an <see cref="IObjectRenderer"/> for <paramref name="typeToRender"/>. 
		/// </summary>
		/// <param name="typeToRender">the type that will be rendered by <paramref name="renderer"/></param>
		/// <param name="renderer">the renderer for <paramref name="typeToRender"/></param>
		public void Put(Type typeToRender, IObjectRenderer renderer) 
		{
			if (typeToRender == null)
			{
				throw new ArgumentNullException("typeToRender");
			}
			if (renderer == null)
			{
				throw new ArgumentNullException("renderer");
			}

			m_map[typeToRender] = renderer;
		}	
	}
}
