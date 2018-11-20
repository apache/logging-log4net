#!/bin/bash

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

MY_PATH="`dirname \"$0\"`"              # relative
MY_PATH="`( cd \"$MY_PATH\" && pwd )`"  # absolutized and normalized
if [ -z "$MY_PATH" ] ; then
  # error; for some reason, the path is not accessible
  # to the script (e.g. permissions re-evaled after suid)
  exit 1  # fail
fi

NAME="builder-netstandard-1.3"
JENKINS_UID=$2
if [ -z "$JENKINS_UID" ] ; then
  JENKINS_UID="`stat -c \"%u\" $0`"
fi

JENKINS_GID=$3
if [ -z "$JENKINS_GID" ] ; then
  JENKINS_GID="`stat -c \"%g\" $0`"
fi

docker build --build-arg JENKINS_UID=$JENKINS_UID --build-arg JENKINS_GID=$JENKINS_GID --tag $NAME:latest $MY_PATH/$NAME
