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

using log4net.Core;

namespace log4net.Util
{
  /// <summary>
  /// Implementation of Stack for the <see cref="log4net.ThreadContext"/>
  /// </summary>
  /// <remarks>
  /// <para>
  /// Implementation of Stack for the <see cref="log4net.ThreadContext"/>
  /// </para>
  /// </remarks>
  /// <author>Nicko Cadell</author>
  public sealed class ThreadContextStack : IFixingRequired
  {
    /// <summary>
    /// The stack store.
    /// </summary>
    private readonly Stack<StackFrame> m_stack = new();

    /// <summary>
    /// Internal constructor
    /// </summary>
    /// <remarks>
    /// <para>
    /// Initializes a new instance of the <see cref="ThreadContextStack" /> class. 
    /// </para>
    /// </remarks>
    internal ThreadContextStack()
    {
    }

    /// <summary>
    /// The number of messages in the stack
    /// </summary>
    /// <value>
    /// The current number of messages in the stack
    /// </value>
    /// <remarks>
    /// <para>
    /// The current number of messages in the stack. That is
    /// the number of times <see cref="Push"/> has been called
    /// minus the number of times <see cref="Pop"/> has been called.
    /// </para>
    /// </remarks>
    public int Count => m_stack.Count;

    /// <summary>
    /// Clears all the contextual information held in this stack.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Clears all the contextual information held in this stack.
    /// Only call this if you think that this tread is being reused after
    /// a previous call execution which may not have completed correctly.
    /// You do not need to use this method if you always guarantee to call
    /// the <see cref="IDisposable.Dispose"/> method of the <see cref="IDisposable"/>
    /// returned from <see cref="Push"/> even in exceptional circumstances,
    /// for example by using the <c>using(log4net.ThreadContext.Stacks["NDC"].Push("Stack_Message"))</c> 
    /// syntax.
    /// </para>
    /// </remarks>
    public void Clear() => m_stack.Clear();

    /// <summary>
    /// Removes the top context from this stack.
    /// </summary>
    /// <returns>The message in the context that was removed from the top of this stack.</returns>
    /// <remarks>
    /// <para>
    /// Remove the top context from this stack, and return
    /// it to the caller. If this stack is empty then an
    /// empty string (not <see langword="null"/>) is returned.
    /// </para>
    /// </remarks>
    public string? Pop()
    {
      Stack<StackFrame> stack = m_stack;
      if (stack.Count > 0)
      {
        return stack.Pop().Message;
      }
      return string.Empty;
    }

    /// <summary>
    /// Pushes a new context message into this stack.
    /// </summary>
    /// <param name="message">The new context message.</param>
    /// <returns>
    /// An <see cref="IDisposable"/> that can be used to clean up the context stack.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Pushes a new context onto this stack. An <see cref="IDisposable"/>
    /// is returned that can be used to clean up this stack. This
    /// can be easily combined with the <c>using</c> keyword to scope the
    /// context.
    /// </para>
    /// </remarks>
    /// <example>Simple example of using the <c>Push</c> method with the <c>using</c> keyword.
    /// <code lang="C#">
    /// using(log4net.ThreadContext.Stacks["NDC"].Push("Stack_Message"))
    /// {
    ///    log.Warn("This should have an ThreadContext Stack message");
    ///  }
    /// </code>
    /// </example>
    public IDisposable Push(string? message)
    {
      Stack<StackFrame> stack = m_stack;
      stack.Push(new StackFrame(message, (stack.Count > 0) ? stack.Peek() : null));

      return new AutoPopStackFrame(stack, stack.Count - 1);
    }

    /// <summary>
    /// Returns the top context from this stack.
    /// </summary>
    /// <returns>The message in the context from the top of this stack.</returns>
    /// <remarks>
    /// <para>
    /// Returns the top context from this stack. If this stack is empty then an
    /// empty string (not <see langword="null"/>) is returned.
    /// </para>
    /// </remarks>
    public string? Peek()
    {
      Stack<StackFrame> stack = m_stack;
      if (stack.Count > 0)
      {
        return stack.Peek().Message;
      }
      return "";
    }

