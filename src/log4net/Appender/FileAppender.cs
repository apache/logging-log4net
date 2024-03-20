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
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using log4net.Util;
using log4net.Layout;
using log4net.Core;
using System.Threading.Tasks;

namespace log4net.Appender
{
  /// <summary>
  /// Appends logging events to a file.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Logging events are sent to the file specified by
  /// the <see cref="File"/> property.
  /// </para>
  /// <para>
  /// The file can be opened in either append or overwrite mode 
  /// by specifying the <see cref="AppendToFile"/> property.
  /// If the file path is relative it is taken as relative from 
  /// the application base directory. The file encoding can be
  /// specified by setting the <see cref="Encoding"/> property.
  /// </para>
  /// <para>
  /// The layout's <see cref="ILayout.Header"/> and <see cref="ILayout.Footer"/>
  /// values will be written each time the file is opened and closed
  /// respectively. If the <see cref="AppendToFile"/> property is <see langword="true"/>
  /// then the file may contain multiple copies of the header and footer.
  /// </para>
  /// <para>
  /// This appender will first try to open the file for writing when <see cref="ActivateOptions"/>
  /// is called. This will typically be during configuration.
  /// If the file cannot be opened for writing the appender will attempt
  /// to open the file again each time a message is logged to the appender.
  /// If the file cannot be opened for writing when a message is logged then
  /// the message will be discarded by this appender.
  /// </para>
  /// <para>
  /// The <see cref="FileAppender"/> supports pluggable file locking models via
  /// the <see cref="LockingModel"/> property.
  /// The default behavior, implemented by <see cref="FileAppender.ExclusiveLock"/> 
  /// is to obtain an exclusive write lock on the file until this appender is closed.
  /// The alternative models only hold a
  /// write lock while the appender is writing a logging event (<see cref="FileAppender.MinimalLock"/>)
  /// or synchronize by using a named system-wide Mutex (<see cref="FileAppender.InterProcessLock"/>).
  /// </para>
  /// <para>
  /// All locking strategies have issues and you should seriously consider using a different strategy that
  /// avoids having multiple processes logging to the same file.
  /// </para>
  /// </remarks>
  /// <author>Nicko Cadell</author>
  /// <author>Gert Driesen</author>
  /// <author>Rodrigo B. de Oliveira</author>
  /// <author>Douglas de la Torre</author>
  /// <author>Niall Daley</author>
  public class FileAppender : TextWriterAppender
  {
    /// <summary>
    /// Write only <see cref="Stream"/> that uses the <see cref="LockingModelBase"/> 
    /// to manage access to an underlying resource.
    /// </summary>
    private sealed class LockingStream : Stream, IDisposable
    {
      [Serializable]
      public sealed class LockStateException : LogException
      {
        public LockStateException(string message)
            : base(message)
        {
        }

        public LockStateException()
        {
        }

        public LockStateException(string message, Exception innerException) : base(message, innerException)
        {
        }

        private LockStateException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
      }

      private Stream? m_realStream;
      private readonly LockingModelBase m_lockingModel;
      private int m_lockLevel;

      public LockingStream(LockingModelBase locking)
      {
        if (locking is null)
        {
          throw new ArgumentNullException(nameof(locking));
        }

        m_lockingModel = locking;
      }

      protected override void Dispose(bool disposing)
      {
        m_lockingModel.CloseFile();
        base.Dispose(disposing);
      }

      public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
      {
        return AssertLocked().ReadAsync(buffer, offset, count, cancellationToken);
      }

      public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
      {
        AssertLocked();
        return base.WriteAsync(buffer, offset, count, cancellationToken);
      }

      public override void Flush()
      {
        AssertLocked().Flush();
      }

      public override int Read(byte[] buffer, int offset, int count)
      {
        return AssertLocked().Read(buffer, offset, count);
      }

      public override int ReadByte()
      {
        return AssertLocked().ReadByte();
      }

      public override long Seek(long offset, SeekOrigin origin)
      {
        return AssertLocked().Seek(offset, origin);
      }

      public override void SetLength(long value)
      {
        AssertLocked().SetLength(value);
      }

      void IDisposable.Dispose()
      {
        Dispose(true);
      }

      public override void Write(byte[] buffer, int offset, int count)
      {
        AssertLocked().Write(buffer, offset, count);
      }

      public override void WriteByte(byte value)
      {
        AssertLocked().WriteByte(value);
      }

      // Properties
      public override bool CanRead
      {
        get { return false; }
      }

      public override bool CanSeek
      {
        get
        {
          return AssertLocked().CanSeek;
        }
      }

