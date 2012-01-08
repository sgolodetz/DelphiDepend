@echo off

IF [%1]==[] GOTO PrintUsage

bin\Release\DelphiDepend -e +m +n %1 > visualddepend.gv
dot -Tpng:gdiplus visualddepend.gv -O
start visualddepend.gv.gdiplus.png
GOTO Finished

:PrintUsage

echo Usage: VisualDDepend ^<path^>

:Finished
