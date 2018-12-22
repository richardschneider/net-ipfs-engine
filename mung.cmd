dotnet build -c Release --framework netcoreapp2.1 ./test

:Loop
dotnet test --logger "console;verbosity=normal" -c Release --no-restore --no-build --framework netcoreapp2.1 ./test --filter BitswapApiTest
if %errorlevel% equ 0 goto :Loop
echo Connection established