      public override bool CanWrite
      {
        get
        {
          return AssertLocked().CanWrite;
        }
      }

      public override long Length
      {
        get
        {
          return AssertLocked().Length;
        }
      }

      public override long Position
      {
        get
        {
          return AssertLocked().Position;
        }
        set
        {
          AssertLocked().Position = value;
        }
      }

      private Stream AssertLocked()
      {
        if (m_realStream is null)
        {
          throw new LockStateException("The file is not currently locked");
        }

        return m_realStream;
      }

      public bool AcquireLock()
      {
        bool ret = false;
        lock (this)
        {
          if (m_lockLevel == 0)
          {
            // If lock is already acquired, nop
            m_realStream = m_lockingModel.AcquireLock();
          }

          if (m_realStream is not null)
          {
            m_lockLevel++;
            ret = true;
          }
        }

        return ret;
      }

      public void ReleaseLock()
      {
        lock (this)
        {
          m_lockLevel--;
          if (m_lockLevel == 0)
          {
            // If already unlocked, nop
            m_lockingModel.ReleaseLock();
            m_realStream = null;
          }
        }
      }
    }

    /// <summary>
    /// Locking model base class
    /// </summary>
    /// <remarks>
    /// <para>
    /// Base class for the locking models available to the <see cref="FileAppender"/> derived loggers.
    /// </para>
    /// </remarks>
    public abstract class LockingModelBase
    {
      /// <summary>
      /// Open the output file
      /// </summary>
      /// <param name="filename">The filename to use</param>
      /// <param name="append">Whether to append to the file, or overwrite</param>
      /// <param name="encoding">The encoding to use</param>
      /// <remarks>
      /// <para>
      /// Open the file specified and prepare for logging. 
      /// No writes will be made until <see cref="AcquireLock"/> is called.
      /// Must be called before any calls to <see cref="AcquireLock"/>,
      /// <see cref="ReleaseLock"/> and <see cref="CloseFile"/>.
      /// </para>
      /// </remarks>
      public abstract void OpenFile(string filename, bool append, Encoding encoding);

      /// <summary>
      /// Close the file
      /// </summary>
      /// <remarks>
      /// <para>
      /// Close the file. No further writes will be made.
      /// </para>
      /// </remarks>
      public abstract void CloseFile();

      /// <summary>
      /// Initializes all resources used by this locking model.
      /// </summary>
      public abstract void ActivateOptions();

      /// <summary>
      /// Disposes all resources that were initialized by this locking model.
      /// </summary>
      public abstract void OnClose();

      /// <summary>
      /// Acquire the lock on the file
      /// </summary>
      /// <returns>A stream that is ready to be written to, or null if there is no active stream because uninitialized or error.</returns>
      /// <remarks>
      /// <para>
      /// Acquire the lock on the file in preparation for writing to it.
      /// Returns a stream pointing to the file. <see cref="ReleaseLock"/>
      /// must be called to release the lock on the output file when the return
      /// value is not null.
      /// </para>
      /// </remarks>
      public abstract Stream? AcquireLock();

      /// <summary>
      /// Releases the lock on the file
      /// </summary>
      /// <remarks>
      /// <para>
      /// No further writes will be made to the stream until <see cref="AcquireLock"/> is called again.
      /// </para>
      /// </remarks>
      public abstract void ReleaseLock();

      /// <summary>
      /// Gets or sets the <see cref="FileAppender"/> for this LockingModel
      /// </summary>
      /// <value>
      /// The <see cref="FileAppender"/> for this LockingModel
      /// </value>
      /// <remarks>
      /// <para>
      /// The file appender this locking model is attached to and working on
      /// behalf of.
      /// </para>
      /// <para>
      /// The file appender is used to locate the security context and the error handler to use.
      /// </para>
      /// <para>
      /// The value of this property will be set before <see cref="OpenFile"/> is
      /// called.
      /// </para>
      /// </remarks>
      public FileAppender? CurrentAppender { get; set; }

      /// <summary>
      /// Helper method that creates a FileStream under CurrentAppender's SecurityContext.
      /// </summary>
      /// <remarks>
      /// <para>
      /// Typically called during OpenFile or AcquireLock. 
      /// </para>
      /// <para>
      /// If the directory portion of the <paramref name="filename"/> does not exist, it is created
      /// via Directory.CreateDirectory.
      /// </para>
      /// </remarks>
      /// <param name="filename"></param>
      /// <param name="append"></param>
      /// <param name="fileShare"></param>
      /// <returns></returns>
      protected Stream CreateStream(string filename, bool append, FileShare fileShare)
      {
        filename = Environment.ExpandEnvironmentVariables(filename);
        using (CurrentAppender?.SecurityContext?.Impersonate(this))
        {
          // Ensure that the directory structure exists
          string? directoryFullName = Path.GetDirectoryName(filename);

          // Only create the directory if it does not exist
          // doing this check here resolves some permissions failures
          if (directoryFullName is not null && !Directory.Exists(directoryFullName))
          {
            Directory.CreateDirectory(directoryFullName);
          }

          FileMode fileOpenMode = append
              ? FileMode.Append
              : FileMode.Create;
          return new FileStream(filename, fileOpenMode, FileAccess.Write, fileShare);
        }
      }

