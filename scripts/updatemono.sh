#!/bin/bash -ex

#
# this script will checkout and build a mono/mcs for the buildbot to use (so that nothing clashes with whatever we're testing)
#

# to avoid weird differences between bots, we put a revision here so that all bots use the same revision
REVISION=132691

source `dirname $0`/config.sh

B_ROOT=$BUILDER_DATA/bot-dependencies
mkdir -p $B_ROOT/install
mkdir -p $B_ROOT/src

cd $B_ROOT/src
svn co svn://anonsvn.mono-project.com/source/trunk/mono mono -r $REVISION
svn co svn://anonsvn.mono-project.com/source/trunk/mcs  mcs  -r $REVISION

cd mono
./autogen.sh --prefix=$B_ROOT/install
make
make install

