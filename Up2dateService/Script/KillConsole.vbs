Dim oShell : Set oShell = CreateObject("WScript.Shell")
oShell.Run "taskkill /f /im Up2dateConsole.exe", 0, True
