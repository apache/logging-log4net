using System;
using System.Reflection;

namespace log4net.Tests
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class Util
	{
		public Util()
		{
		}

		public static object InvokeMethod(object target, string name, params object[] args)
		{
			return target.GetType().GetMethod(name, BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance).Invoke(target, args);
		}

		public static object InvokeMethod(Type target, string name, params object[] args)
		{
			return target.GetMethod(name, BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Static).Invoke(null, args);
		}

		public static object GetField(object target, string name)
		{
			return target.GetType().GetField(name, BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance).GetValue(target);
		}

		public static void SetField(object target, string name, object val)
		{
			target.GetType().GetField(name, BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance).SetValue(target, val);
		}
	}
}
