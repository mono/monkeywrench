
generate-assembly-info:
	@echo "Updating AssemblyInfo"
	@cp AssemblyInfo.cs.in AssemblyInfo.tmp
	@sed -e "s/.*AssemblyDescription.*/[assembly: AssemblyDescription (@\"MonkeyWrench (branch: `git branch | grep "^[*] " | sed 's/^[*] //'` commit #`git log --pretty=format:''|wc -l` sha: `git log --pretty=format:'%h' -1` date: `git log --pretty=format:'%ci' -1`) subject: `git log --pretty=format:'%s' -1 | sed 's_\\"_\\"\\"_g' | sed 's_/__g'`)\")]/" \
	     -e "s/.*AssemblyVersion.*/[assembly: AssemblyVersion (\"1.0.0.`git log --pretty=format:''|wc -l`\")]/" AssemblyInfo.cs.in > AssemblyInfo.tmp
	@rsync --checksum AssemblyInfo.tmp AssemblyInfo.cs
