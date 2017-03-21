using System;
using System.Security;
using System.Security.Principal;
using System.Threading;
using log4net.Util;

namespace log4net.Core
{
	/// <summary>
	/// Provide methods for interactions with WindowsIdentity.
	/// </summary>
	public class WindowsIdentityProvider
	{
		private readonly static Type declaringType = typeof(WindowsIdentityProvider);

		#region Public Static Properties

		/// <summary>
		/// Gets the cached name of the current WindowsIdentity.
		/// </summary>
		/// <value>
		/// The cached name of the current WindowsIdentity.
		/// </value>
		/// <exception cref="SecurityException"/>
		/// <remarks>
		/// <para>
		/// Gets the cached name of the current WindowsIdentity.
		/// </para>
		/// <para>
		/// Calls <c>WindowsIdentity.GetCurrent().Name</c> to get the name of
		/// the current windows user and cache it.
		/// </para>
		/// </remarks>
		public static string CurrentIdentityName
		{
			get
			{
				if (s_currentIdentityName != null)
					return s_currentIdentityName;
				lock (s_syncRoot)
				{
					if (s_currentIdentityName != null)
						return s_currentIdentityName;
					s_currentIdentityName = GetCurrentIdentityName();
					s_updateCurrentIdentityNameTimer = new Timer(UpdateCurrentIdentityName, null, s_updateCurrentIdentityNameInterval, s_updateCurrentIdentityNameInterval);
				}
				return s_currentIdentityName;
			}
		}

		private static void UpdateCurrentIdentityName(object state)
		{
			try
			{
				var identityName = GetCurrentIdentityName();
				if (!string.IsNullOrEmpty(identityName))
				{
					s_currentIdentityName = identityName;
				}
			}
			catch (SecurityException)
			{
				// This security exception will occur if the caller does not have 
				// some undefined set of SecurityPermission flags.
				LogLog.Debug(declaringType, "Security exception while trying to get current windows identity. Error Ignored. Empty user name.");
			}
		}

		/// <para>
		/// Timing for these operations:
		/// </para>
		/// <list type="table">
		///   <listheader>
		///     <term>Method</term>
		///     <description>Results</description>
		///   </listheader>
		///   <item>
		///	    <term><c>WindowsIdentity.GetCurrent()</c></term>
		///	    <description>10000 loops, 00:00:00.2031250 seconds</description>
		///   </item>
		///   <item>
		///	    <term><c>WindowsIdentity.GetCurrent().Name</c></term>
		///	    <description>10000 loops, 00:00:08.0468750 seconds</description>
		///   </item>
		/// </list>
		/// <para>
		/// This means we could speed things up almost 40 times by caching the 
		/// value of the <c>WindowsIdentity.GetCurrent().Name</c> property, since 
		/// this takes (8.04-0.20) = 7.84375 seconds.
		/// </para>
		private static string GetCurrentIdentityName()
		{
			WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();
			return windowsIdentity.Name ?? "";
		}

		#endregion

		#region Private Static Fields

		/// <summary>
		/// Lock object used to synchronize updates within this instance.
		/// </summary>
		private readonly static object s_syncRoot = new object();

		/// <summary>
		/// Interval for current Identity Name updates.
		/// </summary>
		private readonly static TimeSpan s_updateCurrentIdentityNameInterval = TimeSpan.FromSeconds(15);

		/// <summary>
		/// Timer for current Identity Name updates.
		/// </summary>
		private static Timer s_updateCurrentIdentityNameTimer;

		/// <value>
		/// The cached name of the current WindowsIdentity.
		/// </value>
		private static volatile string s_currentIdentityName;

		#endregion
	}
}
