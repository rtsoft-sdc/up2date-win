Set oShell = CreateObject("WScript.Shell")
targetDir = Session.Property("CustomActionData")
oShell.Run """" & targetDir & "Script\ImportCertificates.bat"""