set lang=%1
set TargetDir=%2
set TargetName=%3
set ProjectDir=%4
set ProjectName=%5

echo Generate satellite %lang% lang dll
echo "%TargetDir%locbaml.exe" /generate "%TargetDir%en-US\%TargetName%.resources.dll" /trans:"%ProjectDir%Localization\%ProjectName%_%lang%.csv" /out:"%TargetDir%%lang%" /cul:%lang%
"%TargetDir%locbaml.exe" /generate "%TargetDir%en-US\%TargetName%.resources.dll" /trans:"%ProjectDir%Localization\%ProjectName%_%lang%.csv" /out:"%TargetDir%%lang%" /cul:%lang%
:end
IF %ERRORLEVEL% NEQ 0 EXIT /B %ERRORLEVEL%