      /// <summary>
      /// Helper method to close <paramref name="stream"/> under CurrentAppender's SecurityContext.
      /// </summary>
      /// <remarks>
      /// Does not set <paramref name="stream"/> to null.
      /// </remarks>
      /// <param name="stream"></param>
      protected void CloseStream(Stream stream)
      {
        using (CurrentAppender?.SecurityContext?.Impersonate(this))
        {
          stream.Dispose();
        }
      }
    }

    /// <summary>
    /// Hold an exclusive lock on the output file
    /// </summary>
    /// <remarks>
    /// <para>
    /// Open the file once for writing and hold it open until <see cref="CloseFile"/> is called. 
    /// Maintains an exclusive lock on the file during this time.
    /// </para>
    /// </remarks>
    public class ExclusiveLock : LockingModelBase
    {
      private Stream? m_stream;

      /// <summary>
      /// Open the file specified and prepare for logging.
      /// </summary>
      /// <param name="filename">The filename to use</param>
      /// <param name="append">Whether to append to the file, or overwrite</param>
      /// <param name="encoding">The encoding to use</param>
      /// <remarks>
      /// <para>
      /// Open the file specified and prepare for logging. 
      /// No writes will be made until <see cref="AcquireLock"/> is called.
      /// Must be called before any calls to <see cref="AcquireLock"/>,
      /// <see cref="ReleaseLock"/> and <see cref="CloseFile"/>.
      /// </para>
      /// </remarks>
      public override void OpenFile(string filename, bool append, Encoding encoding)
      {
        try
        {
          m_stream = CreateStream(filename, append, FileShare.Read);
        }
        catch (Exception e1)
        {
          CurrentAppender?.ErrorHandler.Error("Unable to acquire lock on file " + filename + ". " +
              e1.Message);
        }
      }

      /// <summary>
      /// Close the file
      /// </summary>
      /// <remarks>
      /// <para>
      /// Close the file. No further writes will be made.
      /// </para>
      /// </remarks>
      public override void CloseFile()
      {
        if (m_stream is not null)
        {
          CloseStream(m_stream);
          m_stream = null;
        }
      }

      /// <summary>
      /// Acquire the lock on the file
      /// </summary>
      /// <returns>A stream that is ready to be written to.</returns>
      /// <remarks>
      /// <para>
      /// Does nothing. The lock is already taken
      /// </para>
      /// </remarks>
      public override Stream? AcquireLock()
      {
          return m_stream;
      }

      /// <summary>
      /// Release the lock on the file
      /// </summary>
      /// <remarks>
      /// <para>
      /// Does nothing. The lock will be released when the file is closed.
      /// </para>
      /// </remarks>
      public override void ReleaseLock()
      {
        //NOP
      }

      /// <summary>
      /// Initializes all resources used by this locking model.
      /// </summary>
      public override void ActivateOptions()
      {
        //NOP
      }

      /// <summary>
      /// Disposes all resources that were initialized by this locking model.
      /// </summary>
      public override void OnClose()
      {
        //NOP
      }
    }

    /// <summary>
    /// Acquires the file lock for each write
    /// </summary>
    /// <remarks>
    /// <para>
    /// Opens the file once for each <see cref="AcquireLock"/>/<see cref="ReleaseLock"/> cycle, 
    /// thus holding the lock for the minimal amount of time. This method of locking
    /// is considerably slower than <see cref="FileAppender.ExclusiveLock"/> but allows 
    /// other processes to move/delete the log file whilst logging continues.
    /// </para>
    /// </remarks>
    public class MinimalLock : LockingModelBase
    {
      private string? m_filename;
      private bool m_append;
      private Stream? m_stream;

      /// <summary>
      /// Prepares to open the file when the first message is logged.
      /// </summary>
      /// <param name="filename">The filename to use</param>
      /// <param name="append">Whether to append to the file, or overwrite</param>
      /// <param name="encoding">The encoding to use</param>
      /// <remarks>
      /// <para>
      /// Open the file specified and prepare for logging. 
      /// No writes will be made until <see cref="AcquireLock"/> is called.
      /// Must be called before any calls to <see cref="AcquireLock"/>,
      /// <see cref="ReleaseLock"/> and <see cref="CloseFile"/>.
      /// </para>
      /// </remarks>
      public override void OpenFile(string filename, bool append, Encoding encoding)
      {
        m_filename = filename;
        m_append = append;
      }

      /// <summary>
      /// Close the file
      /// </summary>
      /// <remarks>
      /// <para>
      /// Close the file. No further writes will be made.
      /// </para>
      /// </remarks>
      public override void CloseFile()
      {
        // NOP
      }

      /// <summary>
      /// Acquire the lock on the file
      /// </summary>
      /// <returns>A stream that is ready to be written to.</returns>
      /// <remarks>
      /// <para>
      /// Acquire the lock on the file in preparation for writing to it. 
      /// Return a stream pointing to the file. <see cref="ReleaseLock"/>
      /// must be called to release the lock on the output file.
      /// </para>
      /// </remarks>
      public override Stream? AcquireLock()
      {
        if (m_stream is null)
        {
          if (m_filename is not null)
          {
            try
            {
              m_stream = CreateStream(m_filename, m_append, FileShare.Read);
              m_append = true;
            }
            catch (Exception e1)
            {
              CurrentAppender?.ErrorHandler.Error("Unable to acquire lock on file " + m_filename + ". " +
                  e1.Message);
            }
          }
          else
          {
            CurrentAppender?.ErrorHandler.Error($"Unable to acquire lock because {nameof(OpenFile)} has not been called");
          }
        }

        return m_stream;
      }

      /// <summary>
      /// Release the lock on the file
      /// </summary>
      /// <remarks>
      /// <para>
      /// Release the lock on the file. No further writes will be made to the 
      /// stream until <see cref="AcquireLock"/> is called again.
      /// </para>
      /// </remarks>
      public override void ReleaseLock()
      {
        if (m_stream is not null)
        {
          CloseStream(m_stream);
          m_stream = null;
        }
      }

      /// <summary>
      /// Initializes all resources used by this locking model.
      /// </summary>
      public override void ActivateOptions()
      {
        //NOP
      }

      /// <summary>
      /// Disposes all resources that were initialized by this locking model.
      /// </summary>
      public override void OnClose()
      {
        //NOP
      }
    }

    /// <summary>
    /// Provides cross-process file locking.
    /// </summary>
    /// <author>Ron Grabowski</author>
    /// <author>Steve Wranovsky</author>
    public class InterProcessLock : LockingModelBase
    {
      private Mutex? m_mutex;
      private Stream? m_stream;
      private int m_recursiveWatch;

      /// <summary>
      /// Open the file specified and prepare for logging.
      /// </summary>
      /// <param name="filename">The filename to use</param>
      /// <param name="append">Whether to append to the file, or overwrite</param>
      /// <param name="encoding">The encoding to use</param>
      /// <remarks>
      /// <para>
      /// Open the file specified and prepare for logging. 
      /// No writes will be made until <see cref="AcquireLock"/> is called.
      /// Must be called before any calls to <see cref="AcquireLock"/>,
      /// -<see cref="ReleaseLock"/> and <see cref="CloseFile"/>.
      /// </para>
      /// </remarks>
      [System.Security.SecuritySafeCritical]
      public override void OpenFile(string filename, bool append, Encoding encoding)
      {
        try
        {
          m_stream = CreateStream(filename, append, FileShare.ReadWrite);
        }
        catch (Exception e1)
        {
          CurrentAppender?.ErrorHandler.Error("Unable to acquire lock on file " + filename + ". " +
              e1.Message);
        }
      }

      /// <summary>
      /// Close the file
      /// </summary>
      /// <remarks>
      /// <para>
      /// Close the file. No further writes will be made.
      /// </para>
      /// </remarks>
      public override void CloseFile()
      {
        try
        {
          if (m_stream is not null)
          {
            CloseStream(m_stream);
            m_stream = null;
          }
        }
        finally
        {
          ReleaseLock();
        }
      }

      /// <summary>
      /// Acquire the lock on the file
      /// </summary>
      /// <returns>A stream that is ready to be written to.</returns>
      /// <remarks>
      /// <para>
      /// Does nothing. The lock is already taken
      /// </para>
      /// </remarks>
      public override Stream? AcquireLock()
      {
        if (m_mutex is not null)
        {
          // TODO: add timeout?
          m_mutex.WaitOne();

          // increment recursive watch
          m_recursiveWatch++;

          // should always be true (and fast) for FileStream
          if (m_stream is not null)
          {
            if (m_stream.CanSeek)
            {
              m_stream.Seek(0, SeekOrigin.End);
            }
          }
          else
          {
            // this can happen when the file appender cannot open a file for writing
          }
        }
        else
        {
          CurrentAppender?.ErrorHandler.Error(
              "Programming error, no mutex available to acquire lock! From here on things will be dangerous!");
        }

        return m_stream;
      }

