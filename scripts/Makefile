BASEDIR=../Builder

all: database builder updater

database: bin/Builder.dll
builder: bin/Builder.Builder.exe
updater: bin/Builder.Updater.exe
filemanager: bin/Builder.FileManager.exe

BUILDER_SRC = $(wildcard $(BASEDIR)/*.cs)
REFERENCES = -r:bin/Npgsql.dll,System.Data.dll

bin/Builder.dll: $(BUILDER_SRC)
	gmcs -debug:full -target:library -out:bin/Builder.dll $(BUILDER_SRC) $(REFERENCES)
	-cp bin/Builder.dll* ../web/Bin

bin/Builder.Updater.exe: $(BUILDER_SRC)
	gmcs -debug:full -target:exe -out:bin/Builder.Updater.exe $(BUILDER_SRC) $(REFERENCES) -main:Builder.Updater

bin/Builder.Builder.exe: $(BUILDER_SRC)
	gmcs -debug:full -target:exe -out:bin/Builder.Builder.exe $(BUILDER_SRC) $(REFERENCES) -main:Builder.Builder
	
bin/Builder.FileManager.exe: $(BUILDER_SRC)
	gmcs -debug:full -target:exe -out:bin/Builder.FileManager.exe $(BUILDER_SRC) $(REFERENCES) -main:Builder.FileManager


generate:
	$(MAKE) -C ../Builder generate


clean-large-objects: filemanager
	mono --debug bin/Builder.FileManager.exe clean-large-objects

compress-files: filemanager
	mono --debug bin/Builder.FileManager.exe compress-files

execute-deletion-directives: filemanager
	mono --debug bin/Builder.FileManager.exe execute-deletion-directives
