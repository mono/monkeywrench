#!/bin/sh -ex

#
# This script sends an xml file with a list of all the changed directories
# for a particular revision to a configurable url. 
#
# The xml file has the following format:
#
# <monkeywrench version="1">
#   <changeset sourcecountrol="svn|git" root="<repository root>" revision="<revision>">
#     <directories>
#        <directory>/dir1</directory>
#        <directory>/dir2</directory>
#     </directories>
#   </changeset>
# </monkeywrench>
#
#

# configurable variables

REPOSITORY_ROOT=mono-cvs.ximian.com/source
REPORT_URL=http://sublimeintervention.com:8123/ReportCommit.aspx

## 

REPOS="$1"
REV="$2"

TMPFILE=`mktemp`

# write xml start

cat >$TMPFILE <<EOF
<monkeywrench version="1">
  <changeset sourcecontrol="svn" root="$REPOSITORY_ROOT" revision="$REV">
    <directories>
EOF

# write directories
for directory in `svnlook dirs-changed -r $REV $REPOS`; do
	echo "      <directory>$directory</directory>" >> $TMPFILE
done

# write xml end

cat >>$TMPFILE <<EOF
    </directories>
  </changeset>
</monkeywrench>
EOF

curl $REPORT_URL --form xml=@$TMPFILE -m 15

rm $TMPFILE

# exit 1