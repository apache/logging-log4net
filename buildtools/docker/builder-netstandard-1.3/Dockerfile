# Licensed to the Apache Software Foundation (ASF) under one or more
# contributor license agreements.  See the NOTICE file distributed with
# this work for additional information regarding copyright ownership.
# The ASF licenses this file to You under the Apache License, Version 2.0
# (the "License"); you may not use this file except in compliance with
# the License.  You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

FROM microsoft/dotnet:2-sdk-jessie

# define arguments passed in as environment variables
ARG JENKINS_UID
ARG JENKINS_GID

# set the maintainer of this docker image
MAINTAINER "dev@logging.apache.org"

# add custom workspace that can be written by anyone
RUN mkdir -p /var/workspaces && chmod 0777 /var/workspaces

# add jenkins user that maps to the custom workspace
RUN groupadd -r -g $JENKINS_GID jenkins-slave
RUN useradd --base-dir /var/workspaces --create-home --shell /bin/bash --uid $JENKINS_UID --gid $JENKINS_GID --groups $JENKINS_GID -p -M jenkins

# install dependencies
RUN apt-get update && apt-get install -y nant git
