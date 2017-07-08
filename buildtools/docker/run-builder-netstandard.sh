#!/bin/bash

MY_PATH="`dirname \"$0\"`"              # relative
MY_PATH="`( cd \"$MY_PATH\" && pwd )`"  # absolutized and normalized
if [ -z "$MY_PATH" ] ; then
  # error; for some reason, the path is not accessible
  # to the script (e.g. permissions re-evaled after suid)
  echo "Path is not accessible to the script"
  exit 1  # fail
fi

WORKING_DIR="$1"
if [ -z "$WORKING_DIR" ]; then
	WORKING_DIR=$(pwd)
fi

$MY_PATH/run.sh "builder-netstandard" $WORKING_DIR

