rd src\palace\bin\debug\net6.0\publish /s /Q
dotnet publish src\palace\palace.csproj
e:
cd src\palace\bin\debug\net6.0\publish
del appsettings.json
del appsettings.*.json
del *.zip
del *.7z
del ..\..\..\..\..\..\publish\palace.zip
"C:\Program Files\7-Zip\7z.exe" a -tzip ..\..\..\..\..\..\publish\palace.zip * -r 
explorer "..\..\..\..\..\..\publish\"
