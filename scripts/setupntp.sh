#!/bin/bash -ex

# this script must be run as root

pushd .

echo `dirname $0`

if grep vmware /etc/ntp.conf; then
	echo You have the correct ntp.conf installed
	exit 0
fi

echo Installing ntp.conf

cp /etc/ntp.conf /etc/ntp.conf.moonbuilder-backup
cp ntp.conf.sample /etc/ntp.conf

# start the deamon
chkconfig ntp on

echo You need to restart the machine for changes to take effect.

popd 
