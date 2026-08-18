#!/bin/bash
set -euo pipefail

if ! which unzip >/dev/null 2>&1; then
  echo "The 'unzip' utility is required, but was not found in your path" >&2
  exit 1
fi

TARGET_DIR="${1:-}"
if test -z "$TARGET_DIR"; then
  TARGET_DIR="$(pwd)"
fi
cd "$TARGET_DIR"

# Everything that is not a hash, a signature or the key file has to be covered by both. Driving the
# checks from the artifacts, rather than from the .sha512 and .asc files that happen to be present,
# is what turns a missing signature into a failure instead of one loop iteration fewer.
shopt -s nullglob
artifacts=()
for file in *; do
  case "$file" in
    *.asc|*.sha512|KEYS) continue ;;
  esac
  test -f "$file" || continue
  artifacts+=("$file")
done

if test ${#artifacts[@]} -eq 0; then
  echo "No artifacts to verify in $TARGET_DIR" >&2
  exit 1
fi

for file in "${artifacts[@]}"; do
  if test ! -f "$file.sha512"; then
    echo "$file: no $file.sha512 to check it against" >&2
    exit 1
  fi
  sha512sum --check "$file.sha512"
done

wget https://downloads.apache.org/logging/KEYS

# A key ring of its own, holding only the downloaded KEYS. Importing into the default key ring
# would accept a signature from any key this machine already has, not only from a key in the
# Logging Services KEYS file.
keyring_dir="$(mktemp -d)"
trap 'rm -rf "$keyring_dir"' EXIT
gpg --no-default-keyring --keyring "$keyring_dir/logging-keys.gpg" --batch --quiet --import KEYS

for file in "${artifacts[@]}"; do
  if test ! -f "$file.asc"; then
    echo "$file: no $file.asc to verify it with" >&2
    exit 1
  fi
  gpg --no-default-keyring --keyring "$keyring_dir/logging-keys.gpg" --batch --verify "$file.asc" "$file"
done

mkdir -p src
cd src
unzip -q -o ../*source*.zip

# Do not "cd" here to position the reviewer: this runs as "bash ./verify-release.sh", so a child
# process, and the change would be lost. The step stays in release-review.adoc, typed by the reviewer.
echo "Sources extracted to $(pwd)"
