#!/bin/bash -ex

#
# Script to start xsp2 on port 8123 and send all output to /tmp/xsp2.log
#

pushd .

cd `dirname $0`

make -C .. all
ROOT=`readlink -f $PWD/..`

MONO_OPTIONS="--debug $MONO_OPTIONS" xsp2 --port 8123 --root $ROOT --applications /WebServices:$ROOT/MonkeyWrench.Web.WebService/,/:$ROOT/MonkeyWrench.Web.UI --nonstop >> /tmp/xsp2.log 2>&1

popd

