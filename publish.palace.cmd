rd src\dotnet6\palace\bin\debug\net6.0\publish /s /Q
dotnet publish src\dotnet6\palace\palace.csproj
e:
cd src\dotnet6\palace\bin\debug\net6.0\publish
"C:\Program Files\7-Zip\7z.exe" a -tzip ..\..\..\..\..\..\..\publish\palace.zip * -r -x!*.json
explorer "..\..\..\..\..\..\..\publish\"
