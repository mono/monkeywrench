generate: ../scripts/database.sql CustomTypes.sql
	gmcs SqlToCSharp.cs -out:SqlToCSharp.exe -debug+
	cat ../scripts/database.sql > tmp
	cat CustomTypes.sql >> tmp
	mono --debug SqlToCSharp.exe tmp
	rm -f tmp
	rm SqlToCSharp.exe*
	unix2dos *.generated.cs
