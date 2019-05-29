namespace log4net.Util.PatternStringConverters.LogLogConverters
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;


	/// <summary>
	/// Uses the <see cref="LogLog.Prefix"/> property to write out the event level.
	/// </summary>
	public class LogLogLevelPatternConverter : LogLogPatternConverter
	{
		/// <summary>
		/// Write the event level stored in <see cref="LogLog.Prefix"/> to the output
		/// </summary>
		/// <param name="writer"><see cref="TextWriter" /> that will receive the formatted result.</param>
		/// <param name="logLog">The <see cref="LogLog" /> instance on which the pattern converter should be executed.</param>
		override protected void Convert(TextWriter writer, LogLog logLog)
		{
			writer.Write(logLog.Prefix);
		}
	}
}
