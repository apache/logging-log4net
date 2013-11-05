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

using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: System.Runtime.InteropServices.ComVisible(false)]

//
// log4net is CLS compliant
//
[assembly: System.CLSCompliant(true)]

#if (!NETCF)
//
// If log4net is strongly named it still allows partially trusted callers
//
[assembly: System.Security.AllowPartiallyTrustedCallers]
#endif

#if FRAMEWORK_4_0_OR_ABOVE
//
// Allows partial trust applications (e.g. ASP.NET shared hosting) on .NET 4.0 to work
// given our implementation of ISerializable.
//
[assembly: System.Security.SecurityRules(System.Security.SecurityRuleSet.Level1)]
#endif

//
// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//

#if DOTNET
#if FRAMEWORK_4_0_OR_ABOVE
#if CLIENT_PROFILE
[assembly: AssemblyTitle("Apache log4net for .NET Framework 4.0 Client Profile")]
#else
[assembly: AssemblyTitle("Apache log4net for .NET Framework 4.0")]
#endif // Client Profile
#elif FRAMEWORK_3_5_OR_ABOVE
#if CLIENT_PROFILE
[assembly: AssemblyTitle("Apache log4net for .NET Framework 3.5 Client Profile")]
#else
[assembly: AssemblyTitle("Apache log4net for .NET Framework 3.5")]
#endif // Client Profile
#else
[assembly: AssemblyTitle("Apache log4net for .NET Framework 2.0")]
#endif // FW 4.0 
#elif NETCF
[assembly: AssemblyTitle("Apache log4net for .NET Compact Framework 2.0")]
#elif MONO
[assembly: AssemblyTitle("Apache log4net for Mono 2.0")]
#endif

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Retail")]
#endif

[assembly: AssemblyProduct("log4net")]
[assembly: AssemblyDefaultAlias("log4net")]
[assembly: AssemblyCulture("")]		
		
