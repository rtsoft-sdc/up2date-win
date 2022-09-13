Echo off
set lang=%1
set ProjectName=Up2dateConsole

echo === Updating translation to language %lang% ===

pushd %~dp0

echo === Update identifiers in all XAMLs ===
msbuild /t:updateuid ..\%ProjectName%.csproj
IF %ERRORLEVEL% NEQ 0 GOTO err

echo === Build the project (Debug configuration) ===
msbuild ..\%ProjectName%.csproj /p:Configuration=Debug;PostBuildEvent=
IF %ERRORLEVEL% NEQ 0 GOTO err

cd ..\bin\Debug\

echo === Copy locbaml.exe to target folder ===
copy /Y /B %locbaml%
IF %ERRORLEVEL% NEQ 0 GOTO err

echo === Update translation csv file ===
locbaml.exe /parse /update en-US\%ProjectName%.resources.dll /out:..\..\Localization\%ProjectName%_%lang%.csv
IF %ERRORLEVEL% NEQ 0 GOTO err

echo === Completed ===
goto end

:err
echo === Error! ===
:end
popd
Echo on
IF %ERRORLEVEL% NEQ 0 EXIT /B %ERRORLEVEL%
