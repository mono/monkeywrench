SOURCES = \
	$(wildcard *.cs)	\
	$(wildcard */*.cs)

TOPDIR = ..

LIBRARY = MonkeyWrench.Database.dll
OUT_DIR = $(TOPDIR)/class/lib
OUT = $(OUT_DIR)/$(LIBRARY)

BUILDABLE_REFS = \
	$(OUT_DIR)/MonkeyWrench.dll	\
	$(OUT_DIR)/MonkeyWrench.DataClasses.dll

REFERENCES = \
	$(foreach ref,$(BUILDABLE_REFS),-r:$(ref))	\
	-r:Npgsql.dll	\
	-r:System.Data.dll

$(OUT): $(SOURCES) $(BUILDABLE_REFS)
	@echo "    DMCS $(LIBRARY)"
	@MONO_PATH=$(OUT_DIR):$(MONO_PATH) dmcs -target:library -debug:full -out:$@ $(SOURCES) $(REFERENCES)

all: $(OUT)

clean:
	@rm -f $(OUT)*

publish install: all
	@:
