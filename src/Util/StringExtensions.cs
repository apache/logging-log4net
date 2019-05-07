using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace log4net.Util
{
	/// <summary>
	/// Extension methods for <see cref="string"/>
	/// </summary>
	public static class StringExtensions
	{
		/// <summary>
		/// Replaces all non alphanumeric characters in a string with the replacement specified
		/// </summary>
		/// <param name="input"></param>
		/// <param name="replacement"></param>
		/// <returns></returns>
		public static string ReplaceNonAlphanumericChars(this string input, string replacement)
		{
			if (string.IsNullOrWhiteSpace(input)) return string.Empty;
			var mName = input;
			foreach (var c in mName.ToCharArray().Where(c => !char.IsLetterOrDigit(c)))
			{
				mName = mName.Replace(c.ToString(CultureInfo.InvariantCulture), replacement);
			}
			return mName;
		}
	}
}
