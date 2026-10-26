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

# Apache log4net: a logging library for .NET, built and tested on Linux with the .NET 10 SDK.
# The net462 targets compile against the Microsoft.NETFramework.ReferenceAssemblies packages the SDK
# references implicitly, so no Mono is needed. On Linux the test projects target net10.0 only.
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble
ENV TZ=Etc/UTC \
    DOTNET_NOLOGO=true \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=true

# The documents SECURITY.md links, fetched now because the audit runs offline (see threat_model.md).
ADD https://raw.githubusercontent.com/apache/logging-site/refs/heads/main/src/site/antora/modules/ROOT/pages/_threat-model-common.adoc /opt/logging-security/
ADD https://raw.githubusercontent.com/apache/logging-site/refs/heads/main/src/site/antora/modules/ROOT/pages/security/faq.adoc /opt/logging-security/
ADD https://logging.apache.org/cyclonedx/vdr.xml /opt/logging-security/

# scanner contract: the checkout lives inside the image, at /src
COPY . /src
WORKDIR /src

# Restore every package now (the audit runs offline), then build the solution in Debug, with portable PDBs.
# The solution builds with 0 warnings and treats nullability warnings as errors, so a failure here is real.
RUN dotnet restore src/log4net.sln \
    && dotnet build src/log4net.sln -c Debug --no-restore

# Run the unit tests so the image is known to work, without letting a failing test abort the build.
# Afterwards `dotnet test src/log4net.Tests/log4net.Tests.csproj --no-build --filter <name>` works offline.
RUN (dotnet test src/log4net.Tests/log4net.Tests.csproj -c Debug --no-build \
      || echo "WARNING: log4net.Tests reported failures") \
    && (dotnet test src/log4net.Ext.Mail.Tests/log4net.Ext.Mail.Tests.csproj -c Debug --no-build \
      || echo "WARNING: log4net.Ext.Mail.Tests reported failures")
