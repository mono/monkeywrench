default: all
	@:

SVN_EXEC_FILES = $(wildcard scripts/*.sh)
SVN_NO_EXEC_FILES = \
	$(wildcard */*.cs)			\
	$(wildcard */*/*.cs)		\
	$(wildcard */*/*/*.cs)		\
	$(wildcard */*.sql)			\
	$(wildcard */*.aspx)		\
	$(wildcard */*.js)			\
	$(wildcard web/doc/*)		\
	$(wildcard */*.css)			\
	$(wildcard */*.master)		\
	$(wildcard */web.config)	\
	$(wildcard */Web.config)	\
	$(wildcard */*.csproj)		\
	Makefile					\
	$(wildcard */Makefile)		\
	LICENSE						\
	README					

SVN_EOL_FILES = $(SVN_EXEC_FILES) $(SVN_NO_EXEC_FILES)

svn:
	unix2dos $(SVN_EOL_FILES)
	dos2unix $(SVN_EXEC_FILES)
	svn ps svn:eol-style native $(SVN_EOL_FILES)
	svn pd svn:executable $(SVN_NO_EXEC_FILES)
	svn ps svn:executable $(SVN_EXEC_FILES)

all clean install:
	@$(MAKE) -C Newtonsoft.Json $@
	@$(MAKE) -C MonkeyWrench $@
	@$(MAKE) -C MonkeyWrench.DataClasses $@
	@$(MAKE) -C MonkeyWrench.Database $@
	@$(MAKE) -C MonkeyWrench.Database.Manager $@
	@$(MAKE) -C MonkeyWrench.Scheduler $@
	@$(MAKE) -C MonkeyWrench.Builder $@
	@$(MAKE) -C MonkeyWrench.Web.UI $@
	@$(MAKE) -C MonkeyWrench.Web.WebService $@
	@$(MAKE) -C MonkeyWrench.CmdClient $@

publish: install

wsdl: publish
	$(MAKE) -C MonkeyWrench.Web.WebService $@

build:
	@$(MAKE) -C MonkeyWrench all
	@$(MAKE) -C MonkeyWrench.DataClasses all
	@$(MAKE) -C MonkeyWrench.Database all
	@$(MAKE) -C MonkeyWrench.Database.Manager all
	@$(MAKE) -C MonkeyWrench.Builder all
	@$(MAKE) -C MonkeyWrench.CmdClient all
	mono --debug class/lib/MonkeyWrench.Builder.exe

schedule update: all
	mono --debug class/lib/MonkeyWrench.Scheduler.exe

web: all publish
	scripts/web.sh

generate:
	$(MAKE) -C MonkeyWrench.DataClasses $@

clean-large-objects compress-files execute-deletion-directives move-files-to-file-system move-files-to-database: all
	mono --debug class/lib/MonkeyWrench.Database.Manager.exe --$@

RELEASE_FILENAME=releases/MonkeyWrench.`grep AssemblyVersion MonkeyWrench/AssemblyInfo.cs | awk -F'\"' '{print $$2}'`.zip
release: all
	@mkdir -p releases
	rm -f $(RELEASE_FILENAME)
	zip -j -9 $(RELEASE_FILENAME) class/lib/* scripts/build

#
# Test targets:
#   test or tests: make the test binary
#   run-test or run-tests: run the tests
#	test-db-psql: check the test database with psql
#	test-web: starts up xsp2 on localhost:8123 allowing you to view the current state of the test database
#

tests test: all
	$(MAKE) -C MonkeyWrench.Test $@

run-test run-tests: tests
	# git creates read-only files, which managed code can't delete (an UnauthorizedException is thrown)
	# delete any test directories right away
	rm -Rf /tmp/MonkeyWrench.Test
	mono --debug class/lib/MonkeyWrench.Test.exe

test-db-start:
	PGPORT=5678 PGDATA=/tmp/MonkeyWrench.Test/db/data/db/data pg_ctl start -w

test-db-stop:
	-PGPORT=5678 PGDATA=/tmp/MonkeyWrench.Test/db/data/db/data pg_ctl stop

test-db-psql:
	$(MAKE) test-db-start
	-(PGPORT=5678 psql --user builder --db builder)
	-$(MAKE) test-db-stop

test-web:
	$(MAKE) test-db-start
	-MONKEYWRENCH_CONFIG_FILE=/tmp/MonkeyWrench.Test/MonkeyWrench.xml MONO_OPTIONS=--debug xsp2 --port 8123 --root $(PWD) --applications /WebServices:$(PWD)/MonkeyWrench.Web.WebService/,/:$(PWD)/MonkeyWrench.Web.UI
	#>> /tmp/MonkeyWrench.Test/xsp2.log 2>&1
	-$(MAKE) test-db-stop


