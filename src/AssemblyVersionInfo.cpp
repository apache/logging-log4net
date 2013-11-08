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

#using <mscorlib.dll>

using namespace System::Reflection;
using namespace System::Runtime::CompilerServices;

//
// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the value or you can default the Revision and Build Numbers 
// by using the '*' as shown below:

[assembly: AssemblyVersionAttribute("1.2.13.0")];
[assembly: AssemblyInformationalVersionAttribute("1.2")];

#if !NETCF
#if !SSCLI
[assembly: AssemblyFileVersionAttribute("1.2.13.0")]
#endif
#endif

//
// Shared assembly settings
//

[assembly: AssemblyCompany("The Apache Software Foundation")];
[assembly: AssemblyCopyright("Copyright 2004-2013 The Apache Software Foundation.")];
[assembly: AssemblyTrademark("Apache and Apache log4net are trademarks of The Apache Software Foundation")];
