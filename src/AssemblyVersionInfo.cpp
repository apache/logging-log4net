//
// Copyright 2001-2006 The Apache Software Foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
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

[assembly: AssemblyVersionAttribute("1.2.10.0")];
[assembly: AssemblyInformationalVersionAttribute("1.2")];

#if !NETCF
#if !SSCLI
[assembly: AssemblyFileVersionAttribute("1.2.10.0")]
#endif
#endif

//
// Shared assembly settings
//

[assembly: AssemblyCompany("The Apache Software Foundation")];
[assembly: AssemblyCopyright("Copyright 2001-2006 The Apache Software Foundation.")];
[assembly: AssemblyTrademark("Copyright 2001-2006 The Apache Software Foundation.")];
