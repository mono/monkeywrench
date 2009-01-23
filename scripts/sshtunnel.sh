#!/bin/bash -ex

pushd .

cd `dirname $0`

source config.sh

ssh -L $BUILDER_DATABASE_PORT:$BUILDER_DATABASE_REMOTE_HOST:$BUILDER_DATABASE_REMOTE_PORT $BUILDER_DATABASE_REMOTE_USER@$BUILDER_DATABASE_REMOTE_HOST -o ExitOnForwardFailure=yes  -o ServerAliveInterval=60 -o PasswordAuthentication=no -t -t -i ~/.ssh/id_dsa_builder -o CheckHostIP=no

popd
