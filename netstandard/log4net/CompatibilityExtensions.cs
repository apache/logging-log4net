#if NETSTANDARD1_3

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Xml;

namespace log4net
{
    internal static class CompatibilityExtensions
    {
        public static void Close(this Mutex mutex) => mutex.Dispose();
        public static void Close(this Socket socket) => socket.Dispose();
        public static void Close(this Stream stream) => stream.Dispose();
        public static void Close(this StreamWriter streamWriter) => streamWriter.Dispose();
        public static void Close(this UdpClient client) => client.Dispose();
        public static void Close(this WebResponse response) => response.Dispose();
        public static void Close(this XmlWriter xmlWriter) => xmlWriter.Dispose();

        public static Attribute[] GetCustomAttributes(this Type type, Type other, bool inherit) => type.GetTypeInfo().GetCustomAttributes(other, inherit).ToArray();
        public static bool IsAssignableFrom(this Type type, Type other) => type.GetTypeInfo().IsAssignableFrom(other.GetTypeInfo());
        public static bool IsSubclassOf(this Type type, Type t) => type.GetTypeInfo().IsSubclassOf(t);

        public static string ToLower(this string s, CultureInfo cultureInfo) => cultureInfo.TextInfo.ToLower(s);
        public static string ToUpper(this string s, CultureInfo cultureInfo) => cultureInfo.TextInfo.ToUpper(s);
    }
}

#endif
