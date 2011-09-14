DIR = SmartIrc4net-0.4.5.1
SOURCES = \
	$(wildcard $(DIR)/src/*.cs) \
	$(wildcard $(DIR)/src/*/*.cs)

TOPDIR = ..

LIBRARY = Meebey.SmartIrc4net.dll
OUT_DIR = $(TOPDIR)/class/lib
OUT = $(OUT_DIR)/$(LIBRARY)

$(OUT): $(SOURCES) $(BUILDABLE_REFS)
	@echo "    GMCS $(LIBRARY)"
	@gmcs -target:library -debug:full -out:$@ $(SOURCES) -warn:0 -D:NET_2_0

all: $(OUT)

clean:
	@rm -f $(OUT)*

publish install: all
	@:
