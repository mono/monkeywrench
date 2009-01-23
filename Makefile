default:
	@:

SVN_EXEC_FILES = $(wildcard scripts/*.sh)
SVN_NO_EXEC_FILES = \
	$(wildcard Builder/*.cs)	\
	$(wildcard */*.sql)			\
	$(wildcard web/*.cs)		\
	$(wildcard web/*.aspx)		\
	$(wildcard web/*.js)		\
	$(wildcard web/doc/*)		\
	$(wildcard web/*.css)		\
	$(wildcard web/*.master)	\
	web/web.config				\
	Makefile				\
	*/Makefile				\
	LICENSE					\
	README					
	
SVN_EOL_FILES = $(SVN_EXEC_FILES) $(SVN_NO_EXEC_FILES)

svn:
	unix2dos $(SVN_EOL_FILES)
	dos2unix $(SVN_EXEC_FILES)
	svn ps svn:eol-style native $(SVN_EOL_FILES)
	svn pd svn:executable $(SVN_NO_EXEC_FILES)
	svn ps svn:executable $(SVN_EXEC_FILES)
	
