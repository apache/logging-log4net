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
using System.Text;

using log4net.Util;
using log4net.Layout;
using log4net.Core;

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
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	/// <author>Rodrigo B. de Oliveira</author>
	/// <author>Douglas de la Torre</author>
	public class FileAppender : TextWriterAppender
	{
		#region Public Instance Constructors

		/// <summary>
		/// Default constructor
		/// </summary>
		public FileAppender()
		{
		}

		/// <summary>
		/// Construct a new appender using the layout, file and append mode.
		/// </summary>
		/// <param name="layout">the layout to use with this appender</param>
		/// <param name="filename">the full path to the file to write to</param>
		/// <param name="append">flag to indicate if the file should be appended to</param>
		[Obsolete("Instead use the default constructor and set the Layout, File, & AppendToFile properties")]
		public FileAppender(ILayout layout, string filename, bool append) 
		{
			Layout = layout;
			SafeOpenFile(filename, append);
		}

		/// <summary>
		/// Construct a new appender using the layout and file specified.
		/// The file will be appended to.
		/// </summary>
		/// <param name="layout">the layout to use with this appender</param>
		/// <param name="filename">the full path to the file to write to</param>
		[Obsolete("Instead use the default constructor and set the Layout & File properties")]
		public FileAppender(ILayout layout, string filename) : this(layout, filename, true)
		{
		}

		#endregion Public Instance Constructors

		#region Public Instance Properties

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
		virtual public string File
		{
			get { return m_fileName; }
			set { m_fileName = ConvertToFullPath(value.Trim()); }
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
		public bool AppendToFile
		{
			get { return m_appendToFile; }
			set { m_appendToFile = value; }
		}

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
		public Encoding Encoding
		{
			get { return m_encoding; }
			set { m_encoding = value; }
		}

		#endregion Public Instance Properties

		#region Override implementation of AppenderSkeleton

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
		override public void ActivateOptions() 
		{	
			base.ActivateOptions();
			if (m_fileName != null) 
			{
				SafeOpenFile(m_fileName, m_appendToFile);
			} 
			else 
			{
				LogLog.Warn("FileAppender: File option not set for appender ["+Name+"].");
				LogLog.Warn("FileAppender: Are you using FileAppender instead of ConsoleAppender?");
			}
		}

		#endregion Override implementation of AppenderSkeleton

		#region Override implementation of TextWriterAppender

		/// <summary>
		/// Closes any previously opened file and calls the parent's <see cref="TextWriterAppender.Reset"/>.
		/// </summary>
		override protected void Reset() 
		{
			base.Reset();
			m_fileName = null;
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
 		override protected void PrepareWriter()
 		{
			SafeOpenFile(m_fileName, m_appendToFile);
 		}

		#endregion Override implementation of TextWriterAppender

		#region Public Instance Methods

		/// <summary>
		/// Closes the previously opened file.
		/// </summary>
		protected void CloseFile() 
		{
			WriteFooterAndCloseWriter();
		}

		#endregion Public Instance Methods

		#region Protected Instance Methods

		/// <summary>
		/// Sets and <i>opens</i> the file where the log output will go. The specified file must be writable.
		/// </summary>
		/// <param name="fileName">The path to the log file</param>
		/// <param name="append">If true will append to fileName. Otherwise will truncate fileName</param>
		/// <remarks>
		/// <para>
		/// Calls <see cref="OpenFile"/> but guarantees not to throw an exception.
		/// Errors are passed to the <see cref="TextWriterAppender.ErrorHandler"/>.
		/// </para>
		/// </remarks>
		virtual protected void SafeOpenFile(string fileName, bool append)
		{
			try 
			{
				OpenFile(fileName, append);
			}
			catch(Exception e) 
			{
				ErrorHandler.Error("OpenFile("+fileName+","+append+") call failed.", e, ErrorCode.FileOpenFailure);
			}
		}

		/// <summary>
		/// Sets and <i>opens</i> the file where the log output will go. The specified file must be writable.
		/// </summary>
		/// <param name="fileName">The path to the log file</param>
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
		virtual protected void OpenFile(string fileName, bool append)
		{
			lock(this)
			{
				Reset();

				LogLog.Debug("FileAppender: Opening file for writing ["+fileName+"] append ["+append+"]");

				// Save these for later, allowing retries if file open fails
				m_fileName = fileName;
				m_appendToFile = append;

				// Ensure that the directory structure exists
				string directoryFullName = Path.GetDirectoryName(fileName);

				// Only create the directory if it does not exist
				// doing this check here resolves some permissions failures
				if (!Directory.Exists(directoryFullName))
				{
					Directory.CreateDirectory(directoryFullName);
				}

				SetQWForFiles(new StreamWriter(fileName, append, m_encoding));

				WriteHeader();
			}
		}

		/// <summary>
		/// Sets the quiet writer being used.
		/// </summary>
		/// <remarks>
		/// This method can be overridden by sub classes.
		/// </remarks>
		/// <param name="writer">the writer to set</param>
		virtual protected void SetQWForFiles(TextWriter writer) 
		{
			QuietWriter = new QuietTextWriter(writer, ErrorHandler);
		}


		#endregion Protected Instance Methods

		#region Protected Static Methods

		/// <summary>
		/// Convert a path into a fully qualified path.
		/// </summary>
		/// <param name="path">The path to convert.</param>
		/// <remarks>
		/// <para>
		/// Converts the path specified to a fully
		/// qualified path. If the path is relative it is
		/// taken as relative from the application base 
		/// directory.
		/// </para>
		/// </remarks>
		/// <returns>The fully qualified path.</returns>
		protected static string ConvertToFullPath(string path)
		{
			if (path == null)
			{
				throw new ArgumentNullException("path");
			}

			string applicationBaseDirectory = SystemInfo.ApplicationBaseDirectory;
			if (applicationBaseDirectory != null)
			{
				// Note that Path.Combine will return the second path if it is rooted
				return Path.GetFullPath(Path.Combine(applicationBaseDirectory, path));
			}
			return Path.GetFullPath(path);
		}

		#endregion Protected Static Methods

		#region Private Instance Fields

		/// <summary>
		/// Flag to indicate if we should append to the file
		/// or overwrite the file. The default is to append.
		/// </summary>
		private bool m_appendToFile = true;

		/// <summary>
		/// The name of the log file.
		/// </summary>
		private string m_fileName = null;

		/// <summary>
		/// The encoding to use for the file stream.
		/// </summary>
		private Encoding m_encoding = Encoding.Default;

		#endregion Private Instance Fields
	}
}