      /// <summary>
      /// Releases the lock and allows others to acquire a lock.
      /// </summary>
      public override void ReleaseLock()
      {
        if (m_mutex is not null)
        {
          if (m_recursiveWatch > 0)
          {
            m_recursiveWatch--;
            m_mutex.ReleaseMutex();
          }
        }
        else
        {
          CurrentAppender?.ErrorHandler.Error("Programming error, no mutex available to release the lock!");
        }
      }

      /// <summary>
      /// Initializes all resources used by this locking model.
      /// </summary>
      public override void ActivateOptions()
      {
        if (m_mutex is null)
        {
          if (CurrentAppender is not null)
          {
            if (CurrentAppender.File is not null)
            {
              string mutexFriendlyFilename = CurrentAppender.File
                  .Replace("\\", "_")
                  .Replace(":", "_")
                  .Replace("/", "_");

              m_mutex = new Mutex(false, mutexFriendlyFilename);
            }
            else
            {
              CurrentAppender.ErrorHandler.Error($"Current appender has no file name, {nameof(OpenFile)} not called or it encountered an error");
            }
          }
        }
        else
        {
          CurrentAppender?.ErrorHandler.Error("Programming error, mutex already initialized!");
        }
      }

      /// <summary>
      /// Disposes all resources that were initialized by this locking model.
      /// </summary>
      public override void OnClose()
      {
        if (m_mutex is not null)
        {
          m_mutex.Dispose();
          m_mutex = null;
        }
        else
        {
          CurrentAppender?.ErrorHandler.Error("Programming error, mutex not initialized!");
        }
      }
    }

    /// <summary>
    /// Hold no lock on the output file
    /// </summary>
    /// <remarks>
    /// <para>
    /// Open the file once and hold it open until <see cref="CloseFile"/> is called. 
    /// Maintains no lock on the file during this time.
    /// </para>
    /// </remarks>
    public class NoLock : LockingModelBase
    {
      private Stream? m_stream;

      /// <summary>
      /// Open the file specified and prepare for logging.
      /// </summary>
      /// <param name="filename">The filename to use</param>
      /// <param name="append">Whether to append to the file, or overwrite</param>
      /// <param name="encoding">The encoding to use</param>
      /// <remarks>
      /// <para>
      /// Open the file specified and prepare for logging. 
      /// No writes will be made until <see cref="AcquireLock"/> is called.
      /// Must be called before any calls to <see cref="AcquireLock"/>,
      /// <see cref="ReleaseLock"/> and <see cref="CloseFile"/>.
      /// </para>
      /// </remarks>
      public override void OpenFile(string filename, bool append, Encoding encoding)
      {
        try
        {
          // no lock
          m_stream = CreateStream(filename, append, FileShare.ReadWrite);
        }
        catch (Exception e1)
        {
          CurrentAppender?.ErrorHandler.Error(
              $"Unable to acquire lock on file {filename}. {e1.Message}"
          );
        }
      }

      /// <summary>
      /// Close the file
      /// </summary>
      /// <remarks>
      /// <para>
      /// Close the file. No further writes will be made.
      /// </para>
      /// </remarks>
      public override void CloseFile()
      {
        if (m_stream is not null)
        {
          CloseStream(m_stream);
          m_stream = null;
        }
      }

      /// <summary>
      /// Acquire the lock on the file
      /// </summary>
      /// <returns>A stream that is ready to be written to.</returns>
      /// <remarks>
      /// <para>
      /// Does nothing. The lock is already taken
      /// </para>
      /// </remarks>
      public override Stream? AcquireLock()
      {
        return m_stream;
      }

      /// <summary>
      /// Release the lock on the file
      /// </summary>
      /// <remarks>
      /// <para>
      /// Does nothing. The lock will be released when the file is closed.
      /// </para>
      /// </remarks>
      public override void ReleaseLock()
      {
        // NOP
      }

      /// <summary>
      /// Initializes all resources used by this locking model.
      /// </summary>
      public override void ActivateOptions()
      {
        // NOP
      }

      /// <summary>
      /// Disposes all resources that were initialized by this locking model.
      /// </summary>
      public override void OnClose()
      {
        // NOP
      }
    }

    /// <summary>
    /// Default locking model (when no locking model was configured)
    /// </summary>
    private static Type defaultLockingModelType = typeof(ExclusiveLock);

