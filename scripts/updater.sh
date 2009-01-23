#!/bin/bash -ex

#
# Script to execute scheduler
#

pushd .

cd `dirname $0`

source config.sh

mkdir -p bin
make updater
mono --debug bin/Builder.Updater.exe "$@"
 
popd
