#!/bin/bash -ex

#
# Script to start xsp4 on port 8123 and send all output to /tmp/xsp2.log
#

XSP=/Library/Frameworks/Mono.framework/Commands/xsp4
if ! test -f $XSP; then
	XSP=xsp4
fi


pushd .

cd `dirname $0`

make -C .. all
#ROOT=`readlink -f $PWD/..`
ROOT=$PWD/..

export MONO_TLS_SESSION_CACHE_TIMEOUT=0
export PATH=$PATH:/Library/Frameworks/Mono.framework/Commands

ulimit -n 4096

( sleep 1 && curl http://localhost:8123/index.aspx >/dev/null ) &

MONO_OPTIONS="--debug $MONO_OPTIONS" $XSP --port 8123 --root $ROOT --applications /WebServices:$ROOT/MonkeyWrench.Web.WebService/,/:$ROOT/MonkeyWrench.Web.UI --nonstop

popd

