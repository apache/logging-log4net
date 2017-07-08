#!/bin/bash
USER_ID=${UID:-9001}

echo "Setting up user 'user' that maps to uid $USER_ID"
useradd --shell /bin/bash -u $USER_ID -o -c "" -m user
export HOME=/home/user

exec /usr/local/bin/gosu user "$@"
