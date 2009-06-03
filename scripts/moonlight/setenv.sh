#!/bin/bash -ex

if [[ "x$BUILD_HOST" == "x" ]]; then
	echo You must source config.sh first.
	exit 1
fi

# 
# This script must be sourced while pwd is the directory of a revision.
# It will setup env variables mostly as the builder sees things.
# 
# Typical usage:
# > cd ~/moonbuilder/builder/scripts/
# > source config.sh
# > cd ~/moonbuilder/data/lanes/moon-trunk-2.0/123456/
# > source ~/moonbuilder/builder/scripts/setenv.sh
#

R=`basename $PWD`

if ! expr $R + 1 ; then
	echo Current directory has to be a revision number.
else
	I=$PWD

	source ~/moonbuilder/builder/scripts/config.sh
	
	export PKG_CONFIG_PATH=$I/install/lib/pkgconfig:$PKG_CONFIG_PATH
	export PATH=$I/install/bin:$PATH
	export LD_LIBRARY_PATH=$I/install/lib:$I/install/.mozilla/plugins/moonlight:/$LD_LIBRARY_PATH
	export DISPLAY=:7
fi

