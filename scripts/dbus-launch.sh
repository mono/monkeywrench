#!/bin/bash -ex


DIR=`dirname $0`
dbus-launch > $DIR/dbus-launch.log

echo -n "export " > $DIR/dbus
grep DBUS_SESSION_BUS_ADDRESS $DIR/dbus-launch.log >> $DIR/dbus
echo -n "export " >> $DIR/dbus
grep DBUS_SESSION_BUS_PID $DIR/dbus-launch.log >> $DIR/dbus

rm $DIR/dbus-launch.log
