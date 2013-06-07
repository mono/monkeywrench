SOURCES = \
	$(wildcard *.cs)	\
	$(wildcard */*.cs)

TOPDIR = ..

LIBRARY = MonkeyWrench.Web.WebService.dll
OUT_DIR = $(TOPDIR)/class/lib
OUT = $(OUT_DIR)/$(LIBRARY)

BUILDABLE_REFS = \
	$(OUT_DIR)/MonkeyWrench.dll	\
	$(OUT_DIR)/MonkeyWrench.DataClasses.dll	\
	$(OUT_DIR)/MonkeyWrench.Database.dll \
	$(OUT_DIR)/Meebey.SmartIrc4net.dll \
	$(OUT_DIR)/Newtonsoft.Json.dll

REFERENCES = \
	$(foreach ref,$(BUILDABLE_REFS),-r:$(ref))	\
	-r:Npgsql.dll	\
	-r:System.Data.dll	\
	-r:System.Web.dll	\
	-r:System.Web.Services.dll

SOURCES = \
	$(wildcard *.cs)

$(OUT): $(SOURCES) $(BUILDABLE_REFS)
	@echo "    DMCS $(LIBRARY)"
	@MONO_PATH=$(OUT_DIR):$(MONO_PATH) dmcs -target:library -debug:full -out:$@ $(SOURCES) $(REFERENCES)

all: $(OUT)

clean:
	@rm -f $(OUT)*
	@rm -f bin/*.dll bin/*.mdb bin/*.pdb

install: all
	@echo "    PUBLISH $(LIBRARY)"
	@mkdir -p bin
	@cp -p $(OUT)* bin
	@cp -p $(OUT_DIR)/Npgsql.dll* bin
	@cp -p $(OUT_DIR)/MonkeyWrench.dll* bin
	@cp -p $(OUT_DIR)/MonkeyWrench.DataClasses.dll* bin
	@cp -p $(OUT_DIR)/MonkeyWrench.Database.dll* bin
	@cp -p $(OUT_DIR)/Meebey.SmartIrc4net.dll* bin
	@cp -p $(OUT_DIR)/Newtonsoft.Json.dll* bin
	@cp -p $(OUT_DIR)/StarkSoftProxy.dll* bin

dataclasses:
	$(MAKE) -C $(TOPDIR)/MonkeyWrench.DataClasses
	$(MAKE) dependencies

wsdl: WebServices.wsdl

WebServices.wsdl: all
	curl http://localhost:8123/WebServices/WebServices.asmx?wsdl > WebServices.wsdl -m  5
	dos2unix WebServices.wsdl

