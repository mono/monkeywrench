SOURCES = \
	$(wildcard *.cs)	\
	$(wildcard */*.cs)

TOPDIR = ..

LIBRARY = MonkeyWrench.Scheduler.exe
OUT_DIR = $(TOPDIR)/class/lib
OUT = $(OUT_DIR)/$(LIBRARY)

BUILDABLE_REFS = \
	$(OUT_DIR)/Npgsql.dll	\
	$(OUT_DIR)/MonkeyWrench.dll	\
	$(OUT_DIR)/MonkeyWrench.DataClasses.dll	\
	$(OUT_DIR)/MonkeyWrench.Database.dll

REFERENCES = \
	$(foreach ref,$(BUILDABLE_REFS),-r:$(ref))	\
	-r:System.Data.dll

$(OUT): $(SOURCES) $(BUILDABLE_REFS)
	@echo "    GMCS $(LIBRARY)"
	@gmcs -target:exe -debug:full -out:$@ $(SOURCES) $(REFERENCES)

all: $(OUT)

clean:
	@rm -f $(OUT)*

publish install: all
	@:
