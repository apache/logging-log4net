#if NETCOREAPP1_0
#region Apache License
//
// Licensed to the Apache Software Foundation (ASF) under one or more 
// contributor license agreements. See the NOTICE file distributed with
// this work for additional information regarding copyright ownership. 
// The ASF licenses this file to you under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with 
// the License. You may obtain a copy of the License at
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
using System.Reflection;
using System.Threading;

namespace log4net
{
	/// <summary>
	/// Extension methods for simple API substitutions for compatibility with .NET Standard 1.3.
	/// </summary>
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
#endif
