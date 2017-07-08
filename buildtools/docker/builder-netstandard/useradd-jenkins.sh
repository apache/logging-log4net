#!/bin/bash
USER="jenkins"
HOME="/home/jenkins"
groupadd --gid `id -g` $USER
useradd --home-dir "$HOME" --shell /bin/bash --uid `id -u` --gid `id -g` --groups `id -g` -p -M "$USER"
