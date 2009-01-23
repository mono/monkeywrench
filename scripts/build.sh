#!/bin/bash -ex

#
# Script to execute builder
#

pushd .

cd `dirname $0`

source config.sh

mkdir -p bin
make builder

mono --debug bin/Builder.Builder.exe "$@"

popd

