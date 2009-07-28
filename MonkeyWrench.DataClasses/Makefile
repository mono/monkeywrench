SOURCES = \
	$(wildcard *.cs)	\
	$(wildcard */*.cs)

TOPDIR = ..

LIBRARY = MonkeyWrench.DataClasses.dll
OUT_DIR = $(TOPDIR)/class/lib
OUT = $(OUT_DIR)/$(LIBRARY)

BUILDABLE_REFS = \
	$(OUT_DIR)/Npgsql.dll	\
	$(OUT_DIR)/MonkeyWrench.dll

REFERENCES = \
	$(foreach ref,$(BUILDABLE_REFS),-r:$(ref))	\
	-r:System.Data.dll	\
	-r:System.Web.dll	\
	-r:System.Web.Services.dll

$(OUT): $(SOURCES) $(BUILDABLE_REFS)
	gmcs -target:library -debug:full -out:$@ $(SOURCES) $(REFERENCES)

generate: ../scripts/database.sql CustomTypes.sql
	gmcs SqlToCSharp.cs -out:SqlToCSharp.exe -debug+
	cat ../scripts/database.sql > tmp
	cat CustomTypes.sql >> tmp
	mono --debug SqlToCSharp.exe tmp
	rm -f tmp
	rm SqlToCSharp.exe*
	unix2dos Database/*.generated.cs

all: $(OUT)

clean:
	rm -f $(OUT)*

publish install: all
	@: