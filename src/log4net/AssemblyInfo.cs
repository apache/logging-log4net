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

// log4net makes use of static methods which cannot be made com visible
[assembly: System.Runtime.InteropServices.ComVisible(false)]
[assembly: System.CLSCompliant(true)]
// If log4net is strongly named it still allows partially trusted callers
[assembly: System.Security.AllowPartiallyTrustedCallers]

// Allows partial trust applications (e.g. ASP.NET shared hosting) on .NET 4.0 to work
// given our implementation of ISerializable.
[assembly: System.Security.SecurityRules(System.Security.SecurityRuleSet.Level1)]