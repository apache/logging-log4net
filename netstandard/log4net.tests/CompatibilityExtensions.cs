using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace log4net
{
    internal static class CompatibilityExtensions
    {
        public static void Close(this Mutex mutex) => mutex.Dispose();
        public static void Close(this Stream stream) => stream.Dispose();
        public static void Close(this StreamReader streamReader) => streamReader.Dispose();

        public static ConstructorInfo GetConstructor(this Type type, BindingFlags bindingAttr, object binder, Type[] types, object[] modifiers)
        {
            return type.GetConstructor(types);
        }
    }
}
