set lang=%1
set TargetDir=%2
set TargetName=%3
set ProjectDir=%4
set ProjectName=%5

echo Check if translation %lang% is up-to-date
copy /Y /B "%locbaml%" "%TargetDir%"
"%TargetDir%locbaml.exe" /parse /check "%TargetDir%en-US\%TargetName%.resources.dll" /out:"%ProjectDir%Localization\%ProjectName%_%lang%.csv"
IF %ERRORLEVEL% NEQ 0 GOTO end

echo Generate satellite %lang% lang dll
"%TargetDir%locbaml.exe" /generate "%TargetDir%en-US\%TargetName%.resources.dll" /trans:"%ProjectDir%Localization\%ProjectName%_%lang%.csv" /out:"%TargetDir%%lang%" /cul:%lang%
:end
IF %ERRORLEVEL% NEQ 0 EXIT /B %ERRORLEVEL%