    /// <summary>
    /// Specify default locking model
    /// </summary>
    /// <typeparam name="TLockingModel">Type of LockingModel</typeparam>
    public static void SetDefaultLockingModelType<TLockingModel>() where TLockingModel : LockingModelBase
    {
      defaultLockingModelType = typeof(TLockingModel);
    }

    /// <summary>
    /// Default constructor
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default constructor
    /// </para>
    /// </remarks>
    public FileAppender()
    {
    }

    /// <summary>
    /// Construct a new appender using the layout, file and append mode.
    /// </summary>
    /// <param name="layout">the layout to use with this appender</param>
    /// <param name="filename">the full path to the file to write to</param>
    /// <param name="append">flag to indicate if the file should be appended to</param>
    /// <remarks>
    /// <para>
    /// Obsolete constructor.
    /// </para>
    /// </remarks>
    [Obsolete("Instead use the default constructor and set the Layout, File & AppendToFile properties")]
    public FileAppender(ILayout layout, string filename, bool append)
    {
      Layout = layout;
      File = filename;
      AppendToFile = append;
      ActivateOptions();
    }

    /// <summary>
    /// Construct a new appender using the layout and file specified.
    /// The file will be appended to.
    /// </summary>
    /// <param name="layout">the layout to use with this appender</param>
    /// <param name="filename">the full path to the file to write to</param>
    /// <remarks>
    /// <para>
    /// Obsolete constructor.
    /// </para>
    /// </remarks>
    [Obsolete("Instead use the default constructor and set the Layout & File properties")]
    public FileAppender(ILayout layout, string filename)
        : this(layout, filename, true)
    {
    }

    /// <summary>
    /// Gets or sets the path to the file that logging will be written to.
    /// </summary>
    /// <value>
    /// The path to the file that logging will be written to.
    /// </value>
    /// <remarks>
    /// <para>
    /// If the path is relative it is taken as relative from 
    /// the application base directory.
    /// </para>
    /// </remarks>
    public virtual string? File
    {
      get { return m_fileName; }
      set { m_fileName = value; }
    }

    /// <summary>
    /// Gets or sets a flag that indicates whether the file should be
    /// appended to or overwritten.
    /// </summary>
    /// <value>
    /// Indicates whether the file should be appended to or overwritten.
    /// </value>
    /// <remarks>
    /// <para>
    /// If the value is set to false then the file will be overwritten, if 
    /// it is set to true then the file will be appended to.
    /// </para>
    /// The default value is true.
    /// </remarks>
    public bool AppendToFile { get; set; } = true;

    /// <summary>
    /// Gets or sets <see cref="Encoding"/> used to write to the file.
    /// </summary>
    /// <value>
    /// The <see cref="Encoding"/> used to write to the file.
    /// </value>
    /// <remarks>
    /// <para>
    /// The default encoding set is <see cref="System.Text.Encoding.Default"/>
    /// which is the encoding for the system's current ANSI code page.
    /// </para>
    /// </remarks>
    public Encoding Encoding { get; set; } = Encoding.GetEncoding(0);

