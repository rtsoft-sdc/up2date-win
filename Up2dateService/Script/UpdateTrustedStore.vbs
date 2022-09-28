Set oShell = CreateObject("WScript.Shell")
targetDir = Session.Property("CustomActionData")
certDir = targetDir & "Script\"
oShell.Run "certutil.exe -addStore Root """ & certDir & "ISRG_Root_X1.der"""
oShell.Run "certutil.exe -addStore CA """ & certDir & "ISRG_Root_X1.der"""
oShell.Run "certutil.exe -addStore Root """ & certDir & "RTSoft GmbH Root CA.cer"""
oShell.Run "certutil.exe -addStore CA """ & certDir & "RTSoft GmbH RITMS Code Signing.cer"""
