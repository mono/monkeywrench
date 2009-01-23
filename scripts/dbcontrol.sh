#!/bin/bash -ex

#
# use this script to control the database
#

# options:
# start: starts the db. if the database doesn't exist, it is created.
# stop: stops the db. if the database doesn't exist, this option does nothing.
# create: creates the db (and starts it). if the database already exists, an error is returned.
# delete: stops the db and deletes all files. if the database doesn't exist, this option does nothing.
#
# if no argument is specified, it defaults to 'start' if the database files are found, otherwise 'create'
#

SCRIPT_DIR=`dirname $0`

echo SCRIPT_DIR=$SCRIPT_DIR

source $SCRIPT_DIR/config.sh

# derive some paths from the config sourced above
export BUILDER_DATA_DB=$BUILDER_DATA/db
export BUILDER_DATA_DB_DATA=$BUILDER_DATA_DB/data
export BUILDER_DATA_DB_LOGS=$BUILDER_DATA_DB/logs

# some postgres variables
export PGDATA=$BUILDER_DATA_DB_DATA

# verify variables
if [[ "x$2" != "x" ]]; then
	echo Too many arguments, only one is possible.
	exit 1
fi

# check if there already is a database created
EXISTS=0
if [[ -e $BUILDER_DATA_DB/data/PG_VERSION ]]; then
	EXISTS=1
fi

# parse commandline arguments
case "$1" in 
	start)
		if [[ "x$EXISTS" == "x0" ]]; then
			CMD=create
		else
			CMD=start
		fi
		;;
	stop)
		if [[ "x$EXISTS" == "x0" ]]; then
			#the database doesn't exist, do nothing
			echo "The database doesn't exist, there is nothing to stop"
			exit 0
		else
			CMD=stop
		fi
		;;
	create)
		if [[ "x$EXISTS" == "x0" ]]; then
			CMD=create
		else
			echo "The database already exists, to create a new one delete the old one first."
			exit 1
		fi
		;;
	delete)
		if [[ "x$EXISTS" == "x0" ]]; then
			#the database doesn't exist, do nothing
			echo "The database doesn't exist, there is nothing to delete"
			exit 0
		else
			CMD=delete
		fi
		;;
	"")
		if [[ "x$EXISTS" == "x0" ]]; then
			CMD=create
		else
			CMD=start
		fi
		;;
	*)
		echo Invalid option: $1
		exit 1
		;;
esac

# do the work
case "$CMD" in
	stop)
		pg_ctl stop
		;;
	delete)
		# try to stop the database first, this may fail if the database has already been stopped
		pg_ctl stop || true
		rm -Rf $BUILDER_DATA_DB
		;;
	create)
		# create the directories
		mkdir -p $BUILDER_DATA_DB
		mkdir -p $BUILDER_DATA_DB_DATA
		mkdir -p $BUILDER_DATA_DB_LOGS
		initdb
		# start the db
		pg_ctl -l $BUILDER_DATA_DB_LOGS/logfile start
		# wait a bit for the db to finish starting up
		sleep 2
		# create the user 'builder' owner is the user 'builder'
		createuser -s -d -r -e builder
		# create the database
		psql --user builder --db template1 --file database.sql
		;;
	start)
		# start the database
		pg_ctl -l $BUILDER_DATA_DB_LOGS/logfile start
		;;
	*)
		echo "Invalid command: $CMD"
		exit 1
		;;
esac
