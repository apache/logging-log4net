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
using System.Text;
using System.IO;

using log4net.Util;

namespace log4net.ObjectRenderer
{
	/// <summary>
	/// The default Renderer renders objects by calling their <see cref="Object.ToString"/> method.
	/// </summary>
	/// <remarks>
	/// <para>The default renderer supports rendering objects to strings as follows:</para>
	/// 
	/// <list type="table">
	///		<listheader>
	///			<term>Value</term>
	///			<description>Rendered String</description>
	///		</listheader>
	///		<item>
	///			<term><c>null</c></term>
	///			<description><para>"(null)"</para></description>
	///		</item>
	///		<item>
	///			<term><see cref="Array"/></term>
	///			<description>
	///			<para>For a one dimensional array this is the
	///			array type name, an open brace, followed by a comma
	///			separated list of the elements (using the appropriate
	///			renderer), followed by a close brace. For example:
	///			<c>int[] {1, 2, 3}</c>.</para>
	///			<para>If the array is not one dimensional the 
	///			<c>Array.ToString()</c> is returned.</para>
	///			</description>
	///		</item>
	///		<item>
	///			<term><see cref="Exception"/></term>
	///			<description>
	///			<para>Renders the exception type, message
	///			and stack trace. Any nested exception is also rendered.</para>
	///			</description>
	///		</item>
	///		<item>
	///			<term>other</term>
	///			<description>
	///			<para><c>Object.ToString()</c></para>
	///			</description>
	///		</item>
	/// </list>
	/// 
	/// <para>The <see cref="DefaultRenderer"/> serves as a good base class 
	/// for renderers that need to provide special handling of exception
	/// types. The <see cref="RenderException"/> method is used to render
	/// the exception and its nested exceptions, however the <see cref="RenderExceptionMessage"/>
	/// method is called just to render the exceptions message. This method
	/// can be overridden is a subclass to provide additional information
	/// for some exception types. See <see cref="RenderException"/> for
	/// more information.</para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class DefaultRenderer : IObjectRenderer
	{
		private static readonly string NewLine = SystemInfo.NewLine;

		#region Constructors

		/// <summary>
		/// Default constructor
		/// </summary>
		/// <remarks>
		/// Default constructor
		/// </remarks>
		public DefaultRenderer()
		{
		}

		#endregion

		#region Implementation of IObjectRenderer

		/// <summary>
		/// Render the object <paramref name="obj"/> to a string
		/// </summary>
		/// <param name="rendererMap">The map used to lookup renderers</param>
		/// <param name="obj">The object to render</param>
		/// <param name="writer">The writer to render to</param>
		/// <remarks>
		/// <para>Render the object <paramref name="obj"/> to a 
		/// string.</para>
		/// 
		/// <para>The <paramref name="rendererMap"/> parameter is
		/// provided to lookup and render other objects. This is
		/// very useful where <paramref name="obj"/> contains
		/// nested objects of unknown type. The <see cref="RendererMap.FindAndRender"/>
		/// method can be used to render these objects.</para>
		/// 
		/// <para>The default renderer supports rendering objects to strings as follows:</para>
		/// 
		/// <list type="table">
		///		<listheader>
		///			<term>Value</term>
		///			<description>Rendered String</description>
		///		</listheader>
		///		<item>
		///			<term><c>null</c></term>
		///			<description>
		///			<para>"(null)"</para>
		///			</description>
		///		</item>
		///		<item>
		///			<term><see cref="Array"/></term>
		///			<description>
		///			<para>For a one dimensional array this is the
		///			array type name, an open brace, followed by a comma
		///			separated list of the elements (using the appropriate
		///			renderer), followed by a close brace. For example:
		///			<c>int[] {1, 2, 3}</c>.</para>
		///			<para>If the array is not one dimensional the 
		///			<c>Array.ToString()</c> is returned.</para>
		///			
		///			<para>The <see cref="RenderArray"/> method is called
		///			to do the actual array rendering. This method can be
		///			overridden in a subclass to provide different array
		///			rendering.</para>
		///			</description>
		///		</item>
		///		<item>
		///			<term><see cref="Exception"/></term>
		///			<description>
		///			<para>Renders the exception type, message
		///			and stack trace. Any nested exception is also rendered.</para>
		///			
		///			<para>The <see cref="RenderException"/> method is called
		///			to do the actual exception rendering. This method can be
		///			overridden in a subclass to provide different exception
		///			rendering.</para>
		///			</description>
		///		</item>
		///		<item>
		///			<term>other</term>
		///			<description>
		///			<para><c>Object.ToString()</c></para>
		///			</description>
		///		</item>
		/// </list>
		/// </remarks>
		virtual public void RenderObject(RendererMap rendererMap, object obj, TextWriter writer)
		{
			if (rendererMap == null)
			{
				throw new ArgumentNullException("rendererMap");
			}

			if (obj == null)
			{
				writer.Write("(null)");
			}
			else if (obj is Array)
			{
				RenderArray(rendererMap, (Array)obj, writer);
			}
			else if (obj is Exception)
			{
				RenderException(rendererMap, (Exception)obj, writer);
			}
			else
			{
				string str = obj.ToString();
				writer.Write( (str==null) ? "(null)" : str );
			}
		}

		#endregion

		/// <summary>
		/// Render the array argument into a string
		/// </summary>
		/// <param name="rendererMap">The map used to lookup renderers</param>
		/// <param name="array">the array to render</param>
		/// <param name="writer">The writer to render to</param>
		/// <remarks>
		/// <para>For a one dimensional array this is the
		///	array type name, an open brace, followed by a comma
		///	separated list of the elements (using the appropriate
		///	renderer), followed by a close brace. For example:
		///	<c>int[] {1, 2, 3}</c>.</para>
		///	<para>If the array is not one dimensional the 
		///	<c>Array.ToString()</c> is returned.</para>
		/// </remarks>
		virtual protected void RenderArray(RendererMap rendererMap, Array array, TextWriter writer)
		{
			if (array.Rank != 1)
			{
				writer.Write(array.ToString());
			}
			else
			{
				writer.Write(array.GetType().Name + " {");
				int len = array.Length;

				if (len > 0)
				{
					rendererMap.FindAndRender(array.GetValue(0), writer);
					for(int i=1; i<len; i++)
					{
						writer.Write(", ");
						rendererMap.FindAndRender(array.GetValue(i), writer);
					}
				}
				writer.Write("}");
			}
		}

		/// <summary>
		/// Render the exception into a string
		/// </summary>
		/// <param name="rendererMap">The map used to lookup renderers</param>
		/// <param name="ex">the exception to render</param>
		/// <param name="writer">The writer to render to</param>
		/// <remarks>
		/// <para>Renders the exception type, message, and stack trace. Any nested
		/// exceptions are also rendered.</para>
		/// 
		/// <para>The <see cref="RenderExceptionMessage(RendererMap,Exception,TextWriter)"/>
		/// method is called to render the Exception's message into a string. This method
		/// can be overridden to change the behavior when rendering
		/// exceptions. To change or extend only the message that is
		/// displayed override the <see cref="RenderExceptionMessage(RendererMap,Exception,TextWriter)"/>
		/// method instead.</para>
		/// </remarks>
		virtual protected void RenderException(RendererMap rendererMap, Exception ex, TextWriter writer)
		{
			writer.Write("Exception: ");
			writer.WriteLine(ex.GetType().FullName);
			writer.Write("Message: ");
			RenderExceptionMessage(rendererMap, ex, writer);
			writer.WriteLine();

#if !NETCF
			try
			{
				if (ex.Source != null && ex.Source.Length > 0)
				{
					writer.Write("Source: ");
					writer.WriteLine(ex.Source);
				}
			}
			catch
			{
				writer.WriteLine("Source: (Exception Occurred)");
			}

			if (ex.StackTrace != null && ex.StackTrace.Length > 0)
			{
				writer.WriteLine(ex.StackTrace);
			}
#endif
			if (ex.InnerException != null)
			{
				writer.WriteLine();
				writer.WriteLine("Nested Exception");
				writer.WriteLine();
				RenderException(rendererMap, ex.InnerException, writer);
				writer.WriteLine();
			}
		}

		/// <summary>
		/// Render the exception message into a string
		/// </summary>
		/// <param name="rendererMap">The map used to lookup renderers</param>
		/// <param name="ex">the exception to get the message from and render</param>
		/// <param name="writer">The writer to render to</param>
		/// <remarks>
		/// <para>This method is called to render the exception's message into
		/// a string. This method should be overridden to extend the information
		/// that is rendered for a specific exception.</para>
		/// 
		/// <para>See <see cref="RenderException"/> for more information.</para>
		/// </remarks>
		virtual protected void RenderExceptionMessage(RendererMap rendererMap, Exception ex, TextWriter writer)
		{
			writer.Write(ex.Message);
		}

	}
}
