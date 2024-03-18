dotnet publish -r win-x64 -c Release -p:PublishReadyToRun=true -p:PublishSingleFile=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -r osx-x64 -c Release /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true --self-contained
dotnet publish -r linux-arm -c Release /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true --self-contained
dotnet publish -r linux-x64 -c Release /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true --self-contained

cd \Stuff\Repos\EmbyRefreshLogos\Build
copy /Y "C:\Stuff\Repos\EmbyRefreshLogos\bin\Release\net8.0\win-x64\publish\EmbyRefreshLogos.exe" .
"C:\Program Files\7-Zip\7z" a -tzip EmbyRefreshLogos-WIN.zip EmbyRefreshLogos.exe EmbyRefreshLogos.txt

copy /Y "C:\Stuff\Repos\EmbyRefreshLogos\bin\Release\net8.0\linux-arm\publish\EmbyRefreshLogos" .
"C:\Program Files\7-Zip\7z" a -t7z EmbyRefreshLogos-RasPi.7z EmbyRefreshLogos EmbyRefreshLogos.txt

copy /Y "C:\Stuff\Repos\EmbyRefreshLogos\bin\Release\net8.0\osx-x64\publish\EmbyRefreshLogos" .
"C:\Program Files\7-Zip\7z" a -t7z EmbyRefreshLogos-OSX.7z EmbyRefreshLogos EmbyRefreshLogos.txt

copy /Y "C:\Stuff\Repos\EmbyRefreshLogos\bin\Release\net8.0\linux-x64\publish\EmbyRefreshLogos" .
"C:\Program Files\7-Zip\7z" a -t7z EmbyRefreshLogos-LIN64.7z EmbyRefreshLogos EmbyRefreshLogos.txt
