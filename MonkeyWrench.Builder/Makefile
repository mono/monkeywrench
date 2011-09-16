SOURCES = \
	$(wildcard *.cs)	\
	$(wildcard */*.cs)

TOPDIR = ..

LIBRARY = MonkeyWrench.Builder.exe
OUT_DIR = $(TOPDIR)/class/lib
OUT = $(OUT_DIR)/$(LIBRARY)

BUILDABLE_REFS = \
	$(OUT_DIR)/Npgsql.dll	\
	$(OUT_DIR)/MonkeyWrench.dll	\
	$(OUT_DIR)/MonkeyWrench.DataClasses.dll

REFERENCES = \
	$(foreach ref,$(BUILDABLE_REFS),-r:$(ref))	\
	-r:System.Data.dll	\
	-r:System.Web.Services.dll

$(OUT): $(SOURCES) $(BUILDABLE_REFS)
	@echo "    DMCS $(LIBRARY)"
	@dmcs -target:exe -debug:full -out:$@ $(SOURCES) $(REFERENCES)

all: $(OUT)

clean:
	@rm -f $(OUT)*

publish install: all
	@:
