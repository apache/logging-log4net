namespace log4net.Util.PatternStringConverters.LogLogConverters
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;


	/// <summary>
	/// Uses the <see cref="LogLog.Source"/> property to write out the fully qualified type name of the logger.
	/// </summary>
	public class LogLogSourcePatternConverter : LogLogPatternConverter
	{
		/// <summary>
		/// Writes the fully qualified type name of the logger stored in <see cref="LogLog.Source"/> to the output.
		/// </summary>
		/// <param name="writer"><see cref="TextWriter" /> that will receive the formatted result.</param>
		/// <param name="logLog">The <see cref="LogLog" /> instance on which the pattern converter should be executed.</param>
		/// <remarks>
		/// </remarks>
		override protected void Convert(TextWriter writer, LogLog logLog)
		{
			writer.Write(logLog.Source);
		}
	}
}
