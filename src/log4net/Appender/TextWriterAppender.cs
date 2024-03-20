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

using log4net.Util;
using log4net.Layout;
using log4net.Core;

namespace log4net.Appender
{
  /// <summary>
  /// Sends logging events to a <see cref="TextWriter"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// An Appender that writes to a <see cref="TextWriter"/>.
  /// </para>
  /// <para>
  /// This appender may be used stand alone if initialized with an appropriate
  /// writer, however it is typically used as a base class for an appender that
  /// can open a <see cref="TextWriter"/> to write to.
  /// </para>
  /// </remarks>
  /// <author>Nicko Cadell</author>
  /// <author>Gert Driesen</author>
  /// <author>Douglas de la Torre</author>
  public class TextWriterAppender : AppenderSkeleton
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="TextWriterAppender" /> class.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default constructor.
    /// </para>
    /// </remarks>
    public TextWriterAppender()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextWriterAppender" /> class and
    /// sets the output destination to a new <see cref="StreamWriter"/> initialized 
    /// with the specified <see cref="Stream"/>.
    /// </summary>
    /// <param name="layout">The layout to use with this appender.</param>
    /// <param name="os">The <see cref="Stream"/> to output to.</param>
    /// <remarks>
    /// <para>
    /// Obsolete constructor.
    /// </para>
    /// </remarks>
    [Obsolete("Instead use the default constructor and set the Layout & Writer properties")]
    public TextWriterAppender(ILayout layout, Stream os) : this(layout, new StreamWriter(os))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextWriterAppender" /> class and sets
    /// the output destination to the specified <see cref="StreamWriter" />.
    /// </summary>
    /// <param name="layout">The layout to use with this appender</param>
    /// <param name="writer">The <see cref="TextWriter" /> to output to</param>
    /// <remarks>
    /// The <see cref="TextWriter" /> must have been previously opened.
    /// </remarks>
    /// <remarks>
    /// <para>
    /// Obsolete constructor.
    /// </para>
    /// </remarks>
    [Obsolete("Instead use the default constructor and set the Layout & Writer properties")]
    public TextWriterAppender(ILayout layout, TextWriter writer)
    {
      Layout = layout;
      Writer = writer;
    }

    /// <summary>
    /// Gets or set whether the appender will flush at the end 
    /// of each append operation.
    /// </summary>
    /// <value>
    /// <para>
    /// The default behavior is to flush at the end of each 
    /// append operation.
    /// </para>
    /// <para>
    /// If this option is set to <c>false</c>, then the underlying 
    /// stream can defer persisting the logging event to a later 
    /// time.
    /// </para>
    /// </value>
    /// <remarks>
    /// Avoiding the flush operation at the end of each append results in
    /// a performance gain of 10 to 20 percent. However, there is a safety
    /// trade-off involved in skipping flushing. Indeed, when flushing is
    /// skipped, then it is likely that the last few log events will not
    /// be recorded on disk when the application exits. This is a high
    /// price to pay even for a 20% performance gain.
    /// </remarks>
    public bool ImmediateFlush { get; set; } = true;

    /// <summary>
    /// Sets the <see cref="TextWriter"/> where the log output will go.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The specified <see cref="TextWriter"/> must be open and writable.
    /// </para>
    /// <para>
    /// The <see cref="TextWriter"/> will be closed when the appender 
    /// instance is closed.
    /// </para>
    /// <para>
    /// <b>Note:</b> Logging to an unopened <see cref="TextWriter"/> will fail.
    /// </para>
    /// </remarks>
    public virtual TextWriter? Writer
    {
      get => QuietWriter;
      set
      {
        lock (m_lockObj)
        {
          Reset();
          if (value is not null)
          {
            QuietWriter = new QuietTextWriter(value, ErrorHandler);
            WriteHeader();
          }
        }
      }
    }

    /// <summary>
    /// This method determines if there is a sense in attempting to append.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method checks if an output target has been set and if a
    /// layout has been set. 
    /// </para>
    /// </remarks>
    /// <returns><c>false</c> if any of the preconditions fail.</returns>
    protected override bool PreAppendCheck()
    {
      if (!base.PreAppendCheck())
      {
        return false;
      }

      if (QuietWriter == null)
      {
        // Allow subclass to lazily create the writer
        PrepareWriter();

        if (QuietWriter == null)
        {
          ErrorHandler.Error("No output stream or file set for the appender named [" + Name + "].");
          return false;
        }
      }
      if (QuietWriter.Closed)
      {
        ErrorHandler.Error("Output stream for appender named [" + Name + "] has been closed.");
        return false;
      }

      return true;
    }

    /// <summary>
    /// This method is called by the <see cref="M:AppenderSkeleton.DoAppend(LoggingEvent)"/>
    /// method. 
    /// </summary>
    /// <param name="loggingEvent">The event to log.</param>
    /// <remarks>
    /// <para>
    /// Writes a log statement to the output stream if the output stream exists 
    /// and is writable.  
    /// </para>
    /// <para>
    /// The format of the output will depend on the appender's layout.
    /// </para>
    /// </remarks>
    protected override void Append(LoggingEvent loggingEvent)
    {
      if (QuietWriter is null)
      {
        return;
      }

      RenderLoggingEvent(QuietWriter, loggingEvent);

      if (ImmediateFlush)
      {
        QuietWriter.Flush();
      }
    }

