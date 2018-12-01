dotnet build -c Release --framework net461 ./test

:Loop
dotnet test --logger "console;verbosity=normal" -c Release --no-restore --no-build --framework net461 ./test
if %errorlevel% equ 0 goto :Loop
echo Connection established
