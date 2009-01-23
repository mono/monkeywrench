#!/bin/bash -ex

#
# Script to start xsp2 on port 8123 and send all output to /tmp/xsp2.log
#

pushd .

cd `dirname $0`

source config.sh

make database

MONO_OPTIONS=--debug xsp2 --port 8123 --root $BUILDER_CONFIG/web  --nonstop >> /tmp/xsp2.log 2>&1

popd

