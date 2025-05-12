#!/bin/bash
set -e

TARGET_DIR="$1";
if test -z "$TARGET_DIR"; then
  TARGET_DIR="$(pwd)"
fi

sha512sum --check *.sha512

wget https://downloads.apache.org/logging/KEYS
gpg --import -q KEYS
for f in $(find "$TARGET_DIR" -iname '*.asc'); do
  gpg --verify "$f"
done

mkdir -p src
cd src
unzip -q -o ../*source*.zip
