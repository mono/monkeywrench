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
	
all clean publish install:
	$(MAKE) -C MonkeyWrench $@
	$(MAKE) -C MonkeyWrench.DataClasses $@
	$(MAKE) -C MonkeyWrench.Database $@
	$(MAKE) -C MonkeyWrench.Database.Manager $@
	$(MAKE) -C MonkeyWrench.Scheduler $@
	$(MAKE) -C MonkeyWrench.Builder $@
	$(MAKE) -C MonkeyWrench.Web.UI $@
	$(MAKE) -C MonkeyWrench.Web.WebService $@

wsdl:
	$(MAKE) -C MonkeyWrench.Web.WebService $@

build: all
	mono --debug class/lib/MonkeyWrench.Builder.exe

schedule update: all
	mono --debug class/lib/MonkeyWrench.Scheduler.exe

web: all
	scripts/web.sh

generate:
	$(MAKE) -C MonkeyWrench.DataClasses $@

clean-large-objects compress-files execute-deletion-directives move-files-to-file-system move-files-to-database: all
	mono --debug class/lib/MonkeyWrench.Database.Manager.exe --$@

zip:
	echo "Not implemented yet"
	exit 1

	

