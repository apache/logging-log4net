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

# Driven from the artifacts, not from the .sha512 and .asc files present, so a missing one fails
# instead of being one loop iteration fewer.
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

# A home of its own, so only the downloaded KEYS can verify. Not --keyring: gpg ignores that where
# common.conf sets use-keyboxd. Assigned before exporting, or a failed mktemp would go unnoticed
# and an empty GNUPGHOME means the reviewer's own home.
GNUPGHOME="$(mktemp -d)"
export GNUPGHOME
# "|| true", or a failing gpgconf aborts the trap before the directory is removed.
trap 'gpgconf --kill all >/dev/null 2>&1 || true; rm -rf "$GNUPGHOME"' EXIT

# -O, and never the KEYS next to the artifacts: plain "wget URL" refuses to overwrite, so a planted
# KEYS would stay and the download would land in KEYS.1.
wget -O "$GNUPGHOME/KEYS" https://downloads.apache.org/logging/KEYS
gpg --batch --quiet --import "$GNUPGHOME/KEYS"

for file in "${artifacts[@]}"; do
  if test ! -f "$file.asc"; then
    echo "$file: no $file.asc to verify it with" >&2
    exit 1
  fi
  gpg --batch --verify "$file.asc" "$file"
done

mkdir -p src
cd src
unzip -q -o ../*source*.zip

# Do not "cd" here to position the reviewer: this runs as "bash ./verify-release.sh", so a child
# process, and the change would be lost. The step stays in release-review.adoc, typed by the reviewer.
echo "Sources extracted to $(pwd)"
