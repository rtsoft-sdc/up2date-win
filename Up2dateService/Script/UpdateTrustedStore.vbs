Set oShell = CreateObject("WScript.Shell")
targetDir = Session.Property("CustomActionData")
certPath = targetDir & "Script\ISRG_Root_X1.der"
oShell.Run "certutil.exe -addStore Root """ & certPath & """"
oShell.Run "certutil.exe -addStore CA """ & certPath & """"

