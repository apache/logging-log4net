#!/bin/bash

MY_PATH="`dirname \"$0\"`"              # relative
MY_PATH="`( cd \"$MY_PATH\" && pwd )`"  # absolutized and normalized
if [ -z "$MY_PATH" ] ; then
  # error; for some reason, the path is not accessible
  # to the script (e.g. permissions re-evaled after suid)
  exit 1  # fail
fi

NAME="builder-netstandard"
JENKINS_UID=$2
if [ -z "$JENKINS_UID" ] ; then
  JENKINS_UID="`stat -c \"%u\" $0`"
fi

JENKINS_GID=$3
if [ -z "$JENKINS_GID" ] ; then
  JENKINS_GID="`stat -c \"%g\" $0`"
fi

docker build --build-arg JENKINS_UID=$JENKINS_UID --build-arg JENKINS_GID=$JENKINS_GID --tag $NAME:latest $MY_PATH/$NAME

