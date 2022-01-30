cd src\dotnet6\palaceserver\
dotnet publish -p:PublishProfile=Properties\PublishProfiles\FolderProfile.pubxml
cd bin\debug\net6.0\publish
"C:\Program Files\7-Zip\7z.exe" a -xr@exclude.txt -tzip -r ..\..\..\..\..\..\..\publish\palaceserver.zip *
explorer "..\..\..\..\..\..\..\publish\"