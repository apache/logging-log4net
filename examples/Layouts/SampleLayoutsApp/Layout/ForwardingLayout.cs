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
using log4net.Layout;
using log4net.Core;
using log4net.Util;

namespace SampleLayoutsApp.Layout
{
	/// <summary>
	/// The ForwardingLayout forwards to a nested <see cref="ILayout"/>
	/// </summary>
	/// <remarks>
	/// The ForwardingLayout base class is used by layouts that want
	/// to decorate other <see cref="ILayout"/> objects.
	/// </remarks>
	public abstract class ForwardingLayout : ILayout, IOptionHandler
	{
		private ILayout m_nestedLayout;

		protected ForwardingLayout()
		{
		}

		public ILayout Layout
		{
			get { return m_nestedLayout; }
			set { m_nestedLayout = value; }
		}

		#region Implementation of IOptionHandler

		/// <summary>
		/// Activate component options
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
		/// <para>
		/// This method must be implemented by the subclass.
		/// </para>
		/// </remarks>
		public virtual void ActivateOptions()
		{
			IOptionHandler optionHandler = m_nestedLayout as IOptionHandler;
			if (optionHandler != null)
			{
				optionHandler.ActivateOptions();
			}
		}

		#endregion

		#region Implementation of ILayout

		/// <summary>
		/// Implement this method to create your own layout format.
		/// </summary>
		/// <param name="writer">The TextWriter to write the formatted event to</param>
		/// <param name="loggingEvent">The event to format</param>
		/// <remarks>
		/// <para>
		/// This method is called by an appender to format
		/// the <paramref name="loggingEvent"/> as text.
		/// </para>
		/// </remarks>
		virtual public void Format(TextWriter writer, LoggingEvent loggingEvent)
		{
			if (m_nestedLayout != null)
			{
				m_nestedLayout.Format(writer, loggingEvent);
			}
		}

		/// <summary>
		/// The content type output by this layout. 
		/// </summary>
		/// <value>The content type is <c>"text/plain"</c></value>
		/// <remarks>
		/// <para>
		/// The content type output by this layout.
		/// </para>
		/// <para>
		/// This base class uses the value <c>"text/plain"</c>.
		/// To change this value a subclass must override this
		/// property.
		/// </para>
		/// </remarks>
		virtual public string ContentType
		{
			get 
			{
				if (m_nestedLayout != null)
				{
					return m_nestedLayout.ContentType;
				}
				return "text/plain"; 
			}
		}

		/// <summary>
		/// The header for the layout format.
		/// </summary>
		/// <value>the layout header</value>
		/// <remarks>
		/// <para>
		/// The Header text will be appended before any logging events
		/// are formatted and appended.
		/// </para>
		/// </remarks>
		virtual public string Header
		{
			get 
			{ 
				if (m_nestedLayout != null)
				{
					return m_nestedLayout.Header;
				}
				return null;
			}
		}

		/// <summary>
		/// The footer for the layout format.
		/// </summary>
		/// <value>the layout footer</value>
		/// <remarks>
		/// <para>
		/// The Footer text will be appended after all the logging events
		/// have been formatted and appended.
		/// </para>
		/// </remarks>
		virtual public string Footer
		{
			get 
			{ 
				if (m_nestedLayout != null)
				{
					return m_nestedLayout.Footer;
				}
				return null;
			}
		}

		/// <summary>
		/// Flag indicating if this layout handles exceptions
		/// </summary>
		/// <value><c>false</c> if this layout handles exceptions</value>
		/// <remarks>
		/// <para>
		/// If this layout handles the exception object contained within
		/// <see cref="LoggingEvent"/>, then the layout should return
		/// <c>false</c>. Otherwise, if the layout ignores the exception
		/// object, then the layout should return <c>true</c>.
		/// </para>
		/// <para>
		/// Set this value to override a this default setting. The default
		/// value is <c>true</c>, this layout does not handle the exception.
		/// </para>
		/// </remarks>
		virtual public bool IgnoresException 
		{ 
			get 
			{ 
				if (m_nestedLayout != null)
				{
					return m_nestedLayout.IgnoresException;
				}
				return true;
			}
		}

		#endregion
	}
}
