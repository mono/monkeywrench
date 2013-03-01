DIR = Newtonsoft.Json
SOURCES = \
	$(wildcard ../$(DIR)/Src/Newtonsoft.Json/*.cs) \
	$(wildcard ../$(DIR)/Src/Newtonsoft.Json/*/*.cs)

TOPDIR = ..

LIBRARY = Newtonsoft.Json.dll
OUT_DIR = $(TOPDIR)/class/lib
OUT = $(OUT_DIR)/$(LIBRARY)

$(OUT): $(SOURCES) $(BUILDABLE_REFS)
	@echo "    DMCS $(LIBRARY)"
	@dmcs -target:library -debug:full -out:$@ -r:System.Xml.Linq -r:System.Data -r:System.Runtime.Serialization.dll $(SOURCES) -warn:0 -D:NET_2_0

all: $(OUT)

clean:
	@rm -f $(OUT)*

publish install: all
	@:
