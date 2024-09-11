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

FROM mono:latest

RUN apt-get update \
  && apt-get upgrade -y \
  && apt-get install -y wget \
  && apt-get install -y tree \
  && wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh \
  && chmod +x ./dotnet-install.sh \
  && ./dotnet-install.sh --channel 8.0
ENV DOTNET_NOLOGO=true
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true
ENV DOTNET_ROOT=/root/.dotnet
ENV PATH="$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools"
  
ADD . /logging-log4net
RUN dotnet restore /logging-log4net/src/log4net.sln
RUN dotnet build -c Release /logging-log4net/src/log4net.sln
CMD [/bin/bash]