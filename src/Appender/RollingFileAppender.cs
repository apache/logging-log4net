#region Copyright
//
// This framework is based on log4j see http://jakarta.apache.org/log4j
// Copyright (C) The Apache Software Foundation. All rights reserved.
//
// This software is published under the terms of the Apache Software
// License version 1.1, a copy of which has been included with this
// distribution in the LICENSE.txt file.
// 
#endregion

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

using log4net.Util;
using log4net.Layout;
using log4net.Core;

#if NUNIT_TESTS
using NUnit.Framework;
using log4net.Repository;
using log4net.Repository.Hierarchy;
#endif // NUNIT_TESTS

namespace log4net.Appender
{
	/// <summary>
	/// Appender that rolls log files based on size or date or both.
	/// </summary>
	/// <remarks>
	/// <para>
	/// RollingFileAppender can function as either or and do both
	/// at the same time (making size based rolling files until a data/time 
	/// boundary is crossed at which time it rolls all of those files
	/// based on the setting for <see cref="RollingStyle"/>.
	/// </para>
	/// <para>
	/// A of few additional optional features have been added:<br/>
	/// -- Attach date pattern for current log file <see cref="StaticLogFileName"/><br/>
	/// -- Backup number increments for newer files <see cref="CountDirection"/><br/>
	/// -- Infinite number of backups by file size <see cref="MaxSizeRollBackups"/>
	/// </para>
	/// <para>
	/// A few notes and warnings:  For large or infinite number of backups
	/// countDirection &gt; 0 is highly recommended, with staticLogFileName = false if
	/// time based rolling is also used -- this will reduce the number of file renamings
	/// to few or none.  Changing staticLogFileName or countDirection without clearing
	/// the directory could have nasty side effects.  If Date/Time based rolling
	/// is enabled, CompositeRollingAppender will attempt to roll existing files
	/// in the directory without a date/time tag based on the last modified date
	/// of the base log files last modification.
	/// </para>
	/// <para>
	/// A maximum number of backups based on date/time boundaries would be nice
	/// but is not yet implemented.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	/// <author>Aspi Havewala</author>
	/// <author>Douglas de la Torre</author>
	/// <author>Edward Smit</author>
	public class RollingFileAppender : FileAppender
	{
		#region Public Enums

		/// <summary>
		/// Style of rolling to use
		/// </summary>
		public enum RollingMode
		{
			/// <summary>
			/// Roll files based only on the size of the file
			/// </summary>
			Size		= 1,

			/// <summary>
			/// Roll files based only on the date
			/// </summary>
			Date		= 2,

			/// <summary>
			/// Roll files based on both the size and date of the file
			/// </summary>
			Composite	= 3
		}

		#endregion

		#region Protected Enums

		/// <summary>
		/// The code assumes that the following 'time' constants are in a increasing sequence.
		/// </summary>
		protected enum RollPoint
		{
			/// <summary>
			/// Roll the log not based on the date
			/// </summary>
			TopOfTrouble	=-1,

			/// <summary>
			/// Roll the log for each minute
			/// </summary>
			TopOfMinute		= 0,

			/// <summary>
			/// Roll the log for each hour
			/// </summary>
			TopOfHour		= 1,

			/// <summary>
			/// Roll the log twice a day (midday and midnight)
			/// </summary>
			HalfDay			= 2,

			/// <summary>
			/// Roll the log each day (midnight)
			/// </summary>
			TopOfDay		= 3,

			/// <summary>
			/// Roll the log each week
			/// </summary>
			TopOfWeek		= 4,

			/// <summary>
			/// Roll the log each month
			/// </summary>
			TopOfMonth		= 5
		}

		#endregion Protected Enums

		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="RollingFileAppender" /> class.
		/// </summary>
		public RollingFileAppender() 
		{
			m_dateTime = new DefaultDateTime();
		}

		#endregion Public Instance Constructors

		#region Public Instance Properties

		/// <summary>
		/// Gets or sets the datepattern to be used for generating file names
		/// when rolling over on date.
		/// </summary>
		/// <value>
		/// The datepattern to be used for generating file names when rolling 
		/// over on date.
		/// </value>
		/// <remarks>
		/// <para>
		/// Takes a string in the same format as expected by 
		/// <see cref="log4net.DateFormatter.SimpleDateFormatter" />.
		/// </para>
		/// <para>
		/// This property determines the rollover schedule when rolling over
		/// on date.
		/// </para>
		/// </remarks>
		public string DatePattern
		{
			get { return m_datePattern; }
			set { m_datePattern = value; }
		}
  
		/// <summary>
		/// Gets or sets the maximum number of backup files that are kept before
		/// the oldest is erased.
		/// </summary>
		/// <value>
		/// The maximum number of backup files that are kept before the oldest is
		/// erased.
		/// </value>
		/// <remarks>
		/// <para>
		/// If set to zero, then there will be no backup files and the log file 
		/// will be truncated when it reaches <see cref="MaxFileSize"/>.  
		/// </para>
		/// <para>
		/// If a negative number is supplied then no deletions will be made.  Note 
		/// that this could result in very slow performance as a large number of 
		/// files are rolled over unless <see cref="CountDirection"/> is used.
		/// </para>
		/// <para>
		/// The maximum applies to <b>each</b> time based group of files and 
		/// <b>not</b> the total.
		/// </para>
		/// <para>
		/// Using a daily roll the maximum total files would be 
		/// <c>(#days run) * (maxSizeRollBackups)</c>.
		/// </para>
		/// </remarks>
		public int MaxSizeRollBackups
		{
			get { return m_maxSizeRollBackups; }
			set { m_maxSizeRollBackups = value; }
		}
  
		/// <summary>
		/// Gets or sets the maximum size that the output file is allowed to reach
		/// before being rolled over to backup files.
		/// </summary>
		/// <value>
		/// The maximum size that the output file is allowed to reach before being 
		/// rolled over to backup files.
		/// </value>
		/// <remarks>
		/// <para>
		/// This property is equivalent to <see cref="MaximumFileSize"/> except
		/// that it is required for differentiating the setter taking a
		/// <see cref="long"/> argument from the setter taking a <see cref="string"/> 
		/// argument.
		/// </para>
		/// <para>
		/// The default maximum filesize is 10MB.
		/// </para>
		/// </remarks>
		public long MaxFileSize
		{
			get { return m_maxFileSize; }
			set { m_maxFileSize = value; }
		}
  
