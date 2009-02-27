#!/bin/sh -ex

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
  <changeset sourcecontrol="svn" root="$REPOSITORY_ROOT">
    <directories>
EOF

# write directories
for directory in `svnlook dirs-changed -r $REV $REPOS`; do
	echo "      <directory>$directory</directory" >> $TMPFILE
done

# write xml end

cat >>$TMPFILE <<EOF
    </directories>
  </changeset>
</monkeywrench>
EOF

curl $REPORT_URL --form xml=@$TMPFILE

rm $TMPFILE

# exit 1