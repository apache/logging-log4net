namespace log4net.Util.PatternStringConverters.LogLogConverters
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;


	/// <summary>
	/// Abstract class that provides the formatting functionality that derived classes need.
	/// </summary>
	/// <remarks>
	/// Conversion specifiers in a conversion patterns are parsed to
	/// individual PatternConverters. Each of which is responsible for
	/// converting a LogLog object in a converter specific manner.
	/// </remarks>
	public abstract class LogLogPatternConverter : PatternConverter
	{
		#region Fields
		#endregion Fields

		#region Protected Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="LogLogPatternConverter" /> class.
		/// </summary>
		protected LogLogPatternConverter()
		{
		}

		#endregion Protected Instance Constructors

		#region Protected Abstract Methods

		/// <summary>
		/// Derived pattern converters must override this method in order to
		/// convert conversion specifiers in the correct way.
		/// </summary>
		/// <param name="writer"><see cref="TextWriter" /> that will receive the formatted result.</param>
		/// <param name="logLog">The <see cref="LogLog" /> instance on which the pattern converter should be executed.</param>
		abstract protected void Convert(TextWriter writer, LogLog logLog);

		#endregion Protected Abstract Methods

		#region Protected Methods

		/// <summary>
		/// Derived pattern converters must override this method in order to
		/// convert conversion specifiers in the correct way.
		/// </summary>
		/// <param name="writer"><see cref="TextWriter" /> that will receive the formatted result.</param>
		/// <param name="state">The state object on which the pattern converter should be executed.</param>
		override protected void Convert(TextWriter writer, object state)
		{
			if (state == null)
			{
				throw new ArgumentNullException(nameof(state));
			}

			LogLog logLog = state as LogLog;
			if (logLog != null)
			{
				Convert(writer, logLog);
			}
			else
			{
				throw new ArgumentException("state must be of type [" + typeof(LogLog).FullName + "]", "state");
			}
		}

		#endregion Protected Methods
	}
}
