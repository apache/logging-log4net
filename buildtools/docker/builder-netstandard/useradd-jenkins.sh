#!/bin/bash
L_USER="jenkins"
L_HOME="/home/jenkins"
L_UID=10025
L_GID=12040
groupadd --gid $L_GID $USER
useradd --home-dir "$HOME" --shell /bin/bash --uid $L_UID --gid $L_GID --groups $L_GID -p -M $L_USER
