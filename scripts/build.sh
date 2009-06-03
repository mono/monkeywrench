#!/bin/bash -ex

#
# Script to execute builder
#

make build -C `dirname $0`\..