    /// <summary>
    /// Gets the current context information for this stack.
    /// </summary>
    /// <returns>The current context information.</returns>
    internal string? GetFullMessage()
    {
      Stack<StackFrame> stack = m_stack;
      if (stack.Count > 0)
      {
        return stack.Peek().FullMessage;
      }
      return null;
    }

    /// <summary>
    /// Gets and sets the internal stack used by this <see cref="ThreadContextStack"/>
    /// </summary>
    /// <value>The internal storage stack</value>
    /// <remarks>
    /// <para>
    /// This property is provided only to support backward compatibility 
    /// of the <see cref="NDC"/>. Typically the internal stack should not
    /// be modified.
    /// </para>
    /// </remarks>
    internal Stack InternalStack
    {
        get => new Stack(new Stack(m_stack));
        set
        {
          m_stack.Clear();
          var frames = (StackFrame[])value.ToArray();
          for (int i = frames.Length - 1; i >= 0; i--)
          {
             m_stack.Push(frames[i]);
          }
        }
    }

    /// <summary>
    /// Gets the current context information for this stack.
    /// </summary>
    public override string? ToString() => GetFullMessage();

    /// <summary>
    /// Get a portable version of this object
    /// </summary>
    object? IFixingRequired.GetFixedObject() => GetFullMessage();

    /// <summary>
    /// Inner class used to represent a single context frame in the stack.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Inner class used to represent a single context frame in the stack.
    /// </para>
    /// </remarks>
    internal sealed class StackFrame
    {
      private readonly StackFrame? m_parent;
      private string? m_fullMessage;

      /// <summary>
      /// Constructor
      /// </summary>
      /// <param name="message">The message for this context.</param>
      /// <param name="parent">The parent context in the chain.</param>
      /// <remarks>
      /// <para>
      /// Initializes a new instance of the <see cref="StackFrame" /> class
      /// with the specified message and parent context.
      /// </para>
      /// </remarks>
      internal StackFrame(string? message, StackFrame? parent)
      {
        Message = message;
        m_parent = parent;

        if (parent is null)
        {
          m_fullMessage = message;
        }
      }

      /// <summary>
      /// Get the message.
      /// </summary>
      /// <value>The message.</value>
      /// <remarks>
      /// <para>
      /// Get the message.
      /// </para>
      /// </remarks>
      internal string? Message { get; }

      /// <summary>
      /// Gets the full text of the context down to the root level.
      /// </summary>
      /// <value>
      /// The full text of the context down to the root level.
      /// </value>
      /// <remarks>
      /// <para>
      /// Gets the full text of the context down to the root level.
      /// </para>
      /// </remarks>
      internal string? FullMessage
      {
        get
        {
          if (m_fullMessage is null && m_parent is not null)
          {
            m_fullMessage = string.Concat(m_parent.FullMessage, " ", Message);
          }
          return m_fullMessage;
        }
      }
    }

    /// <summary>
    /// Struct returned from the <see cref="ThreadContextStack.Push"/> method.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This struct implements the <see cref="IDisposable"/> and is designed to be used
    /// with the <see langword="using"/> pattern to remove the stack frame at the end of the scope.
    /// </para>
    /// </remarks>
    private readonly struct AutoPopStackFrame : IDisposable
    {
      /// <summary>
      /// The ThreadContextStack internal stack
      /// </summary>
      private readonly Stack<StackFrame> m_frameStack;

      /// <summary>
      /// The depth to trim the stack to when this instance is disposed
      /// </summary>
      private readonly int m_frameDepth;

      /// <summary>
      /// Initializes a new instance of the <see cref="AutoPopStackFrame" /> class with
      /// the specified stack and return depth.
      /// </summary>
      /// <param name="frameStack">The internal stack used by the ThreadContextStack.</param>
      /// <param name="frameDepth">The depth to return the stack to when this object is disposed.</param>
      internal AutoPopStackFrame(Stack<StackFrame> frameStack, int frameDepth)
      {
        m_frameStack = frameStack;
        m_frameDepth = frameDepth;
      }

      /// <summary>
      /// Returns the stack to the correct depth.
      /// </summary>
      public void Dispose()
      {
        if (m_frameDepth >= 0)
        {
          while (m_frameStack.Count > m_frameDepth)
          {
            m_frameStack.Pop();
          }
        }
      }
    }
  }
}