		/// <summary>
		/// Gets or sets the maximum size that the output file is allowed to reach
		/// before being rolled over to backup files.
		/// </summary>
		/// <value>
		/// The maximum size that the output file is allowed to reach before being 
		/// rolled over to backup files.
		/// </value>
		/// <remarks>
		/// <para>
		/// This property allows you to specify the maximum size with the
		/// suffixes "KB", "MB" or "GB" so that the size is interpreted being 
		/// expressed respectively in kilobytes, megabytes or gigabytes. 
		/// </para>
		/// <para>
		/// For example, the value "10KB" will be interpreted as 10240.
		/// </para>
		/// <para>
		/// The default maximum filesize is 10MB.
		/// </para>
		/// </remarks>
		public string MaximumFileSize
		{
			get { return m_maxFileSize.ToString(NumberFormatInfo.InvariantInfo); }
			set { m_maxFileSize = OptionConverter.ToFileSize(value, m_maxFileSize + 1); }
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
		override public string File
		{
			get { return base.File; }
			set 
			{ 
				base.File = value; 
				m_baseFileName = base.File;
			}
		}

		/// <summary>
		/// Gets or sets the rolling file count direction. 
		/// </summary>
		/// <value>
		/// The rolling file count direction.
		/// </value>
		/// <remarks>
		/// <para>
		/// Indicates if the current file is the lowest numbered file or the
		/// highest numbered file.
		/// </para>
		/// <para>
		/// By default newer files have lower numbers (<see cref="CountDirection" /> &lt; 0),
		/// ie. log.1 is most recent, log.5 is the 5th backup, etc...
		/// </para>
		/// <para>
		/// <see cref="CountDirection" /> &gt; 0 does the opposite ie.
		/// log.1 is the first backup made, log.5 is the 5th backup made, etc.
		/// For infinite backups use <see cref="CountDirection" /> &gt; 0 to reduce 
		/// rollover costs.
		/// </para>
		/// <para>The default file count direction is -1.</para>
		/// </remarks>
		public int CountDirection
		{
			get { return m_countDirection; }
			set { m_countDirection = value; }
		}
  
		/// <summary>
		/// Gets or sets the rolling style.
		/// </summary>
		/// <value>The rolling style.</value>
		/// <remarks>
		/// The default rolling style is <see cref="RollingMode.Composite" />.
		/// </remarks>
		public RollingMode RollingStyle
		{
			get { return m_rollingStyle; }
			set
			{
				m_rollingStyle = value;
				switch (m_rollingStyle) 
				{
					case RollingMode.Size:
						m_rollDate = false;
						m_rollSize = true;
						break;

					case RollingMode.Date:
						m_rollDate = true;
						m_rollSize = false;
						break;

					case RollingMode.Composite:
						m_rollDate = true;
						m_rollSize = true;
						break;	  
				}
			}
		}
  
		/// <summary>
		/// Gets or sets a value indicting whether to always log to
		/// the same file.
		/// </summary>
		/// <value>
		/// <c>true</c> if always should be logged to the same file, otherwise <c>false</c>.
		/// </value>
		/// <remarks>
		/// <para>
		/// By default file.log is always the current file.  Optionally
		/// file.log.yyyy-mm-dd for current formatted datePattern can by the currently
		/// logging file (or file.log.curSizeRollBackup or even
		/// file.log.yyyy-mm-dd.curSizeRollBackup).
		/// </para>
		/// <para>
		/// This will make time based rollovers with a large number of backups 
		/// much faster -- it won't have to
		/// rename all the backups!
		/// </para>
		/// </remarks>
		public bool StaticLogFileName
		{
			get { return m_staticLogFileName; }
			set { m_staticLogFileName = value; }
		}

		#endregion Public Instance Properties

		#region Override implementation of FileAppender 
  
		/// <summary>
		/// Sets the quiet writer being used.
		/// </summary>
		/// <remarks>
		/// This method can be overridden by sub classes.
		/// </remarks>
		/// <param name="writer">the writer to set</param>
		override protected void SetQWForFiles(TextWriter writer) 
		{
			QuietWriter = new CountingQuietTextWriter(writer, ErrorHandler);
		}

		/// <summary>
		/// Handles append time behaviour for CompositeRollingAppender.  This checks
		/// if a roll over either by date (checked first) or time (checked second)
		/// is need and then appends to the file last.
		/// </summary>
		/// <param name="loggingEvent"></param>
		override protected void Append(LoggingEvent loggingEvent) 
		{
			if (m_rollDate) 
			{
				DateTime n = m_dateTime.Now;
				if (n >= m_nextCheck) 
				{
					m_now = n;
					m_nextCheck = NextCheckDate(m_now);
	
					RollOverTime();
				}
			}
	
			if (m_rollSize) 
			{
				if ((File != null) && ((CountingQuietTextWriter)QuietWriter).Count >= m_maxFileSize) 
				{
					RollOverSize();
				}
			}

			base.Append(loggingEvent);
		}
  
  
		/// <summary>
		/// Creates and opens the file for logging.  If <see cref="StaticLogFileName"/>
		/// is false then the fully qualified name is determined and used.
		/// </summary>
		/// <param name="fileName">the name of the file to open</param>
		/// <param name="append">true to append to existing file</param>
		/// <remarks>
		/// <para>This method will ensure that the directory structure
		/// for the <paramref name="fileName"/> specified exists.</para>
		/// </remarks>
		override protected void OpenFile(string fileName, bool append)
		{
			lock(this)
			{
				if (!m_staticLogFileName) 
				{
					m_scheduledFilename = fileName = fileName.Trim();

					if (m_rollDate)
					{
						m_scheduledFilename = fileName = fileName + m_now.ToString(m_datePattern, System.Globalization.DateTimeFormatInfo.InvariantInfo);
					}

					if (m_countDirection > 0) 
					{
						m_scheduledFilename = fileName = fileName + '.' + (++m_curSizeRollBackups);
					}
				}
	
				// Calculate the current size of the file
				long currentCount = 0;
				if (append) 
				{
					FileInfo fi = new FileInfo(fileName);
					if (fi.Exists)
					{
						currentCount = fi.Length;
					}
				}

				// Open the file (call the base class to do it)
				base.OpenFile(fileName, append);

				// Set the file size onto the counting writer
				((CountingQuietTextWriter)QuietWriter).Count = currentCount;
			}
		}

		#endregion

		#region Initialise Options

		/// <summary>
		///	Determines curSizeRollBackups (only within the current rollpoint)
		/// </summary>
		private void DetermineCurSizeRollBackups()
		{
			m_curSizeRollBackups = 0;
	
			string sName = null;
			if (m_staticLogFileName || !m_rollDate) 
			{
				sName = m_baseFileName;
			} 
			else 
			{
				sName = m_scheduledFilename;
			}

			FileInfo fileInfo = new FileInfo(sName);
			if (null != fileInfo)
			{
				ArrayList arrFiles = GetExistingFiles(fileInfo.FullName);
				InitializeRollBackups((new FileInfo(m_baseFileName)).Name, arrFiles);

			}

			LogLog.Debug("RollingFileAppender: curSizeRollBackups starts at ["+m_curSizeRollBackups+"]");
		}

		/// <summary>
		/// Generates a wildcard pattern that can be used to find all files
		/// that are similar to the base file name.
		/// </summary>
		/// <param name="baseFileName"></param>
		/// <returns></returns>
		private static string GetWildcardPatternForFile(string baseFileName)
		{
			return baseFileName + "*";
		}

		/// <summary>
		/// Builds a list of filenames for all files matching the base filename plus a file
		/// pattern.
		/// </summary>
		/// <param name="baseFilePath"></param>
		/// <returns></returns>
		private static ArrayList GetExistingFiles(string baseFilePath)
		{
			ArrayList alFiles = new ArrayList();

			FileInfo fileInfo = new FileInfo(baseFilePath);
			DirectoryInfo dirInfo = fileInfo.Directory;
			LogLog.Debug("RollingFileAppender: Searching for existing files in ["+dirInfo+"]");

			if (dirInfo.Exists)
			{
				string baseFileName = fileInfo.Name;

				FileInfo[] files = dirInfo.GetFiles(GetWildcardPatternForFile(baseFileName));
	
				if (files != null)
				{
					for (int i = 0; i < files.Length; i++) 
					{
						string curFileName = files[i].Name;
						if (curFileName.StartsWith(baseFileName))
						{
							alFiles.Add(curFileName);
						}
					}
				}
			}
			return alFiles;
		}

		/// <summary>
		/// Initiates a roll over if needed for crossing a date boundary since the last run.
		/// </summary>
		private void RollOverIfDateBoundaryCrossing()
		{
			if (m_staticLogFileName && m_rollDate) 
			{
				FileInfo old = new FileInfo(m_baseFileName);
				if (old.Exists) 
				{
					DateTime last = old.LastWriteTime;
					LogLog.Debug("RollingFileAppender: ["+last.ToString(m_datePattern,System.Globalization.DateTimeFormatInfo.InvariantInfo)+"] vs. ["+m_now.ToString(m_datePattern,System.Globalization.DateTimeFormatInfo.InvariantInfo)+"]");

					if (!(last.ToString(m_datePattern,System.Globalization.DateTimeFormatInfo.InvariantInfo).Equals(m_now.ToString(m_datePattern, System.Globalization.DateTimeFormatInfo.InvariantInfo)))) 
					{
						m_scheduledFilename = m_baseFileName + last.ToString(m_datePattern, System.Globalization.DateTimeFormatInfo.InvariantInfo);
						LogLog.Debug("RollingFileAppender: Initial roll over to ["+m_scheduledFilename+"]");
						RollOverTime();
						LogLog.Debug("RollingFileAppender: curSizeRollBackups after rollOver at ["+m_curSizeRollBackups+"]");
					}
				}
			}
		}

		/// <summary>
		/// <para>Initializes based on existing conditions at time of <see cref="ActivateOptions"/>.
		/// The following is done:</para>
		///		A) determine curSizeRollBackups (only within the current rollpoint)
		///		B) initiates a roll over if needed for crossing a date boundary since the last run.
		/// </summary>
		protected void ExistingInit() 
		{
			DetermineCurSizeRollBackups();
			RollOverIfDateBoundaryCrossing();
		}

		/// <summary>
		/// Does the work of bumping the 'current' file counter higher
		/// to the highest count when an incremental file name is seen.
		/// The highest count is either the first file (when count direction
		/// is greater than 0 ) or the last file (when count direction less than 0).
		/// In either case, we want to know the highest count that is present.
		/// </summary>
		/// <param name="baseFile"></param>
		/// <param name="curFileName"></param>
		private void InitializeFromOneFile(string baseFile, string curFileName)
		{
			if (! curFileName.StartsWith(baseFile) )
			{
				// This is not a log file, so ignore
				return;
			}
			if (curFileName.Equals(baseFile)) 
			{
				// Base log file is not an incremented logfile (.1 or .2, etc)
				return;
			}
	
			int index = curFileName.LastIndexOf(".");
			if (-1 == index) 
			{
				// This is not an incremented logfile (.1 or .2)
				return;
			}
	
			if (m_staticLogFileName) 
			{
				int endLength = curFileName.Length - index;
				if (baseFile.Length + endLength != curFileName.Length) 
				{
					//file is probably scheduledFilename + .x so I don't care
					return;
				}
			}
	
			// Only look for files in the current rollpoint
			if (m_rollDate)
			{
				if (! curFileName.StartsWith(baseFile + m_dateTime.Now.ToString(m_datePattern, System.Globalization.DateTimeFormatInfo.InvariantInfo)))
				{
					LogLog.Debug("RollingFileAppender: Ignoring file ["+curFileName+"] because it is from a different date period");
					return;
				}
			}

			try 
			{
				// Bump the counter up to the highest count seen so far
				int backup = int.Parse(curFileName.Substring(index + 1), System.Globalization.NumberFormatInfo.InvariantInfo);
				if (backup > m_curSizeRollBackups)
				{
					if (0 == m_maxSizeRollBackups)
					{
						// Stay at zero when zero backups are desired
					}
					else if (-1 == m_maxSizeRollBackups)
					{
						// Infinite backups, so go as high as the highest value
						m_curSizeRollBackups = backup;
					}
					else
					{
						// Backups limited to a finite number
						if (m_countDirection > 0) 
						{
							// Go with the highest file when counting up
							m_curSizeRollBackups = backup;
						} 
						else
						{
							// Clip to the limit when counting down
							if (backup <= m_maxSizeRollBackups)
							{
								m_curSizeRollBackups = backup;
							}
						}
					}
					LogLog.Debug("RollingFileAppender: File name ["+curFileName+"] moves current count to ["+m_curSizeRollBackups+"]");
				}
			} 
			catch (Exception /*e*/) 
			{
				//this happens when file.log -> file.log.yyyy-mm-dd which is normal
				//when staticLogFileName == false
				LogLog.Debug("RollingFileAppender: Encountered a backup file not ending in .x ["+curFileName+"]");
			}
		}

		/// <summary>
		/// Takes a list of files and a base file name, and looks for 
		/// 'incremented' versions of the base file.  Bumps the max
		/// count up to the highest count seen.
		/// </summary>
		/// <param name="baseFile"></param>
		/// <param name="arrayFiles"></param>
		private void InitializeRollBackups(string baseFile, ArrayList arrayFiles)
		{
			if (null != arrayFiles)
			{
				string baseFileLower = baseFile.ToLower(System.Globalization.CultureInfo.InvariantCulture);

				foreach(string curFileName in arrayFiles)
				{
					InitializeFromOneFile(baseFileLower, curFileName.ToLower(System.Globalization.CultureInfo.InvariantCulture));
				}
			}
		}

		/// <summary>
		/// Calculates the RollPoint for the m_datePattern supplied.
		/// </summary>
		/// <returns>The RollPoint that is most accurate for the date pattern supplied</returns>
		/// <remarks>
		/// Essentially the date pattern is examined to determine what the
		/// most suitable roll point is. The roll point chosen is the roll point
		/// with the smallest period that can be detected using the date pattern
		/// supplied. i.e. if the date pattern only outputs the year, month, day 
		/// and hour then the smallest roll point that can be detected would be
		/// and hourly roll point as minutes could not be detected.
		/// </remarks>
		private RollPoint ComputeCheckPeriod() 
		{
			if (m_datePattern != null) 
			{
				// set date to 1970-01-01 00:00:00Z this is UniversalSortableDateTimePattern 
				// (based on ISO 8601) using universal time. This date is used for reference
				// purposes to calculate the resolution of the date pattern.
				DateTime epoch = DateTime.Parse("1970-01-01 00:00:00Z", System.Globalization.DateTimeFormatInfo.InvariantInfo);

				// Get string representation of base line date
				string r0 = epoch.ToString(m_datePattern, System.Globalization.DateTimeFormatInfo.InvariantInfo);

				// Check each type of rolling mode starting with the smallest increment.
				for(int i = (int)RollPoint.TopOfMinute; i <= (int)RollPoint.TopOfMonth; i++) 
				{
					// Get string representation of next pattern
					string r1 = NextCheckDate(epoch, (RollPoint)i).ToString(m_datePattern, System.Globalization.DateTimeFormatInfo.InvariantInfo);

					LogLog.Debug("RollingFileAppender: Type = ["+i+"], r0 = ["+r0+"], r1 = ["+r1+"]");

					// Check if the string representations are different
					if (r0 != null && r1 != null && !r0.Equals(r1)) 
					{
						// Found highest precision roll point
						return (RollPoint)i;
					}
				}
			}
			return RollPoint.TopOfTrouble; // Deliberately head for trouble...
		}

		/// <summary>
		/// Sets initial conditions including date/time roll over information, first check,
		/// scheduledFilename, and calls <see cref="ExistingInit"/> to initialize
		/// the current number of backups.
		/// </summary>
		override public void ActivateOptions() 
		{
			if (m_rollDate && m_datePattern != null) 
			{
				m_now = m_dateTime.Now;
				m_rollPoint = ComputeCheckPeriod();

				// next line added as this removes the name check in rollOver
				m_nextCheck = NextCheckDate(m_now);
			} 
			else 
			{
				if (m_rollDate)
				{
					ErrorHandler.Error("Either DatePattern or rollingStyle options are not set for ["+Name+"].");
				}
			}
	
			if (m_rollDate && File != null && m_scheduledFilename == null)
			{
				m_scheduledFilename = File + m_now.ToString(m_datePattern, System.Globalization.DateTimeFormatInfo.InvariantInfo);
			}

			ExistingInit();
	
			base.ActivateOptions();
		}

		#endregion
  
		#region Roll File

		/// <summary>
		/// Rollover the file(s) to date/time tagged file(s).
		/// Opens the new file (through setFile) and resets curSizeRollBackups.
		/// </summary>
		protected void RollOverTime() 
		{
			if (m_staticLogFileName) 
			{
				/* Compute filename, but only if datePattern is specified */
				if (m_datePattern == null) 
				{
					ErrorHandler.Error("Missing DatePattern option in rollOver().");
					return;
				}
	  
				//is the new file name equivalent to the 'current' one
				//something has gone wrong if we hit this -- we should only
				//roll over if the new file will be different from the old
				string dateFormat = m_now.ToString(m_datePattern, System.Globalization.DateTimeFormatInfo.InvariantInfo);
				if (m_scheduledFilename.Equals(File + dateFormat)) 
				{
					ErrorHandler.Error("Compare " + m_scheduledFilename + " : " + File + dateFormat);
					return;
				}
	  
				// close current file, and rename it to datedFilename
				this.CloseFile();
	  
				//we may have to roll over a large number of backups here
				string from, to;
				for (int i = 1; i <= m_curSizeRollBackups; i++) 
				{
					from = File + '.' + i;
					to = m_scheduledFilename + '.' + i;
					RollFile(from, to);
				}
	  
				RollFile(File, m_scheduledFilename);
			}
	
			try 
			{
				//We've cleared out the old date and are ready for the new
				m_curSizeRollBackups = 0; 
	  
				//new scheduled name
				m_scheduledFilename = File + m_now.ToString(m_datePattern, System.Globalization.DateTimeFormatInfo.InvariantInfo);

				// This will also close the file. This is OK since multiple
				// close operations are safe.
				this.OpenFile(m_baseFileName, false);
			}
			catch(Exception e) 
			{
				ErrorHandler.Error("setFile(" + File + ", false) call failed.", e, ErrorCode.FileOpenFailure);
			}
		}
  
		/// <summary>
		/// Renames file <paramref name="fromFile"/> to file <paramref name="toFile"/>.  It
		/// also checks for existence of target file and deletes if it does.
		/// </summary>
		/// <param name="fromFile">Name of existing file to roll.</param>
		/// <param name="toFile">New name for file.</param>
		protected void RollFile(string fromFile, string toFile) 
		{
			FileInfo target = new FileInfo(toFile);
			if (target.Exists) 
			{
				LogLog.Debug("RollingFileAppender: Deleting existing target file ["+target+"]");
				target.Delete();
			}
	
			FileInfo file = new FileInfo(fromFile);
			if (file.Exists)
			{
				// We may not have permission to move the file, or the file may be locked
				try
				{
					file.MoveTo(toFile);
					LogLog.Debug("RollingFileAppender: Moved [" + fromFile + "] -> [" + toFile + "]");
				}
				catch(Exception ex)
				{
					ErrorHandler.Error("Exception while rolling file [" + fromFile + "] -> [" + toFile + "]", ex, ErrorCode.GenericFailure);
				}
			}
			else
			{
				LogLog.Warn("RollingFileAppender: Cannot RollFile [" + fromFile + "] -> [" + toFile + "]. Source does not exist");
			}
		}
  
		/// <summary>
		/// Deletes the specified file if it exists.
		/// </summary>
		/// <param name="fileName">The file to delete.</param>
		protected void DeleteFile(string fileName) 
		{
			FileInfo file = new FileInfo(fileName);
			if (file.Exists) 
			{
				// We may not have permission to delete the file, or the file may be locked
				try
				{
					file.Delete();
					LogLog.Debug("RollingFileAppender: Deleted file [" + fileName + "]");
				}
				catch(Exception ex)
				{
					ErrorHandler.Error("Exception while deleting file [" + fileName + "]", ex, ErrorCode.GenericFailure);
				}
			}
		}
  
		/// <summary>
		/// Implements roll overs base on file size.
		/// </summary>
		/// <remarks>
		/// <para>If the maximum number of size based backups is reached
		/// (<c>curSizeRollBackups == maxSizeRollBackups</c>) then the oldest
		/// file is deleted -- it's index determined by the sign of countDirection.
		/// If <c>countDirection</c> &lt; 0, then files
		/// {<c>File.1</c>, ..., <c>File.curSizeRollBackups -1</c>}
		/// are renamed to {<c>File.2</c>, ...,
		/// <c>File.curSizeRollBackups</c>}.	 Moreover, <c>File</c> is
		/// renamed <c>File.1</c> and closed.</para>
		/// 
		/// A new file is created to receive further log output.
		/// 
		/// <para>If <c>maxSizeRollBackups</c> is equal to zero, then the
		/// <c>File</c> is truncated with no backup files created.</para>
		/// 
		/// <para>If <c>maxSizeRollBackups</c> &lt; 0, then <c>File</c> is
		/// renamed if needed and no files are deleted.</para>
		/// </remarks>
		protected void RollOverSize() 
		{
			this.CloseFile(); // keep windows happy.
	
			LogLog.Debug("RollingFileAppender: rolling over count ["+((CountingQuietTextWriter)QuietWriter).Count+"]");
			LogLog.Debug("RollingFileAppender: maxSizeRollBackups ["+m_maxSizeRollBackups+"]");
			LogLog.Debug("RollingFileAppender: curSizeRollBackups ["+m_curSizeRollBackups+"]");
			LogLog.Debug("RollingFileAppender: countDirection ["+m_countDirection+"]");
	
			// If maxBackups <= 0, then there is no file renaming to be done.
			if (m_maxSizeRollBackups != 0) 
			{
				if (m_countDirection < 0) 
				{
					// Delete the oldest file, to keep Windows happy.
					if (m_curSizeRollBackups == m_maxSizeRollBackups) 
					{
						DeleteFile(File + '.' + m_maxSizeRollBackups);
						m_curSizeRollBackups--;
					}
	
					// Map {(maxBackupIndex - 1), ..., 2, 1} to {maxBackupIndex, ..., 3, 2}
					for (int i = m_curSizeRollBackups; i >= 1; i--) 
					{
						RollFile((File + "." + i), (File + '.' + (i + 1)));
					}
	
					m_curSizeRollBackups++;

					// Rename fileName to fileName.1
					RollFile(File, File + ".1");
				} 
				else 
				{	//countDirection > 0
					if (m_curSizeRollBackups >= m_maxSizeRollBackups && m_maxSizeRollBackups > 0) 
					{
						//delete the first and keep counting up.
						int oldestFileIndex = m_curSizeRollBackups - m_maxSizeRollBackups + 1;
						DeleteFile(File + '.' + oldestFileIndex);
					}
	
					if (m_staticLogFileName) 
					{
						m_curSizeRollBackups++;
						RollFile(File, File + '.' + m_curSizeRollBackups);
					}
				}
			}
	
			try 
			{
				// This will also close the file. This is OK since multiple
				// close operations are safe.
				this.OpenFile(m_baseFileName, false);
			} 
			catch(Exception e) 
			{
				ErrorHandler.Error("OpenFile ["+m_baseFileName+"] call failed.", e);
			}
		}

		#endregion

		#region NextCheckDate

		/// <summary>
		/// Roll on to the next interval after the date passed
		/// </summary>
		/// <param name="currentDateTime">the current date</param>
		/// <returns>the next roll point an interval after the currentDateTime date</returns>
		/// <remarks>
		/// Advances the date to the next roll point after the 
		/// currentDateTime date passed to the method.
		/// </remarks>
		protected DateTime NextCheckDate(DateTime currentDateTime) 
		{
			return NextCheckDate(currentDateTime, m_rollPoint);
		}

		/// <summary>
		/// Roll on to the next interval after the date passed
		/// </summary>
		/// <param name="currentDateTime">the current date</param>
		/// <param name="rollPoint">the type of roll point we are working with</param>
		/// <returns>the next roll point an interval after the currentDateTime date</returns>
		/// <remarks>
		/// Advances the date to the next roll point after the 
		/// currentDateTime date passed to the method.
		/// </remarks>
		protected DateTime NextCheckDate(DateTime currentDateTime, RollPoint rollPoint) 
		{
			// Local variable to work on (this does not look very efficient)
			DateTime current = currentDateTime;

			// Do different things depending on what the type of roll point we are going for is
			switch(rollPoint) 
			{
				case RollPoint.TopOfMinute:
					current = current.AddMilliseconds(-current.Millisecond);
					current = current.AddSeconds(-current.Second);
					current = current.AddMinutes(1);
					break;

				case RollPoint.TopOfHour:
					current = current.AddMilliseconds(-current.Millisecond);
					current = current.AddSeconds(-current.Second);
					current = current.AddMinutes(-current.Minute);
					current = current.AddHours(1);
					break;

				case RollPoint.HalfDay:
					current = current.AddMilliseconds(-current.Millisecond);
					current = current.AddSeconds(-current.Second);
					current = current.AddMinutes(-current.Minute);

					if (current.Hour < 12) 
					{
						current = current.AddHours(12 - current.Hour);
					} 
					else 
					{
						current = current.AddHours(-current.Hour);
						current = current.AddDays(1);
					}
					break;

				case RollPoint.TopOfDay:
					current = current.AddMilliseconds(-current.Millisecond);
					current = current.AddSeconds(-current.Second);
					current = current.AddMinutes(-current.Minute);
					current = current.AddHours(-current.Hour);
					current = current.AddDays(1);
					break;

				case RollPoint.TopOfWeek:
					current = current.AddMilliseconds(-current.Millisecond);
					current = current.AddSeconds(-current.Second);
					current = current.AddMinutes(-current.Minute);
					current = current.AddHours(-current.Hour);
					current = current.AddDays(7 - (int)current.DayOfWeek);
					break;

				case RollPoint.TopOfMonth:
					current = current.AddMilliseconds(-current.Millisecond);
					current = current.AddSeconds(-current.Second);
					current = current.AddMinutes(-current.Minute);
					current = current.AddHours(-current.Hour);
					current = current.AddDays(DateTime.DaysInMonth(current.Year, current.Month) - current.Day);
					break;
			}	  
			return current;
		}

		#endregion

		#region Private Instance Fields

		/// <summary>
		/// This object supplies the current date/time.  Allows test code to plug in
		/// a method to control this class when testing date/time based rolling.
		/// </summary>
		private IDateTime m_dateTime = null;

		/// <summary>
		/// The date pattern. By default, the pattern is set to <c>".yyyy-MM-dd"</c> 
		/// meaning daily rollover.
		/// </summary>
		private string m_datePattern = ".yyyy-MM-dd";
  
		/// <summary>
		/// The actual formatted filename that is currently being written to
		/// or will be the file transferred to on roll over
		/// (based on staticLogFileName).
		/// </summary>
		private string m_scheduledFilename = null;
  
		/// <summary>
		/// The timestamp when we shall next recompute the filename.
		/// </summary>
		private DateTime m_nextCheck = DateTime.MaxValue;
  
		/// <summary>
		/// Holds date of last roll over
		/// </summary>
		private DateTime m_now;
  
		/// <summary>
		/// The type of rolling done
		/// </summary>
		private RollPoint m_rollPoint;
  
		/// <summary>
		/// The default maximum file size is 10MB
		/// </summary>
		private long m_maxFileSize = 10*1024*1024;
  
		/// <summary>
		/// There is zero backup files by default
		/// </summary>
		private int m_maxSizeRollBackups  = 0;

		/// <summary>
		/// How many sized based backups have been made so far
		/// </summary>
		private int m_curSizeRollBackups = 0;
  
		/// <summary>
		/// The rolling file count direction. 
		/// </summary>
		private int m_countDirection = -1;
  
		/// <summary>
		/// The rolling mode used in this appender.
		/// </summary>
		private RollingMode m_rollingStyle = RollingMode.Composite;

		/// <summary>
		/// Cache flag set if we are rolling by date.
		/// </summary>
		private bool m_rollDate = true;

		/// <summary>
		/// Cache flag set if we are rolling by size.
		/// </summary>
		private bool m_rollSize = true;
  
		/// <summary>
		/// Value indicting whether to always log to the same file.
		/// </summary>
		private bool m_staticLogFileName = true;
  
		/// <summary>
		/// FileName provided in configuration.  Used for rolling properly
		/// </summary>
		private string m_baseFileName;
  
		#endregion Private Instance Fields

		#region DateTime

		/// <summary>
		/// This interface is used to supply Date/Time information to the <see cref="RollingFileAppender"/>.
		/// </summary>
		/// <remarks>
		/// This interface is used to supply Date/Time information to the <see cref="RollingFileAppender"/>.
		/// Used primarily to allow test classes to plug themselves in so they can
		/// supply test date/times.
		/// </remarks>
		public interface IDateTime
		{
			/// <summary>
			/// Gets the &quot;current&quot; time.
			/// </summary>
			/// <value>The &quot;current&quot; time.</value>
			DateTime Now { get; }
		}

		/// <summary>
		/// Default implementation of <see cref="IDateTime"/> that returns the current time.
		/// </summary>
		private class DefaultDateTime : IDateTime
		{
			/// <summary>
			/// Gets the &quot;current&quot; time.
			/// </summary>
			/// <value>The &quot;current&quot; time.</value>
			public DateTime Now
			{
				get { return DateTime.Now; }
			}
		}

		#endregion DateTime

		#region NUnit Tests
#if NUNIT_TESTS

		/// <summary>
		/// Used for internal unit testing the <see cref="RollingFileAppender"/> class.
		/// </summary>
		[TestFixture] public class RollingFileAppenderTest
		{
			const string _fileName = "test_41d3d834_4320f4da.log";
			const string _testMessage = "01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567";
			const int _iMaximumFileSize = 450;	// in bytes
			int _iMessagesLoggedThisFile = 0;
			int _iMessagesLogged = 0;
			int _iCountDirection = 0;
			int _MaxSizeRollBackups = 3;
			CountingAppender _caRoot;
			Logger _root;

			/// <summary>
			/// Sets up variables used for the tests
			/// </summary>
			private void InitializeVariables()
			{
				_iMessagesLoggedThisFile = 0;
				_iMessagesLogged = 0;
				_iCountDirection = +1;  // Up
				_MaxSizeRollBackups = 3;
			}

			/// <summary>
			/// Shuts down any loggers in the hierarchy, along
			/// with all appenders, and deletes any test files used
			/// for logging.
			/// </summary>
			private void ResetAndDeleteTestFiles()
			{
				// Regular users should not use the clear method lightly!
				LogManager.GetLoggerRepository().ResetConfiguration();
				LogManager.GetLoggerRepository().Shutdown();
				((Hierarchy)LogManager.GetLoggerRepository()).Clear();

				DeleteTestFiles();
			}

			/// <summary>
			/// Any initialization that happens before each test can
			/// go here
			/// </summary>
			[SetUp] public void SetUp() 
			{
				ResetAndDeleteTestFiles();
				InitializeVariables();
			}

			/// <summary>
			/// Any steps that happen after each test go here
			/// </summary>
			[TearDown] public void TearDown() 
			{
				ResetAndDeleteTestFiles();
			}

			/// <summary>
			/// Finds the number of files that match the base file name,
			/// and matches the result against an expected count
			/// </summary>
			/// <param name="iExpectedCount"></param>
			private void VerifyFileCount( int iExpectedCount )
			{
				ArrayList alFiles = GetExistingFiles(_fileName);
				Assertion.AssertNotNull(alFiles);
				Assertion.AssertEquals(iExpectedCount, alFiles.Count);
			}

			/// <summary>
			/// Creates a file with the given number, and the shared base file name
			/// </summary>
			/// <param name="iFileNumber"></param>
			private void CreateFile( int iFileNumber )
			{
				FileInfo fileInfo = new FileInfo( MakeFileName(_fileName, iFileNumber) );

				FileStream fileStream = null;
				try 
				{
					fileStream = fileInfo.Create();
				}
				finally
				{
					if (null != fileStream)
					{
						try
						{
							fileStream.Close();
						} 
						catch {}
					}
					fileStream = null;
				}
			}

			/// <summary>
			/// Verifies that the code correctly loads all filenames
			/// </summary>
			[Test] public void TestGetExistingFiles()
			{
				VerifyFileCount(0);
				CreateFile(0);
				VerifyFileCount(1);
				CreateFile(1);
				VerifyFileCount(2);
			}

			/// <summary>
			/// Removes all test files that exist
			/// </summary>
			private void DeleteTestFiles()
			{
				ArrayList alFiles = GetExistingFiles(_fileName);
				foreach(string sFile in alFiles)
				{
					try
					{
						System.Diagnostics.Debug.WriteLine("Deleting test file " + sFile);
						System.IO.File.Delete(sFile);
					} 
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine("Exception while deleting test file " + ex.ToString());
					}
				}
			}

			/// <summary>
			/// Generates a file name associated with the count.
			/// </summary>
			/// <param name="iFileCount"></param>
			/// <returns></returns>
			private string MakeFileName(int iFileCount)
			{
				return MakeFileName(_fileName, iFileCount);
			}

			/// <summary>
			/// Generates a file name associated with the count, using
			/// the base file name.
			/// </summary>
			/// <param name="sBaseFile"></param>
			/// <param name="iFileCount"></param>
			/// <returns></returns>
			private string MakeFileName(string sBaseFile, int iFileCount)
			{
				if (0 == iFileCount) 
				{
					return sBaseFile;
				} 
				return sBaseFile + "." + iFileCount;
			}

			/// <summary>
			/// Returns a RollingFileAppender using all the internal settings for maximum
			/// file size and number of backups
			/// </summary>
			/// <returns></returns>
			private RollingFileAppender CreateAppender()
			{
				//
				// Use a basic pattern that
				// includes just the message and a CR/LF.
				//
				PatternLayout layout = new PatternLayout("%m%n");

				//
				// Create the new appender
				//
				RollingFileAppender appender = new RollingFileAppender();
				appender.Layout			  = layout;
				appender.File				= _fileName;
				appender.MaximumFileSize	 = _iMaximumFileSize.ToString();
				appender.MaxSizeRollBackups  = _MaxSizeRollBackups;
				appender.CountDirection	  = _iCountDirection;
				appender.RollingStyle		= RollingFileAppender.RollingMode.Size;
				appender.ActivateOptions();

				return appender;
			}

			/// <summary>
			/// Used for test purposes, a table of these objects can be used to identify
			/// any existing files and their expected length.
			/// </summary>
			public class RollFileEntry
			{
				/// <summary>
				/// Stores the name of the file
				/// </summary>
				private string m_fileName;

				/// <summary>
				/// The expected length of the file
				/// </summary>
				private long m_fileLength;

				/// <summary>
				/// Default constructor
				/// </summary>
				public RollFileEntry() {}

				/// <summary>
				/// Constructor used when the fileInfo and expected length are known
				/// </summary>
				/// <param name="fileName"></param>
				/// <param name="fileLength"></param>
				public RollFileEntry(string fileName, long fileLength)
				{
					m_fileName   = fileName;
					m_fileLength = fileLength;
				}

				/// <summary>
				/// Stores the name of the file
				/// </summary>
				public string FileName
				{
					get { return m_fileName; }
				}
						   
				/// <summary>
				/// The expected length of the file
				/// </summary>
				public long FileLength
				{
					get { return m_fileLength; }
				}
			}

			/// <summary>
			/// Used for table-driven testing.  This class holds information that can be used
			/// for testing of file rolling.
			/// </summary>
			public class RollConditions
			{
				/// <summary>
				/// A table of entries showing files that should exist and their expected sizes
				/// before logging is called
				/// </summary>
				private RollFileEntry[] m_preLogFileEntries;

				/// <summary>
				/// A table of entries showing files that should exist and their expected sizes
				/// after a message is logged
				/// </summary>
				private RollFileEntry[] m_postLogFileEntries;

				/// <summary>
				/// Constructor, taking all required parameters
				/// </summary>
				/// <param name="preLogFileEntries"></param>
				/// <param name="postLogFileEntries"></param>
				public RollConditions( RollFileEntry[] preLogFileEntries, RollFileEntry[] postLogFileEntries )
				{
					m_preLogFileEntries  = preLogFileEntries;
					m_postLogFileEntries = postLogFileEntries;
				}

				/// <summary>
				/// A table of entries showing files that should exist and their expected sizes
				/// before logging is called
				/// </summary>
				public RollFileEntry[] GetPreLogFileEntries()
				{
					return m_preLogFileEntries;
				}

				/// <summary>
				/// A table of entries showing files that should exist and their expected sizes
				/// after a message is logged
				/// </summary>
				public RollFileEntry[] GetPostLogFileEntries()
				{
					return m_postLogFileEntries;
				}
			}

			private void VerifyExistenceAndRemoveFromList( ArrayList alExisting, string sFileName, FileInfo file, RollFileEntry entry )
			{
				Assertion.Assert( "filename " + sFileName + " not found in test directory", alExisting.Contains( sFileName ) );
				Assertion.AssertEquals( "file length mismatch", entry.FileLength, file.Length );
				// Remove this file from the list
				alExisting.Remove( sFileName );
			}

			/// <summary>
			/// Checks that all the expected files exist, and only the expected files.  Also
			/// verifies the length of all files against the expected length
			/// </summary>
			/// <param name="sBaseFileName"></param>
			/// <param name="fileEntries"></param>
			private void VerifyFileConditions( string sBaseFileName, RollFileEntry[] fileEntries )
			{
				ArrayList alExisting = GetExistingFiles( sBaseFileName );
				if (null != fileEntries )
				{
//					AssertEquals( "File count mismatch", alExisting.Count, fileEntries.Length );
					foreach( RollFileEntry rollFile in fileEntries )
					{
						string sFileName = rollFile.FileName;
						FileInfo file = new FileInfo(sFileName);

						if (rollFile.FileLength > 0 )
						{
							Assertion.Assert( "filename " + sFileName + " does not exist", file.Exists );
							VerifyExistenceAndRemoveFromList( alExisting, sFileName, file, rollFile );
						}
						else
						{
							// If length is 0, file may not exist yet.  If file exists, make sure length
							// is zero.  If file doesn't exist, this is OK

							if (file.Exists )
							{
								VerifyExistenceAndRemoveFromList( alExisting, sFileName, file, rollFile );
							}
						}
					}
				}
				else
				{
					Assertion.AssertEquals( 0, alExisting.Count );
				}

				// This check ensures no extra files matching the wildcard pattern exist.
				// We only want the files we expect, and no others
				Assertion.AssertEquals( 0, alExisting.Count );
			}

			/// <summary>
			/// Called before logging a message to check that all the expected files exist, 
			/// and only the expected files.  Also verifies the length of all files against 
			/// the expected length
			/// </summary>
			/// <param name="sBaseFileName"></param>
			/// <param name="entry"></param>
			private void VerifyPreConditions( string sBaseFileName, RollConditions entry )
			{
				VerifyFileConditions( sBaseFileName, entry.GetPreLogFileEntries() );
			}

			/// <summary>
			/// Called after logging a message to check that all the expected files exist, 
			/// and only the expected files.  Also verifies the length of all files against 
			/// the expected length
			/// </summary>
			/// <param name="sBaseFileName"></param>
			/// <param name="entry"></param>
			private void VerifyPostConditions( string sBaseFileName, RollConditions entry )
			{
				VerifyFileConditions( sBaseFileName, entry.GetPostLogFileEntries() );
			}

			/// <summary>
			/// Logs a message, verifying the expected message counts against the 
			/// current running totals.
			/// </summary>
			/// <param name="entry"></param>
			/// <param name="sMessageToLog"></param>
			private void LogMessage( RollConditions entry, string sMessageToLog )
			{
				Assertion.AssertEquals(_caRoot.Counter, _iMessagesLogged++); 
				_root.Log(Level.Debug, sMessageToLog, null);
				Assertion.AssertEquals(_caRoot.Counter, _iMessagesLogged); 
				_iMessagesLoggedThisFile++;
			}

			private void DumpFileEntry( RollFileEntry entry )
			{
				System.Diagnostics.Debug.WriteLine( "\tfile   name: " + entry.FileName );
				System.Diagnostics.Debug.WriteLine( "\tfile length: " + entry.FileLength );
			}

			private void DumpTableEntry( RollConditions entry )
			{
				System.Diagnostics.Debug.WriteLine( "Pre-Conditions" );
				foreach( RollFileEntry file in entry.GetPreLogFileEntries() )
				{
					DumpFileEntry( file );
				}
				System.Diagnostics.Debug.WriteLine( "Post-Conditions" );
				foreach( RollFileEntry file in entry.GetPostLogFileEntries() )
				{
					DumpFileEntry( file );
				}
//				System.Diagnostics.Debug.WriteLine("");
			}

			/// <summary>
			/// Runs through all table entries, logging messages.  Before each message is logged,
			/// pre-conditions are checked to ensure the expected files exist and they are the
			/// expected size.  After logging, verifies the same.
			/// </summary>
			/// <param name="sBaseFileName"></param>
			/// <param name="entries"></param>
			/// <param name="sMessageToLog"></param>
			private void RollFromTableEntries( string sBaseFileName, RollConditions[] entries, string sMessageToLog )
			{
				for( int i=0; i<entries.Length; i++ )
				{
					RollConditions entry = entries[i];

//					System.Diagnostics.Debug.WriteLine( i + ": Entry " + i + " pre/post conditions");
//					DumpTableEntry( entry );
//					System.Diagnostics.Debug.WriteLine( i + ": Testing entry pre-conditions");
					VerifyPreConditions( sBaseFileName, entry );
//					System.Diagnostics.Debug.WriteLine( i + ": Logging message");
					LogMessage( entry, sMessageToLog );
//					System.Diagnostics.Debug.WriteLine( i + ": Testing entry post-conditions");
					VerifyPostConditions( sBaseFileName, entry );
//					System.Diagnostics.Debug.WriteLine( i + ": Finished validating entry\n");
				}
			}

			/// <summary>
			/// Returns the number of bytes logged per message, including
			/// any CR/LF characters in addition to the message length.
			/// </summary>
			/// <param name="sMessage"></param>
			/// <returns></returns>
			private int TotalMessageLength( string sMessage )
			{
				const int iLengthCRLF = 2;
				return sMessage.Length + iLengthCRLF;
			}
 
			/// <summary>
			/// Determines how many messages of a fixed length can be logged
			/// to a single file before the file rolls.
			/// </summary>
			/// <param name="iMessageLength"></param>
			/// <returns></returns>
			private int MessagesPerFile( int iMessageLength )
			{
				int iMessagesPerFile = _iMaximumFileSize / iMessageLength;
				
				//
				// RollingFileAppender checks for wrap BEFORE logging,
				// so we will actually get one more message per file than
				// we would otherwise.
				//
				if (iMessagesPerFile * iMessageLength < _iMaximumFileSize ) 
				{
					iMessagesPerFile++;
				}

				return iMessagesPerFile;
			}

			/// <summary>
			/// Determines the name of the current file
			/// </summary>
			/// <returns></returns>
			private string GetCurrentFile()
			{
				// Current file name is always the base file name when
				// counting.  Dates will need a different approach
				return _fileName;
			}

			/// <summary>
			/// Turns a group of file names into an array of file entries that include the name
			/// and a size.  This is useful for assigning the properties of backup files, when
			/// the length of the files are all the same size due to a fixed message length.
			/// </summary>
			/// <param name="sBackupGroup"></param>
			/// <param name="iBackupFileLength"></param>
			/// <returns></returns>
			private RollFileEntry[] MakeBackupFileEntriesFromBackupGroup( string sBackupGroup, int iBackupFileLength )
			{
				string[] sFiles = sBackupGroup.Split( ' ' );

				ArrayList alEntries = new ArrayList();

				for( int i=0; i<sFiles.Length; i++ )
				{
					// Weed out any whitespace entries from the array
					if (sFiles[i].Trim().Length > 0 )
					{
						alEntries.Add( new RollFileEntry( sFiles[i], iBackupFileLength ) );
					}
				}

				return (RollFileEntry[])alEntries.ToArray(typeof(RollFileEntry));
			}

			/// <summary>
			/// Finds the iGroup group in the string (comma separated groups)
			/// </summary>
			/// <param name="sBackupGroups"></param>
			/// <param name="iGroup"></param>
			/// <returns></returns>
			private string GetBackupGroup( string sBackupGroups, int iGroup )
			{
				string[] sGroups = sBackupGroups.Split( ',' );
				return sGroups[iGroup];
			}

			/// <summary>
			/// Builds a collection of file entries based on the file names
			/// specified in a groups string and the max file size from the
			/// stats object
			/// </summary>
			/// <param name="sBackupGroups"></param>
			/// <param name="stats"></param>
			/// <returns></returns>
			private RollFileEntry[] MakeBackupFileEntriesForPreCondition( string sBackupGroups, RollingStats stats )
			{
				if (0 == stats.NumberOfFileRolls )
				{
					return null;	// first round has no previous backups
				}
				string sGroup;
				if (0 == stats.MessagesThisFile )
				{
					// first file has special pattern...since rolling doesn't occur when message
					// is logged, rather before next message is logged.
					if (stats.NumberOfFileRolls <= 1 )
					{
						return null;   
					}
					// Use backup files from previous round.  The minus 2 is because we have already
					// rolled, and the first round uses null instead of the string
					sGroup = GetBackupGroup( sBackupGroups, stats.NumberOfFileRolls-2 );
				}
				else
				{
					sGroup = GetBackupGroup( sBackupGroups, stats.NumberOfFileRolls-1 );
				}
				return MakeBackupFileEntriesFromBackupGroup( sGroup, stats.MaximumFileSize );
			}

			/// <summary>
			/// Builds a collection of file entries based on the file names
			/// specified in a groups string and the max file size from the
			/// stats object
			/// </summary>
			/// <param name="sBackupGroups"></param>
			/// <param name="stats"></param>
			/// <returns></returns>
			private RollFileEntry[] MakeBackupFileEntriesForPostCondition( string sBackupGroups, RollingStats stats )
			{
				if (0 == stats.NumberOfFileRolls)
				{
					return null;	// first round has no previous backups
				}
				string sGroup = GetBackupGroup( sBackupGroups, stats.NumberOfFileRolls-1 );
				return MakeBackupFileEntriesFromBackupGroup( sGroup, stats.MaximumFileSize );
			}


			/// <summary>
			/// This class holds information that is used while we are generating
			/// test data sets
			/// </summary>
			public class RollingStats
			{
				private int iTotalMessageLength;
				private int iMessagesPerFile;
				private int iMessagesThisFile;
				private int iNumberOfFileRolls;

				/// <summary>
				/// Default constructor
				/// </summary>
				public RollingStats() {}

				/// <summary>
				/// Number of total bytes a log file can reach.
				/// </summary>
				public int MaximumFileSize
				{
					get { return TotalMessageLength * MessagesPerFile; }
				}

				/// <summary>
				/// The length of a message, including any CR/LF characters.
				/// This length assumes all messages are a fixed length for
				/// test purposes.
				/// </summary>
				public int TotalMessageLength
				{
					get { return iTotalMessageLength; }
					set { iTotalMessageLength = value;}
				}

				/// <summary>
				/// A count of the number of messages that are logged to each
				/// file.
				/// </summary>
				public int MessagesPerFile
				{
					get { return iMessagesPerFile; }
					set { iMessagesPerFile = value; }
				}

				/// <summary>
				/// Counts how many messages have been logged to the current file
				/// </summary>
				public int MessagesThisFile
				{
					get { return iMessagesThisFile; }
					set { iMessagesThisFile = value; }
				}

				/// <summary>
				/// Counts how many times a file roll has occurred
				/// </summary>
				public int NumberOfFileRolls
				{
					get { return iNumberOfFileRolls; }
					set { iNumberOfFileRolls = value; }
				}
			}

			/// <summary>
			/// The stats are used to keep track of progress while we are algorithmically
			/// generating a table of pre/post condition tests for file rolling.
			/// </summary>
			/// <param name="sTestMessage"></param>
			/// <returns></returns>
			private RollingStats InitializeStats( string sTestMessage )
			{
				RollingStats rollingStats = new RollingStats();

				rollingStats.TotalMessageLength = TotalMessageLength( sTestMessage );
				rollingStats.MessagesPerFile	= MessagesPerFile( rollingStats.TotalMessageLength );
				rollingStats.MessagesThisFile   = 0;
				rollingStats.NumberOfFileRolls  = 0;

				return rollingStats;
			}

			/// <summary>
			/// Takes an existing array of RollFileEntry objects, creates a new array one element
			/// bigger, and appends the final element to the end.  If the existing entries are
			/// null (no entries), then a one-element array is returned with the final element
			/// as the only entry.
			/// </summary>
			/// <param name="existing"></param>
			/// <param name="final"></param>
			/// <returns></returns>
			private RollFileEntry[] AddFinalElement( RollFileEntry[] existing, RollFileEntry final )
			{
				int iLength = 1;
				if (null != existing )
				{
					iLength += existing.Length;
				}
				RollFileEntry[] combined = new RollFileEntry[iLength];
				if (null != existing )
				{
					System.Array.Copy( existing, 0, combined, 0, existing.Length );
				}
				combined[iLength-1] = final;
				return combined;
			}

			/// <summary>
			/// Generates the pre and post condition arrays from an array of backup files and the
			/// current file / next file.
			/// </summary>
			/// <param name="sBackupFiles"></param>
			/// <param name="preCondition"></param>
			/// <param name="current"></param>
			/// <param name="currentNext"></param>
			/// <param name="rollingStats"></param>
			/// <returns></returns>
			private RollConditions BuildTableEntry( string sBackupFiles, RollConditions preCondition, RollFileEntry current, RollFileEntry currentNext, RollingStats rollingStats )
			{
				RollFileEntry[] backupsPost = MakeBackupFileEntriesForPostCondition( sBackupFiles, rollingStats );
				RollFileEntry[] post		= AddFinalElement( backupsPost, currentNext	);
				if (null == preCondition )
				{
					return new RollConditions( AddFinalElement(null, current), post );
				}
				return new RollConditions( preCondition.GetPostLogFileEntries(), post );
			}

			/// <summary>
			/// Returns a RollFileEntry that represents the next state of the current file,
			/// based on the current state.  When the current state would roll, the next
			/// entry is the current file wrapped to 0 bytes.  Otherwise, the next state
			/// is the post-condition passed in as the currentNext parameter
			/// </summary>
			/// <param name="rollingStats"></param>
			/// <param name="currentNext"></param>
			/// <returns></returns>
			private RollFileEntry MoveNextEntry( RollingStats rollingStats, RollFileEntry currentNext )
			{
				rollingStats.MessagesThisFile = rollingStats.MessagesThisFile + 1;
				if (rollingStats.MessagesThisFile >= rollingStats.MessagesPerFile )
				{
					rollingStats.MessagesThisFile = 0;
					rollingStats.NumberOfFileRolls = rollingStats.NumberOfFileRolls + 1;

					return new RollFileEntry( GetCurrentFile(), 0 );
				}
				else
				{
					return currentNext;
				}
			}

			/// <summary>
			/// Callback point for the regular expression parser.  Turns
			/// the number into a file name.
			/// </summary>
			/// <param name="match"></param>
			/// <returns></returns>
			private string NumberedNameMaker( Match match )
			{
				Int32 iValue = Int32.Parse(match.Value);
				return MakeFileName( _fileName, iValue );
			}

			/// <summary>
			/// Parses a numeric list of files, turning them into file names.
			/// Calls back to a method that does the actual replacement, turning
			/// the numeric value into a filename.
			/// </summary>
			/// <param name="sBackupInfo"></param>
			/// <param name="evaluator"></param>
			/// <returns></returns>
			string ConvertToFiles( string sBackupInfo, MatchEvaluator evaluator )
			{
				Regex regex = new Regex(@"\d+");
				return regex.Replace( sBackupInfo, evaluator );
			}

			/// <summary>
			/// Makes test entries used for verifying counted file names
			/// </summary>
			/// <param name="sTestMessage">A message to log repeatedly</param>
			/// <param name="sBackupInfo">Filename groups used to indicate backup file name progression
			/// that results after each message is logged</param>
			/// <param name="iMessagesToLog">How many times the test message will be repeatedly logged</param>
			/// <returns></returns>
			private RollConditions[] MakeNumericTestEntries(  string sTestMessage, string sBackupInfo, int iMessagesToLog )
			{
				return MakeTestEntries(  
					sTestMessage, 
					sBackupInfo, 
					iMessagesToLog, 
					new MatchEvaluator(NumberedNameMaker ) );
			}

			/// <summary>
			/// This routine takes a list of backup file names and a message that will be logged
			/// repeatedly, and generates a collection of objects containing pre-condition and 
			/// post-condition information.  This pre/post information shows the names and expected 
			/// file sizes for all files just before and just after a message is logged.
			/// </summary>
			/// <param name="sTestMessage">A message to log repeatedly</param>
			/// <param name="sBackupInfo">Filename groups used to indicate backup file name progression
			/// that results after each message is logged</param>
			/// <param name="iMessagesToLog">How many times the test message will be repeatedly logged</param>
			/// <param name="evaluator">Function that can turn a number into a filename</param>
			/// <returns></returns>
			private RollConditions[] MakeTestEntries( string sTestMessage, string sBackupInfo, int iMessagesToLog, MatchEvaluator evaluator )
			{
				string sBackupFiles = ConvertToFiles( sBackupInfo, evaluator );

				RollConditions[] table = new RollConditions[iMessagesToLog];

				RollingStats rollingStats = InitializeStats( sTestMessage );

				RollConditions preCondition = null;
				rollingStats.MessagesThisFile = 0;

				RollFileEntry currentFile = new RollFileEntry( GetCurrentFile(), 0 );
				for( int i=0; i<iMessagesToLog; i++ )
				{
					RollFileEntry currentNext = new RollFileEntry( 
						GetCurrentFile(), 
						(1 + rollingStats.MessagesThisFile) * rollingStats.TotalMessageLength );

					table[i] = BuildTableEntry( sBackupFiles, preCondition, currentFile, currentNext, rollingStats );
					preCondition = table[i];

//System.Diagnostics.Debug.WriteLine( "Message " + i );
//DumpTableEntry( table[i] );

					currentFile = MoveNextEntry( rollingStats, currentNext );
				}

				return table;
			}

			/// <summary>
			/// Uses the externally defined rolling table to verify rolling names/sizes
			/// 
			/// Pattern is:  check pre-conditions.  Log messages, checking size of current file.
			/// when size exceeds limit, check post conditions.  Can determine from message the
			/// number of messages N that will cause a roll to occur.  Challenge is to verify the
			/// expected files, their sizes, and the names.  For a message of length L, the backups
			/// will be of size (N * L), and the current file will be of size (K * L), where K is
			/// the number of messages that have been logged to this file.
			///
			/// File sizes can be checked algorithmically.  
			/// 
			/// File names are generated using a table driven algorithm, where a number is turned into
			/// the actual filename.
			/// 
			/// The entries are comma-separated, with spaces between the names.  Each comma indicates
			/// a 'roll', and the group between commas indicates the numbers for all backup files that
			/// occur as a result of the roll.  It is assumed that no backup files exist before a roll 
			/// occurs
			/// </summary>
			/// <param name="table"></param>
			private void VerifyRolling( RollConditions[] table )
			{
				ConfigureRootAppender();
				RollFromTableEntries( _fileName, table, _testMessage );
			}

			/// <summary>
			/// Validates rolling using a fixed number of backup files, with
			/// count direction set to up, so that newer files have higher counts.
			/// Newest = N, Oldest = N-K, where K is the number of backups to allow
			/// and N is the number of times rolling has occurred.
			/// </summary>
			[Test] public void TestRollingCountUpFixedBackups()
			{
				//
				// Oldest to newest when reading in a group left-to-right, so 1 2 3 means 1 is the
				// oldest, and 3 is the newest
				//
				string sBackupInfo = "1, 1 2, 1 2 3, 2 3 4, 3 4 5";

				//
				// Count Up
				//
				_iCountDirection = +1;

				//
				// Log 30 messages.  This is 5 groups, 6 checks per group ( 0, 100, 200, 300, 400, 500 
				// bytes for current file as messages are logged.
				//
				int iMessagesToLog = 30;	

				VerifyRolling( MakeNumericTestEntries( _testMessage, sBackupInfo, iMessagesToLog ) );
			}

			/// <summary>
			/// Validates rolling using an infinite number of backup files, with
			/// count direction set to up, so that newer files have higher counts.
			/// Newest = N, Oldest = 1, where N is the number of times rolling has 
			/// occurred.
			/// </summary>
			[Test] public void TestRollingCountUpInfiniteBackups()
			{
				//
				// Oldest to newest when reading in a group left-to-right, so 1 2 3 means 1 is the
				// oldest, and 3 is the newest
				//
				string sBackupInfo = "1, 1 2, 1 2 3, 1 2 3 4, 1 2 3 4 5";

				//
				// Count Up
				//
				_iCountDirection = +1;

				//
				// Infinite backups
				//
				_MaxSizeRollBackups = -1;

				//
				// Log 30 messages.  This is 5 groups, 6 checks per group ( 0, 100, 200, 300, 400, 500 
				// bytes for current file as messages are logged.
				//
				int iMessagesToLog = 30;	

				VerifyRolling( MakeNumericTestEntries( _testMessage, sBackupInfo, iMessagesToLog ) );
			}

			/// <summary>
			/// Validates rolling with no backup files, with count direction set to up.
			/// Only the current file should be present, wrapping to 0 bytes once the
			/// previous file fills up.
			/// </summary>
			[Test] public void TestRollingCountUpZeroBackups()
			{
				//
				// Oldest to newest when reading in a group left-to-right, so 1 2 3 means 1 is the
				// oldest, and 3 is the newest
				//
				string sBackupInfo = ", , , , ";

				//
				// Count Up
				//
				_iCountDirection = +1;

				//
				// No backups
				//
				_MaxSizeRollBackups = 0;

				//
				// Log 30 messages.  This is 5 groups, 6 checks per group ( 0, 100, 200, 300, 400, 500 
				// bytes for current file as messages are logged.
				//
				int iMessagesToLog = 30;	

				VerifyRolling( MakeNumericTestEntries( _testMessage, sBackupInfo, iMessagesToLog ) );
			}


			/// <summary>
			/// Validates rolling using a fixed number of backup files, with
			/// count direction set to down, so that older files have higher counts.
			/// Newest = 1, Oldest = N, where N is the number of backups to allow
			/// </summary>
			[Test] public void TestRollingCountDownFixedBackups()
			{
				//
				// Oldest to newest when reading in a group left-to-right, so 1 2 3 means 1 is the
				// oldest, and 3 is the newest
				//
				string sBackupInfo = "1, 1 2, 1 2 3, 1 2 3, 1 2 3";

				//
				// Count Up
				//
				_iCountDirection = -1;

				//
				// Log 30 messages.  This is 5 groups, 6 checks per group ( 0, 100, 200, 300, 400, 500 
				// bytes for current file as messages are logged.
				//
				int iMessagesToLog = 30;	

				VerifyRolling( MakeNumericTestEntries( _testMessage, sBackupInfo, iMessagesToLog ) );
			}

			/// <summary>
			/// Validates rolling using an infinite number of backup files, with
			/// count direction set to down, so that older files have higher counts.
			/// Newest = 1, Oldest = N, where N is the number of times rolling has
			/// occurred
			/// </summary>
			[Test] public void TestRollingCountDownInfiniteBackups()
			{
				//
				// Oldest to newest when reading in a group left-to-right, so 1 2 3 means 1 is the
				// oldest, and 3 is the newest
				//
				string sBackupInfo = "1, 1 2, 1 2 3, 1 2 3 4, 1 2 3 4 5";

				//
				// Count Down
				//
				_iCountDirection = -1;

				//
				// Infinite backups
				//
				_MaxSizeRollBackups = -1;

				//
				// Log 30 messages.  This is 5 groups, 6 checks per group ( 0, 100, 200, 300, 400, 500 
				// bytes for current file as messages are logged.
				//
				int iMessagesToLog = 30;	

				VerifyRolling( MakeNumericTestEntries( _testMessage, sBackupInfo, iMessagesToLog ) );
			}

			/// <summary>
			/// Validates rolling with no backup files, with count direction set to down.
			/// Only the current file should be present, wrapping to 0 bytes once the
			/// previous file fills up.
			/// </summary>
			[Test] public void TestRollingCountDownZeroBackups()
			{
				//
				// Oldest to newest when reading in a group left-to-right, so 1 2 3 means 1 is the
				// oldest, and 3 is the newest
				//
				string sBackupInfo = ", , , , ";

				//
				// Count Up
				//
				_iCountDirection = -1;

				//
				// No backups
				//
				_MaxSizeRollBackups = 0;

				//
				// Log 30 messages.  This is 5 groups, 6 checks per group ( 0, 100, 200, 300, 400, 500 
				// bytes for current file as messages are logged.
				//
				int iMessagesToLog = 30;	

				VerifyRolling( MakeNumericTestEntries( _testMessage, sBackupInfo, iMessagesToLog ) );
			}

			/// <summary>
			/// Configures the root appender for counting and rolling
			/// </summary>
			private void ConfigureRootAppender()
			{
				_root = ((Hierarchy)LogManager.GetLoggerRepository()).Root;	
				_root.Level = Level.Debug;
				_caRoot = new CountingAppender();
				_root.AddAppender(_caRoot);
				Assertion.AssertEquals(_caRoot.Counter, 0); 

				//
				// Set the root appender with a RollingFileAppender
				//
				_root.AddAppender( CreateAppender() );

				_root.Repository.Configured = true;
			}

			/// <summary>
			/// Verifies that the current backup index is detected correctly when initializing
			/// </summary>
			/// <param name="sBaseFile"></param>
			/// <param name="alFiles"></param>
			/// <param name="iExpectedCurSizeRollBackups"></param>
			private void VerifyInitializeRollBackupsFromBaseFile( string sBaseFile, ArrayList alFiles, int iExpectedCurSizeRollBackups )
			{
				InitializeAndVerifyExpectedValue( alFiles, sBaseFile, CreateRollingFileAppender( "5,0,1" ), iExpectedCurSizeRollBackups );
			}

			/// <summary>
			/// Tests that the current backup index is 0 when no
			/// existing files are seen
			/// </summary>
			[Test] public void TestInitializeRollBackups1()
			{
				string sBaseFile = "LogFile.log";
				ArrayList arrFiles = new ArrayList();
				arrFiles.Add( "junk1" );
				arrFiles.Add( "junk1.log" );
				arrFiles.Add( "junk2.log" );
				arrFiles.Add( "junk.log.1" );
				arrFiles.Add( "junk.log.2" );

				int iExpectedCurSizeRollBackups = 0;
				VerifyInitializeRollBackupsFromBaseFile( sBaseFile, arrFiles, iExpectedCurSizeRollBackups );
			}

			/// <summary>
			/// Verifies that files are detected when the base file is specified
			/// </summary>
			/// <param name="sBaseFile"></param>
			private void VerifyInitializeRollBackupsFromBaseFile( string sBaseFile )
			{
				ArrayList alFiles = MakeTestDataFromString( sBaseFile, "0,1,2" );

				int iExpectedCurSizeRollBackups = 2;
				VerifyInitializeRollBackupsFromBaseFile( sBaseFile, alFiles, iExpectedCurSizeRollBackups );
			}

			/// <summary>
			/// Verifies that count goes to the highest when counting up
			/// </summary>
			[Test] public void TestInitializeCountUpFixed()
			{
				ArrayList alFiles = MakeTestDataFromString( "3,4,5" );
				int iExpectedValue = 5;
				InitializeAndVerifyExpectedValue( alFiles, _fileName, CreateRollingFileAppender( "3,0,1" ), iExpectedValue );
			}

			/// <summary>
			/// Verifies that count goes to the highest when counting up
			/// </summary>
			[Test] public void TestInitializeCountUpFixed2()
			{
				ArrayList alFiles = MakeTestDataFromString( "0,3" );
				int iExpectedValue = 3;
				InitializeAndVerifyExpectedValue( alFiles, _fileName, CreateRollingFileAppender( "3,0,1" ), iExpectedValue );
			}

			/// <summary>
			/// Verifies that count stays at 0 for the zero backups case
			/// when counting up
			/// </summary>
			[Test] public void TestInitializeCountUpZeroBackups()
			{
				ArrayList alFiles = MakeTestDataFromString( "0,3" );
				int iExpectedValue = 0;
				InitializeAndVerifyExpectedValue( alFiles, _fileName, CreateRollingFileAppender( "0,0,1" ), iExpectedValue );
			}

			/// <summary>
			/// Verifies that count stays at 0 for the zero backups case
			/// when counting down
			/// </summary>
			[Test] public void TestInitializeCountDownZeroBackups()
			{
				ArrayList alFiles = MakeTestDataFromString( "0,3" );
				int iExpectedValue = 0;
				InitializeAndVerifyExpectedValue( alFiles, _fileName, CreateRollingFileAppender( "0,0,-1" ), iExpectedValue );
			}


			/// <summary>
			/// Verifies that count goes to the highest when counting up
			/// </summary>
			[Test] public void TestInitializeCountDownFixed()
			{
				ArrayList alFiles = MakeTestDataFromString( "4,5,6" );
				VerifyInitializeDownFixedExpectedValue( alFiles, _fileName, 0 );
			}

			/// <summary>
			/// Verifies that count goes to the highest when counting up
			/// </summary>
			[Test] public void TestInitializeCountDownFixed2()
			{
				ArrayList alFiles = MakeTestDataFromString( "1,5,6" );
				VerifyInitializeDownFixedExpectedValue( alFiles, _fileName, 1 );
			}

			/// <summary>
			/// Verifies that count goes to the highest when counting up
			/// </summary>
			[Test] public void TestInitializeCountDownFixed3()
			{
				ArrayList alFiles = MakeTestDataFromString( "2,5,6" );
				VerifyInitializeDownFixedExpectedValue( alFiles, _fileName, 2 );
			}

			/// <summary>
			/// Verifies that count goes to the highest when counting up
			/// </summary>
			[Test] public void TestInitializeCountDownFixed4()
			{
				ArrayList alFiles = MakeTestDataFromString( "3,5,6" );
				VerifyInitializeDownFixedExpectedValue( alFiles, _fileName, 3 );
			}

			/// <summary>
			/// Verifies that count goes to the highest when counting up
			/// </summary>
			[Test] public void TestInitializeCountDownFixed5()
			{
				ArrayList alFiles = MakeTestDataFromString( "1,2,3" );
				VerifyInitializeDownFixedExpectedValue( alFiles, _fileName, 3 );
			}

			/// <summary>
			/// Verifies that count goes to the highest when counting up
			/// </summary>
			[Test] public void TestInitializeCountDownFixed6()
			{
				ArrayList alFiles = MakeTestDataFromString( "1,2" );
				VerifyInitializeDownFixedExpectedValue( alFiles, _fileName, 2 );
			}

			/// <summary>
			/// Verifies that count goes to the highest when counting up
			/// </summary>
			[Test] public void TestInitializeCountDownFixed7()
			{
				ArrayList alFiles = MakeTestDataFromString( "2,3" );
				VerifyInitializeDownFixedExpectedValue( alFiles, _fileName, 3 );
			}

			private void InitializeAndVerifyExpectedValue( ArrayList alFiles, string sBaseFile, RollingFileAppender rfa, int iExpectedValue )
			{
				rfa.InitializeRollBackups( sBaseFile, alFiles );
				Assertion.AssertEquals( iExpectedValue, rfa.m_curSizeRollBackups );
			}

			/// <summary>
			/// Tests the count down case, with infinite max backups, to see that
			/// initialization of the rolling file appender results in the expected value
			/// </summary>
			/// <param name="alFiles"></param>
			/// <param name="sBaseFile"></param>
			/// <param name="iExpectedValue"></param>
			private void VerifyInitializeDownInfiniteExpectedValue( ArrayList alFiles, string sBaseFile, int iExpectedValue )
			{
				InitializeAndVerifyExpectedValue( alFiles, sBaseFile, CreateRollingFileAppender( "-1,0,-1" ), iExpectedValue );
			}

			/// <summary>
			/// Creates a RollingFileAppender with the desired values, where the
			/// values are passed as a comma separated string, with 3 parameters,
			/// m_maxSizeRollBackups, m_curSizeRollBackups, CountDirection
			/// </summary>
			/// <param name="sParams"></param>
			/// <returns></returns>
			private RollingFileAppender CreateRollingFileAppender( string sParams )
			{
				string[] asParams = sParams.Split(',');
				if (null == asParams || asParams.Length != 3 )
				{
					throw new ArgumentOutOfRangeException(sParams, sParams, "Must have 3 comma separated params: MaxSizeRollBackups, CurSizeRollBackups, CountDirection" );
				}

				RollingFileAppender rfa = new RollingFileAppender();
				rfa.RollingStyle = RollingMode.Size;
				rfa.m_maxSizeRollBackups = Int32.Parse(asParams[0].Trim());
				rfa.m_curSizeRollBackups = Int32.Parse(asParams[1].Trim());
				rfa.CountDirection	   = Int32.Parse(asParams[2].Trim());

				return rfa;
			}

			/// <summary>
			/// Verifies that count goes to the highest when counting down
			/// and infinite backups are selected
			/// </summary>
			[Test] public void TestInitializeCountDownInfinite()
			{
				ArrayList alFiles = MakeTestDataFromString( "2,3" );
				VerifyInitializeDownInfiniteExpectedValue( alFiles, _fileName, 3 );
			}

			/// <summary>
			/// Verifies that count goes to the highest when counting down
			/// and infinite backups are selected
			/// </summary>
			[Test] public void TestInitializeCountDownInfinite2()
			{
				ArrayList alFiles = MakeTestDataFromString( "2,3,4,5,6,7,8,9,10" );
				VerifyInitializeDownInfiniteExpectedValue( alFiles, _fileName, 10 );
			}

			/// <summary>
			/// Verifies that count goes to the highest when counting down
			/// and infinite backups are selected
			/// </summary>
			[Test] public void TestInitializeCountDownInfinite3()
			{
				ArrayList alFiles = MakeTestDataFromString( "9,10,3,4,5,7,9,6,1,2,8" );
				VerifyInitializeDownInfiniteExpectedValue( alFiles, _fileName, 10 );
			}

			/// <summary>
			/// Verifies that count goes to the highest when counting up
			/// and infinite backups are selected
			/// </summary>
			[Test] public void TestInitializeCountUpInfinite()
			{
				ArrayList alFiles = MakeTestDataFromString( "2,3" );
				VerifyInitializeUpInfiniteExpectedValue( alFiles, _fileName, 3 );
			}

			/// <summary>
			/// Verifies that count goes to the highest when counting up
			/// and infinite backups are selected
			/// </summary>
			[Test] public void TestInitializeCountUpInfinite2()
			{
				ArrayList alFiles = MakeTestDataFromString( "2,3,4,5,6,7,8,9,10" );
				VerifyInitializeUpInfiniteExpectedValue( alFiles, _fileName, 10 );
			}

			/// <summary>
			/// Verifies that count goes to the highest when counting up
			/// and infinite backups are selected
			/// </summary>
			[Test] public void TestInitializeCountUpInfinite3()
			{
				ArrayList alFiles = MakeTestDataFromString( "9,10,3,4,5,7,9,6,1,2,8" );
				VerifyInitializeUpInfiniteExpectedValue( alFiles, _fileName, 10 );
			}



			/// <summary>
			/// Tests the count up case, with infinite max backups , to see that
			/// initialization of the rolling file appender results in the expected value
			/// </summary>
			/// <param name="alFiles"></param>
			/// <param name="sBaseFile"></param>
			/// <param name="iExpectedValue"></param>
			private void VerifyInitializeUpInfiniteExpectedValue( ArrayList alFiles, string sBaseFile, int iExpectedValue )
			{
				InitializeAndVerifyExpectedValue( alFiles, sBaseFile, CreateRollingFileAppender( "-1,0,1" ), iExpectedValue );
			}



			/// <summary>
			/// Tests the count down case, with max backups limited to 3, to see that
			/// initialization of the rolling file appender results in the expected value
			/// </summary>
			/// <param name="alFiles"></param>
			/// <param name="sBaseFile"></param>
			/// <param name="iExpectedValue"></param>
			private void VerifyInitializeDownFixedExpectedValue( ArrayList alFiles, string sBaseFile, int iExpectedValue )
			{
				InitializeAndVerifyExpectedValue( alFiles, sBaseFile, CreateRollingFileAppender( "3,0,-1" ), iExpectedValue );
			}

			/// <summary>
			/// Turns a string of comma separated numbers into a collection of filenames
			/// generated from the numbers.  
			/// 
			/// Defaults to filename in _fileName variable.
			/// 
			/// </summary>
			/// <param name="sFileNumbers">Comma separated list of numbers for counted file names</param>
			/// <returns></returns>
			private ArrayList MakeTestDataFromString( string sFileNumbers )
			{
				return MakeTestDataFromString( _fileName, sFileNumbers );
			}

			/// <summary>
			/// Turns a string of comma separated numbers into a collection of filenames
			/// generated from the numbers
			/// 
			/// Uses the input filename.
			/// </summary>
			/// <param name="sFileName">Name of file to combine with numbers when generating counted file names</param>
			/// <param name="sFileNumbers">Comma separated list of numbers for counted file names</param>
			/// <returns></returns>
			private ArrayList MakeTestDataFromString( string sFileName, string sFileNumbers )
			{
				ArrayList alFiles = new ArrayList();

				string[] sNumbers = sFileNumbers.Split( ',' );
				foreach( string sNumber in sNumbers )
				{
					Int32 iValue = Int32.Parse( sNumber.Trim() );
					alFiles.Add( MakeFileName(sFileName, iValue) );
				}

				return alFiles;
			}

			/// <summary>
			/// Tests that the current backup index is correctly detected
			/// for a file with no extension
			/// </summary>
			[Test] public void TestInitializeRollBackups2()
			{
				VerifyInitializeRollBackupsFromBaseFile( "LogFile" );
			}

			/// <summary>
			/// Tests that the current backup index is correctly detected
			/// for a file with a .log extension
			/// </summary>
			[Test] public void TestInitializeRollBackups3()
			{
				VerifyInitializeRollBackupsFromBaseFile( "LogFile.log" );
			}

			/// <summary>
			/// Makes sure that the initialization can detect the backup
			/// number correctly.
			/// </summary>
			/// <param name="iBackups"></param>
			/// <param name="iMaxSizeRollBackups"></param>
			public void VerifyInitializeRollBackups(int iBackups, int iMaxSizeRollBackups)
			{
				string sBaseFile = "LogFile.log";
				ArrayList arrFiles = new ArrayList();
				arrFiles.Add( "junk1" );
				for( int i=0; i<iBackups; i++ ) 
				{
					arrFiles.Add( MakeFileName(sBaseFile, i) );
				}
				RollingFileAppender rfa = new RollingFileAppender();
				rfa.RollingStyle = RollingMode.Size;
				rfa.m_maxSizeRollBackups = iMaxSizeRollBackups;
				rfa.m_curSizeRollBackups = 0;
				rfa.InitializeRollBackups( sBaseFile, arrFiles );

				// iBackups	/ Meaning
				// 0 = none
				// 1 = file.log
				// 2 = file.log.1
				// 3 = file.log.2
				if (0 == iBackups || 
					1 == iBackups ) 
				{
					Assertion.AssertEquals( 0, rfa.m_curSizeRollBackups );
				} 
				else 
				{
					Assertion.AssertEquals( Math.Min( iBackups-1, iMaxSizeRollBackups), rfa.m_curSizeRollBackups );
				}
			}

			/// <summary>
			/// Tests that the current backup index is correctly detected,
			/// and gets no bigger than the max backups setting
			/// </summary>
			[Test] public void TestInitializeRollBackups4()
			{
				const int iMaxRollBackups = 5;
				VerifyInitializeRollBackups( 0, iMaxRollBackups );
				VerifyInitializeRollBackups( 1, iMaxRollBackups );
				VerifyInitializeRollBackups( 2, iMaxRollBackups );
				VerifyInitializeRollBackups( 3, iMaxRollBackups );
				VerifyInitializeRollBackups( 4, iMaxRollBackups );
				VerifyInitializeRollBackups( 5, iMaxRollBackups );
				VerifyInitializeRollBackups( 6, iMaxRollBackups );
				// Final we cap out at the max value
				VerifyInitializeRollBackups( 7, iMaxRollBackups );
				VerifyInitializeRollBackups( 8, iMaxRollBackups );
			}

			/// <summary>
			/// 
			/// </summary>
			[Test,Ignore("Not Implemented: Want to test counted files limited up, to see that others are ?? ignored? deleted?")]
			public void TestInitialization3()
			{
			}

			/// <summary>
			/// 
			/// </summary>
			[Test,Ignore("Not Implemented: Want to test counted files limited down, to see that others are ?? ignored? deleted?")]
			public void TestInitialization4()
			{
			}

			/// <summary>
			/// 
			/// </summary>
			[Test,Ignore("Not Implemented: Want to test dated files with a limit, to see that others are ?? ignored? deleted?")]
			public void TestInitialization5()
			{
			}

			/// <summary>
			/// 
			/// </summary>
			[Test,Ignore("Not Implemented: Want to test dated files with no limit, to see that others are ?? ignored? deleted?")]
			public void TestInitialization6()
			{
			}

			/// <summary>
			/// 
			/// </summary>
			[Test,Ignore("Not Implemented: Want to test dated files with mixed dates existing, to see that other dates do not matter")]
			public void TestInitialization7()
			{
			}
		}
#endif // NUNIT_TESTS
		#endregion
	}
}
