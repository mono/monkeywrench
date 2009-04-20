#!/bin/bash -x

find /tmp/AdobeFonts* -ctime +1  | xargs --verbose rm -R
find /tmp/plugtmp* -ctime +1 | xargs --verbose rm -R
find /tmp/test-directory-in-zip* -ctime +1 | xargs --verbose rm -R
find /tmp/ObfuscatedFonts.zip* -ctime +0 | xargs --verbose rm -R
find /tmp/moon-unit.xap* -ctime +0 | xargs --verbose rm -R
find /tmp/foo.zip* -ctime +0 | xargs --verbose rm -R
find /tmp/CustomFonts.zip* -ctime +0 | xargs --verbose rm -R
find /tmp/*.xap* -ctime +0 | xargs --verbose rm -R
find /tmp/47BB3D8Bd01* -ctime +0 | xargs --verbose rm -R
find /tmp/EB84F87Bd01* -ctime +0 | xargs --verbose rm -R
find /tmp/F5508BCBd01* -ctime +0 | xargs --verbose rm -R
find /tmp/linux-temp-aspnet-0/* -ctime +1 | xargs --verbose rm -R
find /tmp/orbit-linux/* -ctime +1 | xargs --verbose rm -R
find /tmp/???????????.?????? -ctime +0 | xargs --verbose rm -R
find /tmp/*.odttf.* -ctime +0 | xargs --verbose rm -R


