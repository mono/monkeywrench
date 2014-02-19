SOURCES = \
	$(wildcard *.cs)

TOPDIR = ..

LIBRARY = MonkeyWrench.dll
OUT_DIR = $(TOPDIR)/class/lib
OUT = $(OUT_DIR)/$(LIBRARY)

BUILDABLE_REFS = \
	$(OUT_DIR)/Npgsql.dll

REFERENCES = \
	$(foreach ref,$(BUILDABLE_REFS),-r:$(ref))	\
	-r:System.Data.dll

all:
	make generate-assembly-info
	make $(OUT)

clean:
	@rm -f $(OUT)*
	@rm -f AssemblyInfo.tmp

publish install: all
	@:

generate-assembly-info:
	@cp AssemblyInfo.cs.in AssemblyInfo.tmp
	@sed -e "s/.*AssemblyDescription.*/[assembly: AssemblyDescription (@\"MonkeyWrench (branch: `git branch | grep "^[*] " | sed 's/^[*] //'` commit #`git log --pretty=format:''|wc -l` sha: `git log --pretty=format:'%h' -1` date: `git log --pretty=format:'%ci' -1`) subject: `git log --pretty=format:'%s' -1 | sed 's_\\"_\\"\\"_g' | sed 's_/__g'`)\")]/" \
	     -e "s/.*AssemblyVersion.*/[assembly: AssemblyVersion (\"1.0.0.`git log --pretty=format:''|wc -l`\")]/" AssemblyInfo.cs.in > AssemblyInfo.tmp
	@(if ! diff -u AssemblyInfo.cs AssemblyInfo.tmp > /dev/null 2>&1; then echo Updating AssemblyInfo.cs; cp -p AssemblyInfo.tmp AssemblyInfo.cs; else echo AssemblyInfo.cs is up to date; fi)

$(OUT): $(SOURCES) $(BUILDABLE_REFS)
	@echo "    DMCS $(LIBRARY)"
	@dmcs -target:library -debug:full -out:$@ $(SOURCES) $(REFERENCES)
