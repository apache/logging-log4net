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
#if !NETCF
using System.Collections;
#endif

namespace log4net
{
	/// <summary>
	/// Implementation of Nested Diagnostic Contexts.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A Nested Diagnostic Context, or NDC in short, is an instrument
	/// to distinguish interleaved log output from different sources. Log
	/// output is typically interleaved when a server handles multiple
	/// clients near-simultaneously.
	/// </para>
	/// <para>
	/// Interleaved log output can still be meaningful if each log entry
	/// from different contexts had a distinctive stamp. This is where NDCs
	/// come into play.
	/// </para>
	/// <para>
	/// Note that NDCs are managed on a per thread basis. The NDC class
	/// is made up of static methods that operate on the context of the
	/// calling thread.
	/// </para>
	/// </remarks>
	/// <example>How to push a message into the context
	/// <code>
	///	using(NDC.Push("my context message"))
	///	{
	///		... all log calls will have 'my context message' included ...
	///	
	///	} // at the end of the using block the message is automatically removed 
	/// </code>
	/// </example>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public sealed class NDC
	{
		#region Private Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="NDC" /> class. 
		/// </summary>
		/// <remarks>
		/// Uses a private access modifier to prevent instantiation of this class.
		/// </remarks>
		private NDC()
		{
		}

		#endregion Private Instance Constructors

		#region Public Static Properties

		/// <summary>
		/// Gets the current context depth.
		/// </summary>
		/// <value>The current context depth.</value>
		public static int Depth
		{
			get { return GetStack().Count; }
		}

		#endregion Public Static Properties

		#region Public Static Methods

		/// <summary>
		/// Clears all the contextual information held on the 
		/// current thread.
		/// </summary>
		/// <remarks>
		/// After calling this method the <see cref="Depth"/> will be <c>0</c>.
		/// </remarks>
		public static void Clear() 
		{
			GetStack().Clear();
		}

		/// <summary>
		/// Creates a clone of the stack of context information.
		/// </summary>
		/// <remarks>
		/// The results of this method can be passed to the <see cref="Inherit"/> 
		/// method to allow child threads to inherit the context of their 
		/// parent thread.
		/// </remarks>
		/// <returns>A clone of the context info for this thread.</returns>
		public static Stack CloneStack() 
		{
			return (Stack)GetStack().Clone();
		}

		/// <summary>
		/// Inherits the contextual information from another thread.
		/// </summary>
		/// <remarks>
		/// This thread will use the context information from the stack
		/// supplied. This can be used to initialise child threads with
		/// the same contextual information as their parent threads. These
		/// contexts will <b>NOT</b> be shared. Any further contexts that
		/// are pushed onto the stack will not be visible to the other.
		/// Call <see cref="CloneStack"/> to obtain a stack to pass to
		/// this method.
		/// </remarks>
		/// <param name="stack">The context stack to inherit.</param>
		public static void Inherit(Stack stack) 
		{
			if (stack == null)
			{
				throw new ArgumentNullException("stack");
			}

			System.Threading.Thread.SetData(s_slot, stack);
		}

		/// <summary>
		/// Removes the top context from the stack.
		/// </summary>
		/// <remarks>
		/// Remove the top context from the stack, and return
		/// it to the caller. If the stack is empty then an
		/// empty string (not <c>null</c>) is returned.
		/// </remarks>
		/// <returns>
		/// The message in the context that was removed from the top 
		/// of the stack.
		/// </returns>
		public static string Pop() 
		{
			Stack stack = GetStack();
			if (stack.Count > 0)
			{
				return ((DiagnosticContext)(stack.Pop())).Message;
			}
			return "";
		}

		/// <summary>
		/// Pushes a new context message.
		/// </summary>
		/// <param name="message">The new context message.</param>
		/// <returns>
		/// An <see cref="IDisposable"/> that can be used to clean up 
		/// the context stack.
		/// </returns>
		/// <remarks>
		/// Pushes a new context onto the context stack. An <see cref="IDisposable"/>
		/// is returned that can be used to clean up the context stack. This
		/// can be easily combined with the <c>using</c> keyword to scope the
		/// context.
		/// </remarks>
		/// <example>Simple example of using the <c>Push</c> method with the <c>using</c> keyword.
		/// <code>
		/// using(log4net.NDC.Push("NDC_Message"))
		/// {
		///		log.Warn("This should have an NDC message");
		///	}
		/// </code>
		/// </example>
		public static IDisposable Push(string message) 
		{
			Stack stack = GetStack();
			stack.Push(new DiagnosticContext(message, (stack.Count>0) ? (DiagnosticContext)stack.Peek() : null));

			return new NDCAutoDisposeFrame(stack, stack.Count - 1);
		}

		/// <summary>
		/// Removes the context information for this thread. It is
		/// not required to call this method.
		/// </summary>
		/// <remarks>
		/// This method is not implemented.
		/// </remarks>
		public static void Remove() 
		{
		}

		/// <summary>
		/// Forces the stack depth to be at most <paramref name="maxDepth"/>.
		/// </summary>
		/// <remarks>
		/// Forces the stack depth to be at most <paramref name="maxDepth"/>.
		/// This may truncate the head of the stack. This only affects the 
		/// stack in the current thread. Also it does not prevent it from
		/// growing, it only sets the maximum depth at the time of the
		/// call. This can be used to return to a known context depth.
		/// </remarks>
		/// <param name="maxDepth">The maximum depth of the stack</param>
		static public void SetMaxDepth(int maxDepth) 
		{
			if (maxDepth < 0)
			{
				throw new ArgumentOutOfRangeException("Parameter: maxDepth, Value: ["+maxDepth+"] out of range. Nonnegative number required");
			}

			Stack stack = GetStack();
			while(stack.Count > maxDepth)
			{
				stack.Pop();
			}
		}

		#endregion Public Static Methods

		#region Internal Static Methods

		/// <summary>
		/// Gets the current context information.
		/// </summary>
		/// <returns>The current context information.</returns>
		internal static string Get() 
		{
			Stack stack = GetStack();
			if (stack.Count > 0)
			{
				return ((DiagnosticContext)(stack.Peek())).FullMessage;
			}
			return null;
		}
  
		/// <summary>
		/// Peeks at the message on the top of the context stack.
		/// </summary>
		/// <returns>The message on the top of the stack.</returns>
		internal static string Peek() 
		{
			Stack stack = GetStack();
			if (stack.Count > 0)
			{
				return ((DiagnosticContext)(stack.Peek())).Message;
			}
			return "";
		}
 
		#endregion Internal Static Methods

		#region Private Static Methods

		/// <summary>
		/// Gets the stack of context objects on this thread.
		/// </summary>
		/// <returns>The stack of context objects on the current thread.</returns>
		static private Stack GetStack()
		{
			Stack stack = (Stack)System.Threading.Thread.GetData(s_slot);
			if (stack == null)
			{
				stack = new Stack();
				System.Threading.Thread.SetData(s_slot, stack);
			}
			return stack;
		}

		#endregion Private Static Methods

		#region Private Static Fields

		/// <summary>
		/// The thread local data slot to use for context information.
		/// </summary>
		private readonly static LocalDataStoreSlot s_slot = System.Threading.Thread.AllocateDataSlot();

		#endregion Private Static Fields

		/// <summary>
		/// Inner class used to represent a single context in the stack.
		/// </summary>
		internal class DiagnosticContext 
		{
			#region Internal Instance Constructors

			/// <summary>
			/// Initializes a new instance of the <see cref="DiagnosticContext" /> class
			/// with the specified message and parent context.
			/// </summary>
			/// <param name="message">The message for this context.</param>
			/// <param name="parent">The parent context in the chain.</param>
			internal DiagnosticContext(string message, DiagnosticContext parent) 
			{
				m_message = message;
				if (parent != null) 
				{
					m_fullMessage = parent.FullMessage + ' ' + message;
				} 
				else 
				{
					m_fullMessage = message;
				}
			}

			#endregion Internal Instance Constructors

			#region Internal Instance Properties

			/// <summary>
			/// Get the message.
			/// </summary>
			/// <value>The message.</value>
			internal string Message
			{
				get { return m_message; }
			}

			/// <summary>
			/// Gets the full text of the context down to the root level.
			/// </summary>
			/// <value>
			/// The full text of the context down to the root level.
			/// </value>
			internal string FullMessage
			{
				get { return m_fullMessage; }
			}

			#endregion Internal Instance Properties

			#region Private Instance Fields

			private string m_fullMessage;
			private string m_message;
    
			#endregion
		}

		/// <summary>
		/// Inner class that is returned from <see cref="NDC.Push"/>
		/// </summary>
		/// <remarks>
 		/// This class is disposable and when it is disposed it automatically
 		/// returns the NDC to the correct depth.
		/// </remarks>
		internal class NDCAutoDisposeFrame : IDisposable
		{
			#region Internal Instance Constructors

			/// <summary>
			/// Initializes a new instance of the <see cref="NDCAutoDisposeFrame" /> class with
			/// the specified stack and return depth.
			/// </summary>
			/// <param name="frameStack">The internal stack used by the NDC.</param>
			/// <param name="frameDepth">The depth to return the stack to when this object is disposed.</param>
			internal NDCAutoDisposeFrame(Stack frameStack, int frameDepth)
			{
				m_frameStack = frameStack;
				m_frameDepth = frameDepth;
			}

			#endregion Internal Instance Constructors

			#region Implementation of IDisposable

			/// <summary>
			/// Returns the NDC stack to the correct depth.
			/// </summary>
			public void Dispose()
			{
				if (m_frameDepth >= 0 && m_frameStack != null)
				{
					while(m_frameStack.Count > m_frameDepth)
					{
						m_frameStack.Pop();
					}
				}
			}

			#endregion Implementation of IDisposable

			#region Private Instance Fields

			/// <summary>
			/// The NDC internal stack
			/// </summary>
			private Stack m_frameStack;

			/// <summary>
			/// The depth to rethrow the stack to when this instance is disposed
			/// </summary>
			private int m_frameDepth;

			#endregion Private Instance Fields
		}

#if NETCF
		/// <summary>
		/// Subclass of <see cref="System.Collections.Stack"/> to
		/// provide missing methods.
		/// </summary>
		/// <remarks>
		/// The Compact Framework version of the <see cref="System.Collections.Stack"/>
		/// class is missing the <c>Clear</c> and <c>Clone</c> methods.
		/// This subclass adds implementations of those missing methods.
		/// </remarks>
		public class Stack : System.Collections.Stack
		{
			/// <summary>
			/// Clears the stack of all elements.
			/// </summary>
			public void Clear()
			{
				while(Count > 0)
				{
					Pop();
				}
			}

			/// <summary>
			/// Makes a shallow copy of the stack's elements.
			/// </summary>
			/// <returns>A new stack that has a shallow copy of the stack's elements.</returns>
			public Stack Clone()
			{
				Stack res = new Stack();
				object[] items = ToArray();
				foreach(object item in items)
				{
					res.Push(item);
				}
				return res;
			}
		}
#endif
	}
}
