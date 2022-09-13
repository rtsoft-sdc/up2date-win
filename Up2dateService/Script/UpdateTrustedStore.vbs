Set oShell = CreateObject("WScript.Shell")
Set oFileSystem = CreateObject("Scripting.FileSystemObject")
location = oFileSystem.GetParentFolderName(WScript.ScriptFullName)
oShell.Run "certutil.exe -addStore Root """ & location & "\ISRG_Root_X1.der"""
oShell.Run "certutil.exe -addStore CA """ & location & "\ISRG_Root_X1.der"""
