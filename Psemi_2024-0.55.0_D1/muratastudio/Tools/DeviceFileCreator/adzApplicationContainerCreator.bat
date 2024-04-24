rem Usage: Drag and drop folder on this batch file
rem New format container.  
rem 
rem 

set zprog="c:\program files\7-zip\7z.exe"
set tgtcont=%1.adz
set devicePW="i8mSsv6Aj"

cd %1

%zprog% a -tzip  %tgtcont% ".\*"  
%zprog% a -tzip -mem=AES256 -p%devicePW% %tgtcont% "config\*"
move %tgtcont% ..
cd ..