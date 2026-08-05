# Licensed to the Apache Software Foundation (ASF) under one
# or more contributor license agreements.  See the NOTICE file
# distributed with this work for additional information
# regarding copyright ownership.  The ASF licenses this file
# to you under the Apache License, Version 2.0 (the
# "License"); you may not use this file except in compliance
# with the License.  You may obtain a copy of the License at

#     http://www.apache.org/licenses/LICENSE-2.0

# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

# MAINTAINER Jan Friedrich

# Ubuntu 24.04 (noble) with the .NET 10 SDK already installed - noble is the only Ubuntu
# variant published for .NET 10. The reference is fully qualified, so it needs no registry
# configuration and no login, unlike the short name it replaces.
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble
ENV TZ=Etc/UTC

# Mono is not required: the net4x targets are compiled against the
# Microsoft.NETFramework.ReferenceAssemblies packages that the .NET SDK references implicitly.
# Mono would only be needed to *execute* net4x assemblies, which this image does not do.

ENV DOTNET_NOLOGO=true
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true

ADD . /logging-log4net
RUN dotnet restore /logging-log4net/src/log4net.sln
RUN dotnet build -c Release /logging-log4net/src/log4net.sln
CMD ["/bin/bash"]