    /// <summary>
    /// Gets or sets the <see cref="SecurityContext"/> used to write to the file.
    /// </summary>
    /// <value>
    /// The <see cref="SecurityContext"/> used to write to the file.
    /// </value>
    /// <remarks>
    /// <para>
    /// Unless a <see cref="SecurityContext"/> specified here for this appender
    /// the <see cref="SecurityContextProvider.DefaultProvider"/> is queried for the
    /// security context to use. The default behavior is to use the security context
    /// of the current thread.
    /// </para>
    /// </remarks>
    public SecurityContext? SecurityContext { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="FileAppender.LockingModel"/> used to handle locking of the file.
    /// </summary>
    /// <value>
    /// The <see cref="FileAppender.LockingModel"/> used to lock the file.
    /// </value>
    /// <remarks>
    /// <para>
    /// Gets or sets the <see cref="FileAppender.LockingModel"/> used to handle locking of the file.
    /// </para>
    /// <para>
    /// There are three built in locking models, <see cref="FileAppender.ExclusiveLock"/>, <see cref="FileAppender.MinimalLock"/> and <see cref="FileAppender.InterProcessLock"/> .
    /// The first locks the file from the start of logging to the end, the 
    /// second locks only for the minimal amount of time when logging each message
    /// and the last synchronizes processes using a named system-wide Mutex.
    /// </para>
    /// <para>
    /// The default locking model is the <see cref="FileAppender.ExclusiveLock"/>.
    /// </para>
    /// </remarks>
    public LockingModelBase LockingModel { get; set; } = (LockingModelBase)Activator.CreateInstance(defaultLockingModelType);

    /// <summary>
    /// Activate the options on the file appender. 
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
    /// This will cause the file to be opened.
    /// </para>
    /// </remarks>
    public override void ActivateOptions()
    {
      base.ActivateOptions();

      SecurityContext ??= SecurityContextProvider.DefaultProvider.CreateSecurityContext(this);

      LockingModel.CurrentAppender = this;
      LockingModel.ActivateOptions();

      if (m_fileName is not null)
      {
        using (SecurityContext.Impersonate(this))
        {
          m_fileName = ConvertToFullPath(m_fileName.Trim());
        }

        SafeOpenFile(m_fileName, AppendToFile);
      }
      else
      {
        LogLog.Warn(declaringType, "FileAppender: File option not set for appender [" + Name + "].");
        LogLog.Warn(declaringType, "FileAppender: Are you using FileAppender instead of ConsoleAppender?");
      }
    }

    /// <summary>
    /// Closes any previously opened file and calls the parent's <see cref="TextWriterAppender.Reset"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Resets the filename and the file stream.
    /// </para>
    /// </remarks>
    protected override void Reset()
    {
      base.Reset();
      m_fileName = null;
    }

    /// <summary>
    /// Close this appender instance. The underlying stream or writer is also closed.
    /// </summary>
    protected override void OnClose()
    {
      base.OnClose();
      LockingModel.OnClose();
    }

    /// <summary>
    /// Called to initialize the file writer
    /// </summary>
    /// <remarks>
    /// <para>
    /// Will be called for each logged message until the file is
    /// successfully opened.
    /// </para>
    /// </remarks>
    protected override void PrepareWriter()
    {
      if (m_fileName is not null)
      {
        SafeOpenFile(m_fileName, AppendToFile);
      }
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
      if (m_stream is not null && m_stream.AcquireLock())
      {
        try
        {
          base.Append(loggingEvent);
        }
        finally
        {
          m_stream.ReleaseLock();
        }
      }
    }

    /// <summary>
    /// This method is called by the <see cref="M:AppenderSkeleton.DoAppend(LoggingEvent[])"/>
    /// method. 
    /// </summary>
    /// <param name="loggingEvents">The array of events to log.</param>
    /// <remarks>
    /// <para>
    /// Acquires the output file locks once before writing all the events to
    /// the stream.
    /// </para>
    /// </remarks>
    protected override void Append(LoggingEvent[] loggingEvents)
    {
      if (m_stream is not null && m_stream.AcquireLock())
      {
        try
        {
          base.Append(loggingEvents);
        }
        finally
        {
          m_stream.ReleaseLock();
        }
      }
    }

    /// <summary>
    /// Writes a footer as produced by the embedded layout's <see cref="ILayout.Footer"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Writes a footer as produced by the embedded layout's <see cref="ILayout.Footer"/> property.
    /// </para>
    /// </remarks>
    protected override void WriteFooter()
    {
      if (m_stream is not null)
      {
        //WriteFooter can be called even before a file is opened
        m_stream.AcquireLock();
        try
        {
          base.WriteFooter();
        }
        finally
        {
          m_stream.ReleaseLock();
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
    protected override void WriteHeader()
    {
      if (m_stream is not null)
      {
        if (m_stream.AcquireLock())
        {
          try
          {
            base.WriteHeader();
          }
          finally
          {
            m_stream.ReleaseLock();
          }
        }
      }
    }

    /// <summary>
    /// Closes the underlying <see cref="TextWriter"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Closes the underlying <see cref="TextWriter"/>.
    /// </para>
    /// </remarks>
    protected override void CloseWriter()
    {
      if (m_stream is not null)
      {
        m_stream.AcquireLock();
        try
        {
          base.CloseWriter();
        }
        finally
        {
          m_stream.ReleaseLock();
        }
      }
    }

    /// <summary>
    /// Closes the previously opened file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Writes the <see cref="ILayout.Footer"/> to the file and then
    /// closes the file.
    /// </para>
    /// </remarks>
    protected void CloseFile()
    {
      WriteFooterAndCloseWriter();
    }

    /// <summary>
    /// Sets and <i>opens</i> the file where the log output will go. The specified file must be writable.
    /// </summary>
    /// <param name="fileName">The path to the log file. Must be a fully qualified path.</param>
    /// <param name="append">If true will append to fileName. Otherwise will truncate fileName</param>
    /// <remarks>
    /// <para>
    /// Calls <see cref="OpenFile"/> but guarantees not to throw an exception.
    /// Errors are passed to the <see cref="TextWriterAppender.ErrorHandler"/>.
    /// </para>
    /// </remarks>
    protected virtual void SafeOpenFile(string fileName, bool append)
    {
      try
      {
        OpenFile(fileName, append);
      }
      catch (Exception e)
      {
        ErrorHandler.Error($"OpenFile({fileName},{append}) call failed.", e, ErrorCode.FileOpenFailure);
      }
    }

    /// <summary>
    /// Sets and <i>opens</i> the file where the log output will go. The specified file must be writable.
    /// </summary>
    /// <param name="fileName">The path to the log file. Must be a fully qualified path.</param>
    /// <param name="append">If true will append to fileName. Otherwise will truncate fileName</param>
    /// <remarks>
    /// <para>
    /// If there was already an opened file, then the previous file
    /// is closed first.
    /// </para>
    /// <para>
    /// This method will ensure that the directory structure
    /// for the <paramref name="fileName"/> specified exists.
    /// </para>
    /// </remarks>
    protected virtual void OpenFile(string fileName, bool append)
    {
      if (LogLog.IsErrorEnabled)
      {
        // Internal check that the fileName passed in is a rooted path
        bool isPathRooted;
        using (SecurityContext?.Impersonate(this))
        {
          isPathRooted = Path.IsPathRooted(fileName);
        }

        if (!isPathRooted)
        {
          LogLog.Error(declaringType,
              "INTERNAL ERROR. OpenFile(" + fileName + "): File name is not fully qualified.");
        }
      }

      lock (this)
      {
        Reset();

        LogLog.Debug(declaringType, "Opening file for writing [" + fileName + "] append [" + append + "]");

        // Save these for later, allowing retries if file open fails
        m_fileName = fileName;
        AppendToFile = append;

        LockingModel.CurrentAppender = this;
        LockingModel.OpenFile(fileName, append, Encoding);
        m_stream = new LockingStream(LockingModel);

        if (m_stream is not null)
        {
          m_stream.AcquireLock();
          try
          {
            SetQWForFiles(m_stream);
          }
          finally
          {
            m_stream.ReleaseLock();
          }
        }

        WriteHeader();
      }
    }

    /// <summary>
    /// Sets the quiet writer used for file output
    /// </summary>
    /// <param name="fileStream">the file stream that has been opened for writing</param>
    /// <remarks>
    /// <para>
    /// This implementation of <see cref="M:SetQWForFiles(Stream)"/> creates a <see cref="StreamWriter"/>
    /// over the <paramref name="fileStream"/> and passes it to the 
    /// <see cref="M:SetQWForFiles(TextWriter)"/> method.
    /// </para>
    /// <para>
    /// This method can be overridden by subclasses that want to wrap the
    /// <see cref="Stream"/> in some way, for example to encrypt the output
    /// data using a <c>System.Security.Cryptography.CryptoStream</c>.
    /// </para>
    /// </remarks>
    protected virtual void SetQWForFiles(Stream fileStream)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
      StreamWriter writer = new(fileStream, Encoding);
#pragma warning restore CA2000 // Dispose objects before losing scope
      SetQWForFiles(writer);
    }

    /// <summary>
    /// Sets the quiet writer being used.
    /// </summary>
    /// <param name="writer">the writer over the file stream that has been opened for writing</param>
    /// <remarks>
    /// <para>
    /// This method can be overridden by subclasses that want to
    /// wrap the <see cref="TextWriter"/> in some way.
    /// </para>
    /// </remarks>
    protected virtual void SetQWForFiles(TextWriter writer)
    {
      QuietWriter = new QuietTextWriter(writer, ErrorHandler);
    }

    /// <summary>
    /// Convert a path into a fully qualified path.
    /// </summary>
    /// <param name="path">The path to convert.</param>
    /// <returns>The fully qualified path.</returns>
    /// <remarks>
    /// <para>
    /// Converts the path specified to a fully
    /// qualified path. If the path is relative it is
    /// taken as relative from the application base 
    /// directory.
    /// </para>
    /// </remarks>
    protected static string ConvertToFullPath(string path)
    {
      return SystemInfo.ConvertToFullPath(path);
    }

    /// <summary>
    /// The name of the log file.
    /// </summary>
    private string? m_fileName;

    /// <summary>
    /// The stream to log to. Has added locking semantics
    /// </summary>
    private LockingStream? m_stream;

    /// <summary>
    /// The fully qualified type of the FileAppender class.
    /// </summary>
    /// <remarks>
    /// Used by the internal logger to record the Type of the
    /// log message.
    /// </remarks>
    private static readonly Type declaringType = typeof(FileAppender);
  }
}