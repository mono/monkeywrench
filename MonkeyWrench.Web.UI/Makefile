SOURCES = \
	$(wildcard *.cs)	\
	$(wildcard */*.cs)

TOPDIR = ..

LIBRARY = MonkeyWrench.Web.UI.dll
OUT_DIR = $(TOPDIR)/class/lib
OUT = $(OUT_DIR)/$(LIBRARY)

BUILDABLE_REFS = \
	$(OUT_DIR)/Newtonsoft.Json.dll \
	$(OUT_DIR)/DotNetOpenAuth.dll	\
	$(OUT_DIR)/MonkeyWrench.dll	\
	$(OUT_DIR)/MonkeyWrench.DataClasses.dll \
	$(OUT_DIR)/MonkeyWrench.Database.dll \
	$(OUT_DIR)/MonkeyWrench.Web.WebService.dll

REFERENCES = \
	$(foreach ref,$(BUILDABLE_REFS),-r:$(ref))	\
	-r:System.Data.dll	\
	-r:System.Web.dll	\
	-r:System.Drawing.dll	\
	-r:System.Web.Extensions.dll \
	-r:System.Web.Services.dll

$(OUT): $(SOURCES) $(BUILDABLE_REFS)
	@echo "    DMCS $(LIBRARY)"
	@dmcs -target:library -debug:full -out:$@ $(SOURCES) $(REFERENCES)

all: $(OUT)

clean:
	@rm -f $(OUT)*
	@rm -f bin/*.mdb bin/*.dll bin/*.pdb

install: all
	@echo "    PUBLISH $(LIBRARY)"
	@mkdir -p bin
	@cp -p $(OUT_DIR)/Npgsql.dll* bin/
	@cp -p $(OUT_DIR)/DotNetOpenAuth.dll* bin/
	@cp -p $(OUT_DIR)/log4net.dll* bin/
	@cp -p $(OUT_DIR)/Contracts_Standalone.dll* bin/
	@cp -p $(OUT_DIR)/Microsoft.Contracts.dll* bin/
	@cp -p $(OUT_DIR)/MonkeyWrench.dll* bin/
	@cp -p $(OUT_DIR)/MonkeyWrench.DataClasses.dll* bin/
	@cp -p $(OUT_DIR)/MonkeyWrench.Web.UI.dll* bin/
	@cp -p $(OUT_DIR)/MonkeyWrench.Web.WebService.dll* bin/
	@cp -p $(OUT_DIR)/Newtonsoft.Json.dll* bin/
	@cp -p $(OUT_DIR)/MonkeyWrench.Database.dll* bin/
