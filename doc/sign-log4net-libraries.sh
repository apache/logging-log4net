#!/bin/bash
# see https://infra.apache.org/release-signing#openpgp-ascii-detach-sig
DID_SOMETHING=0
for f in log4net*.nupkg log4net*.zip; do
  DID_SOMETHING=1
  echo "signing: $f"
  gpg --armor --output $f.asc --detach-sig $f
done

if test "$DID_SOMETHING" = "0"; then
  echo "No log4net artifacts found - are you sure you're in the right directory?"
  exit 2
fi
