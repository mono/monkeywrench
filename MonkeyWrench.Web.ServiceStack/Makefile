TOPDIR = ..

LIBRARY = MonkeyWrench.Web.ServiceStack.dll
OUT_DIR = $(TOPDIR)/class/lib
OUT = $(OUT_DIR)/$(LIBRARY)

all:
	@echo "    XBUILD $(basename $(LIBRARY))"
	@xbuild /property:TreatWarningsAsErrors=False /verbosity:quiet MonkeyWrench.Web.ServiceStack.csproj

clean:
	@rm -f $(OUT)*
	@rm -f bin/*.dll bin/*.mdb bin/*.pdb

install: all

.PHONY: all
