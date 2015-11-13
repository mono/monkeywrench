assemblypath = File.expand_path(File.dirname(__FILE__))
branch = `git branch`.strip.match(/\* (.*)/)[1]
no_commits = `git log --pretty=format:''`.scan(/\n/).length
sha = `git log --pretty=format:%h -1`
date = `git log --pretty=format:%ci -1`
subject = `git log --pretty=format:%s -1`.gsub '\"', '\"\"'

assemblycstmp = File.read(File.join(assemblypath, 'AssemblyInfo.cs.in'))

description = "[assembly: AssemblyDescription (@\"MonkeyWrench (branch: #{branch} commit #: #{no_commits} sha: #{sha} date: #{date} subject: subject)\")]"
version = "[assembly: AssemblyVersion (\"1.0.0.#{no_commits}\")]"

assemblycs = assemblycstmp.gsub(/.*AssemblyDescription.*/, description).gsub(/.*AssemblyVersion.*/, version)

filename = File.join(assemblypath, 'AssemblyInfo.cs')

File.open(filename, 'w') {|f| f.write assemblycs }
