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
using System.Collections.Generic;
using log4net.Core;

namespace log4net.Util;

/// <summary>
/// Delegate type used for LogicalThreadContextStack's callbacks.
/// </summary>
public delegate void TwoArgAction<T1, T2>(T1 t1, T2 t2);

/// <summary>
/// Implementation of Stack for the <see cref="LogicalThreadContext"/>
/// </summary>
/// <author>Nicko Cadell</author>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public sealed class LogicalThreadContextStack : IFixingRequired
{
  /// <summary>
  /// The stack store.
  /// </summary>
  private Stack<StackFrame> _stack = new();

  /// <summary>
  /// The name of this <see cref="LogicalThreadContextStack"/> within the
  /// <see cref="LogicalThreadContextProperties"/>.
  /// </summary>
  private readonly string _propertyKey;

  /// <summary>
  /// The callback used to let the <see cref="LogicalThreadContextStacks"/> register a
  /// new instance of a <see cref="LogicalThreadContextStack"/>.
  /// </summary>
  private readonly TwoArgAction<string, LogicalThreadContextStack> _registerNew;

  /// <summary>
  /// Internal constructor
  /// </summary>
  /// <remarks>
  /// <para>
  /// Initializes a new instance of the <see cref="LogicalThreadContextStack" /> class. 
  /// </para>
  /// </remarks>
  internal LogicalThreadContextStack(string propertyKey, TwoArgAction<string, LogicalThreadContextStack> registerNew)
  {
    _propertyKey = propertyKey;
    _registerNew = registerNew;
  }

  /// <summary>
  /// Gets the number of messages in the stack.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The current number of messages in the stack. That is
  /// the number of times <see cref="Push"/> has been called
  /// minus the number of times <see cref="Pop"/> has been called.
  /// </para>
  /// </remarks>
  public int Count => _stack.Count;

  /// <summary>
  /// Clears all the contextual information held in this stack.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Clears all the contextual information held in this stack.
  /// Only call this if you think that this thread is being reused after
  /// a previous call execution which may not have completed correctly.
  /// You do not need to use this method if you always guarantee to call
  /// the <see cref="IDisposable.Dispose"/> method of the <see cref="IDisposable"/>
  /// returned from <see cref="Push"/> even in exceptional circumstances,
  /// for example by using the <c>using(log4net.LogicalThreadContext.Stacks["NDC"].Push("Stack_Message"))</c> 
  /// syntax.
  /// </para>
  /// </remarks>
  public void Clear() => _registerNew(_propertyKey, new LogicalThreadContextStack(_propertyKey, _registerNew));

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
    // copy current stack
    Stack<StackFrame> stack = new(new Stack<StackFrame>(_stack));
    string? result = string.Empty;
    if (stack.Count > 0)
    {
      result = stack.Pop().Message;
    }
    LogicalThreadContextStack ltcs = new(_propertyKey, _registerNew) { _stack = stack };
    _registerNew(_propertyKey, ltcs);
    return result;
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
  /// can be easily combined with the <see langword="using"/> keyword to scope the
  /// context.
  /// </para>
  /// </remarks>
  /// <example>Simple example of using the <see cref="Push"/> method with the <see langword="using"/> keyword.
  /// <code lang="C#">
  /// using(log4net.LogicalThreadContext.Stacks["NDC"].Push("Stack_Message"))
  /// {
  ///    log.Warn("This should have an ThreadContext Stack message");
  ///  }
  /// </code>
  /// </example>
  public IDisposable Push(string? message)
  {
    // do modifications on a copy
    Stack<StackFrame> stack = new(new Stack<StackFrame>(_stack));
    stack.Push(new StackFrame(message, (stack.Count > 0) ? stack.Peek() : null));

    LogicalThreadContextStack contextStack = new(_propertyKey, _registerNew) { _stack = stack };
    _registerNew(_propertyKey, contextStack);
    return new AutoPopStackFrame(contextStack, stack.Count - 1);
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
    Stack<StackFrame> local = _stack;
    if (local.Count > 0)
    {
      return local.Peek().Message;
    }
    return string.Empty;
  }

  /// <summary>
  /// Gets the current context information for this stack.
  /// </summary>
  /// <returns>The current context information.</returns>
  internal string? GetFullMessage()
  {
    Stack<StackFrame> local = _stack;
    if (local.Count > 0)
    {
      return local.Peek().FullMessage;
    }
    return null;
  }

  /// <summary>
  /// Gets the current context information for this stack.
  /// </summary>
  /// <returns>Gets the current context information</returns>
  public override string? ToString() => GetFullMessage();

  /// <summary>
  /// Gets a cross-thread portable version of this object
  /// </summary>
  object? IFixingRequired.GetFixedObject() => GetFullMessage();

  /// <summary>
  /// Inner class used to represent a single context frame in the stack.
  /// </summary>
  internal sealed class StackFrame
  {
    private readonly StackFrame? _parent;
    private string? _fullMessage;

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
      _parent = parent;

      if (parent is null)
      {
        _fullMessage = message;
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
        if (_fullMessage is null && _parent is not null)
        {
          _fullMessage = string.Concat(_parent.FullMessage, " ", Message);
        }

        return _fullMessage;
      }
    }
  }

  /// <summary>
  /// Struct returned from the <see cref="Push"/> method.
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
    /// The depth to trim the stack to when this instance is disposed
    /// </summary>
    private readonly int _frameDepth;

    /// <summary>
    /// The outer LogicalThreadContextStack.
    /// </summary>
    private readonly LogicalThreadContextStack _logicalThreadContextStack;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logicalThreadContextStack">The internal stack used by the ThreadContextStack.</param>
    /// <param name="frameDepth">The depth to return the stack to when this object is disposed.</param>
    /// <remarks>
    /// <para>
    /// Initializes a new instance of the <see cref="AutoPopStackFrame" /> class with
    /// the specified stack and return depth.
    /// </para>
    /// </remarks>
    internal AutoPopStackFrame(LogicalThreadContextStack logicalThreadContextStack, int frameDepth)
    {
      _frameDepth = frameDepth;
      _logicalThreadContextStack = logicalThreadContextStack;
    }

    /// <summary>
    /// Returns the stack to the correct depth.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns the stack to the correct depth.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
      if (_frameDepth >= 0)
      {
        Stack<StackFrame> local = new(new Stack<StackFrame>(_logicalThreadContextStack._stack));
        while (local.Count > _frameDepth)
        {
          local.Pop();
        }
        LogicalThreadContextStack ltcs = new(_logicalThreadContextStack._propertyKey, _logicalThreadContextStack._registerNew)
        {
          _stack = local
        };
        _logicalThreadContextStack._registerNew(_logicalThreadContextStack._propertyKey, ltcs);
      }
    }
  }
}