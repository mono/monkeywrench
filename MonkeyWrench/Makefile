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

$(OUT): $(SOURCES) $(BUILDABLE_REFS)
	gmcs -target:library -debug:full -out:$@ $(SOURCES) $(REFERENCES)


all: $(OUT)

clean:
	rm -f $(OUT)*

publish install: all
	@:
