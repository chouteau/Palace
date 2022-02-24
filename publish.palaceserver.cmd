cd src\dotnet6\palaceserver\
dotnet publish -p:PublishProfile=Properties\PublishProfiles\FolderProfile.pubxml
cd bin\debug\net6.0\publish
del web.config
del appSettings.json
del appSettings.*.json
del ..\..\..\..\..\..\..\publish\palaceserver.zip
"C:\Program Files\7-Zip\7z.exe" a -tzip -r ..\..\..\..\..\..\..\publish\palaceserver.zip *
explorer "..\..\..\..\..\..\..\publish\"
exit