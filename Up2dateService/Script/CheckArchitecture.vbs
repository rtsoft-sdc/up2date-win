If Session.Property("CustomActionData") <> "" Then
    MsgBox "This package can be installed only on 32-bit platform! Use special 64-bit version for this computer."
    WScript.Quit -1
End If