    /// <summary>
    /// This method is called by the <see cref="M:AppenderSkeleton.DoAppend(LoggingEvent[])"/>
    /// method. 
    /// </summary>
    /// <param name="loggingEvents">The array of events to log.</param>
    /// <remarks>
    /// <para>
    /// This method writes all the bulk logged events to the output writer
    /// before flushing the stream.
    /// </para>
    /// </remarks>
    protected override void Append(LoggingEvent[] loggingEvents)
    {
      if (QuietWriter is null)
      {
        return;
      }

      foreach (LoggingEvent loggingEvent in loggingEvents)
      {
        RenderLoggingEvent(QuietWriter, loggingEvent);
      }

      if (ImmediateFlush)
      {
        QuietWriter.Flush();
      }
    }

    /// <summary>
    /// Close this appender instance. The underlying stream or writer is also closed.
    /// </summary>
    /// <remarks>
    /// Closed appenders cannot be reused.
    /// </remarks>
    protected override void OnClose()
    {
      lock (m_lockObj)
      {
        Reset();
      }
    }

    /// <summary>
    /// Gets or set the <see cref="IErrorHandler"/> and the underlying 
    /// <see cref="QuietTextWriter"/>, if any, for this appender. 
    /// </summary>
    /// <value>
    /// The <see cref="IErrorHandler"/> for this appender.
    /// </value>
    public override IErrorHandler ErrorHandler
    {
      get => base.ErrorHandler;
      set
      {
        lock (m_lockObj)
        {
          if (value is null)
          {
            LogLog.Warn(declaringType, "TextWriterAppender: You have tried to set a null error-handler.");
          }
          else
          {
            base.ErrorHandler = value;
            if (QuietWriter is not null)
            {
              QuietWriter.ErrorHandler = value;
            }
          }
        }
      }
    }

    /// <summary>
    /// This appender requires a <see cref="Layout"/> to be set.
    /// </summary>
    protected override bool RequiresLayout => true;

    /// <summary>
    /// Writes the footer and closes the underlying <see cref="TextWriter"/>.
    /// </summary>
    protected virtual void WriteFooterAndCloseWriter()
    {
      WriteFooter();
      CloseWriter();
    }

    /// <summary>
    /// Closes the underlying <see cref="TextWriter"/>.
    /// </summary>
    protected virtual void CloseWriter()
    {
      if (QuietWriter is not null)
      {
        try
        {
          QuietWriter.Close();
        }
        catch (Exception e)
        {
          ErrorHandler.Error($"Could not close writer [{QuietWriter}]", e);
          // do need to invoke an error handler
          // at this late stage
        }
      }
    }

    /// <summary>
    /// Clears internal references to the underlying <see cref="TextWriter" /> 
    /// and other variables.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Subclasses can override this method for an alternate closing behavior.
    /// </para>
    /// </remarks>
    protected virtual void Reset()
    {
      WriteFooterAndCloseWriter();
      QuietWriter = null;
    }

    /// <summary>
    /// Writes a footer as produced by the embedded layout's <see cref="ILayout.Footer"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Writes a footer as produced by the embedded layout's <see cref="ILayout.Footer"/> property.
    /// </para>
    /// </remarks>
    protected virtual void WriteFooter()
    {
      if (Layout is not null && QuietWriter is not null && !QuietWriter.Closed)
      {
        string? f = Layout.Footer;
        if (f is not null)
        {
          QuietWriter.Write(f);
        }
      }
    }

    /// <summary>
    /// Writes a header produced by the embedded layout's <see cref="ILayout.Header"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Writes a header produced by the embedded layout's <see cref="ILayout.Header"/> property.
    /// </para>
    /// </remarks>
    protected virtual void WriteHeader()
    {
      if (Layout is not null && QuietWriter is not null && !QuietWriter.Closed)
      {
        string h = Layout.Header;
        if (h is not null)
        {
          QuietWriter.Write(h);
        }
      }
    }

    /// <summary>
    /// Called to allow a subclass to lazily initialize the writer
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is called when an event is logged and the <see cref="Writer"/> or
    /// <see cref="QuietWriter"/> have not been set. This allows a subclass to
    /// attempt to initialize the writer multiple times.
    /// </para>
    /// </remarks>
    protected virtual void PrepareWriter()
    {
    }

    /// <summary>
    /// Gets or sets the <see cref="log4net.Util.QuietTextWriter"/> where logging events
    /// will be written to. 
    /// </summary>
    /// <value>
    /// The <see cref="log4net.Util.QuietTextWriter"/> where logging events are written.
    /// </value>
    /// <remarks>
    /// <para>
    /// This is the <see cref="log4net.Util.QuietTextWriter"/> where logging events
    /// will be written to. 
    /// </para>
    /// </remarks>
    protected QuietTextWriter? QuietWriter { get; set; }

    private readonly object m_lockObj = new();

    /// <summary>
    /// The fully qualified type of the TextWriterAppender class.
    /// </summary>
    /// <remarks>
    /// Used by the internal logger to record the Type of the
    /// log message.
    /// </remarks>
    private static readonly Type declaringType = typeof(TextWriterAppender);

    /// <summary>
    /// Flushes any buffered log data.
    /// </summary>
    /// <param name="millisecondsTimeout">The maximum time to wait for logging events to be flushed.</param>
    /// <returns><c>True</c> if all logging events were flushed successfully, else <c>false</c>.</returns>
    public override bool Flush(int millisecondsTimeout)
    {
      // Nothing to do if ImmediateFlush is true
      if (ImmediateFlush)
      {
        return true;
      }

      // lock(this) will block any Appends while the buffer is flushed.
      lock (m_lockObj)
      {
        QuietWriter?.Flush();
      }

      return true;
    }
  }
}
