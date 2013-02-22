#!/bin/bash -ex

#
# use this script to control the database
#

# options:
# start: starts the db. if the database doesn't exist, it is created.
# stop: stops the db. if the database doesn't exist, this option does nothing.
# create: creates the db (and starts it). if the database already exists, an error is returned.
# dropdata: drops all the data from the db (it really drops the entire database and recreates it)
# delete: stops the db and deletes all files. if the database doesn't exist, this option does nothing.
# backup-configuration: backs up the configuration of the monkeywrench instance (all the tables except: file, revision, revisionwork, work, workfile, login. You loose all work done by all buildbots, but nothing you've done yourself)
#                       The data will be written to the files backup-configuration/backup-configuration.<table>.sql.
# restore-configuration: restore what was backed up with backup-configuration. Note that the database must not have any data it in (just created with "dbcontrol.sh create && dbcontrol.sh start")
#

# derive some paths from the config sourced above
PREFIX=
if test -d /Library/PostgreSQL/9.0/bin; then
	PREFIX=/Library/PostgreSQL/9.0/bin/
fi


SCRIPT_DIR=`dirname $0`
export BUILDER_DATA_DB=`$SCRIPT_DIR/getcfgvar.pl /MonkeyWrench/Configuration/DatabaseDirectory`
export BUILDER_DATA_PORT=`$SCRIPT_DIR/getcfgvar.pl /MonkeyWrench/Configuration/DatabasePort`
export BUILDER_DATA_DB_DATA=$BUILDER_DATA_DB/data
export BUILDER_DATA_DB_LOGS=$BUILDER_DATA_DB/logs

# some postgres variables
export PGPORT=$BUILDER_DATA_PORT
export PGDATA=$BUILDER_DATA_DB_DATA

# verify variables
if [[ "x$1" == "x" || "x$2" != "x" ]]; then
	echo "Syntax: $0 [start|stop|create|dropdata|delete|pgsql|backup-configuration|restore-configuration]"
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
	dropdata)
		if [[ "x$EXISTS" == "x0" ]]; then
			#the database doesn't exist, do nothing
			echo "The database doesn't exist, there is nothing to drop"
			exit 0
		else
			CMD=dropdata
		fi
		;;
	psql)
		CMD=psql
		;;
	backup-configuration)
		CMD=backup-configuration
		;;
	restore-configuration)
		CMD=restore-configuration
		;;
	*)
		echo "Invalid option: $1 (must be either start, stop, create, dropdata, delete or psql)"
		exit 1
		;;
esac

CONFIGURATION_TABLES="host masterhost lane environmentvariable filedeletiondirective lanedeletiondirective lanedependency command hostlane lanefile lanefiles person"

# do the work
case "$CMD" in
	stop)
		${PREFIX}pg_ctl stop -m fast
		;;
	delete)
		# try to stop the database first, this may fail if the database has already been stopped
		${PREFIX}pg_ctl stop || true
		rm -Rf $BUILDER_DATA_DB
		;;
	create)
		# create the directories
		mkdir -p $BUILDER_DATA_DB
		mkdir -p $BUILDER_DATA_DB_DATA
		mkdir -p $BUILDER_DATA_DB_LOGS
		${PREFIX}initdb
		# start the db
		${PREFIX}pg_ctl -w -l $BUILDER_DATA_DB_LOGS/logfile start
		# wait a bit for the db to finish starting up
		sleep 1
		# create the user 'builder' owner is the user 'builder'
		if [[ "x$USER" != "xbuilder" ]]; then
			${PREFIX}createuser -s -d -r -e builder
		fi
		# create the database
		${PREFIX}psql --user builder --db template1 --file $SCRIPT_DIR/database.sql
		;;
	dropdata)
		# drop the database
		echo "DROP DATABASE IF EXISTS builder;" | psql --user builder --db template1
		# recreate it
		${PREFIX}psql --user builder --db template1 --file $SCRIPT_DIR/database.sql
		;;
	start)
		# start the database
		PGDATA=$PGDATA ${PREFIX}pg_ctl -l $BUILDER_DATA_DB_LOGS/logfile start
		;;
	psql)
		${PREFIX}psql --user builder --db builder
		;;
	backup-configuration)
		# -a: only data, --column-inserts: add column names to INSERT statements, -d: use INSERT instead of COPY, -F: use postgre custom format
		cd $SCRIPT_DIR
		mkdir -p backup-configuration
		cd backup-configuration
		for i in $CONFIGURATION_TABLES; do
			pg_dump -a --column-inserts -U builder -t $i builder > backup-configuration.$i.sql 
		done
		zip backup-configuration.zip backup-configuration.*.sql
		N=`date +"%Y-%m-%d"`
		mv backup-configuration.zip backup-configuration.$N.zip
		;;
	restore-configuration)
		cd $SCRIPT_DIR/backup-configuration
		# we need to temporarily disable the foreign key the Lanefile table has on one of its own fields, since pg_dump can dump records in any order
		echo "ALTER TABLE Lanefile DROP CONSTRAINT lanefile_original_id_fkey;" | psql --user builder --db builder
		# when creating a new database, we automatically add an admin user. Remove that user here.
		echo "DELETE FROM Person WHERE login = 'admin';" | psql --user builder --db builder
		for i in $CONFIGURATION_TABLES; do
			cat backup-configuration.$i.sql | psql --user builder --db builder
		done
		echo "ALTER TABLE Lanefile ADD CONSTRAINT lanefile_original_id_fkey FOREIGN KEY (original_id) REFERENCES lanefile (id);"
		;;
	*)
		echo "Invalid command: $CMD"
		exit 1
		;;
esac
