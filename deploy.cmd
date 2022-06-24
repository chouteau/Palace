dotnet publish src\dotnet6\demosvc\demosvc.csproj
e:
cd src\DemoSvc\bin\Debug\net6.0\publish
"C:\Program Files\7-Zip\7z.exe" a -tzip -r ..\..\..\..\..\..\palaceserver\temp\demosvc.zip *
xcopy /Y ..\..\..\..\..\..\palaceserver\temp\demosvc.zip  ..\..\..\..\..\..\palaceserver\staging\demosvc.zip
exit
