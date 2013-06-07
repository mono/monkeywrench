DIR = external

TOPDIR = ..

LIBRARY = Meebey.SmartIrc4net.dll
OUT_DIR = $(TOPDIR)/class/lib
OUT = $(OUT_DIR)/$(LIBRARY)

$(OUT): $(SOURCES) $(BUILDABLE_REFS) external/src/starksoftproxy/.git
	cd $(DIR) && xbuild
	cp $(DIR)/bin/release/*.dll* $(OUT_DIR)

all: $(OUT)

clean:
	@rm -f $(OUT)*

publish install: all
	@:

external/src/starksoftproxy/.git:
	cd .. && git submodule update --init --recursive 