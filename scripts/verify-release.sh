#!/bin/bash
set -e

if ! which unzip >/dev/null 2>&1; then
  echo "The 'unzip' utility is required, but was not found in your path" >&2
  exit 1
fi

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

cd src