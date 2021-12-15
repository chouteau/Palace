dotnet publish -p:PublishProfile=src\dotnet6\PalaceServer\Properties\PublishProfiles\FolderProfile.pubxml src\dotnet6\palaceserver\palaceserver.csproj
e:
cd src\dotnet6\palaceserver\bin\debug\net6.0\publish
"C:\Program Files\7-Zip\7z.exe" a -xr@exclude.txt -tzip -r ..\..\..\..\..\..\..\publish\palaceserver.zip *