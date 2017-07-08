#!/bin/bash
echo "Setting up user '$USER' that maps to uid $(id -u)"

export HOME=`pwd`
groupadd --gid `id -g` $USER
useradd --home-dir "$HOME" --shell /bin/bash --uid `id -u` --gid `id -g` --groups `id -g` -p -M "$USER"
