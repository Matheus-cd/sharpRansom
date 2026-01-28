@echo off
echo Matando processos...
taskkill /F /IM main.exe 2>nul
taskkill /F /IM MSBuild.exe 2>nul
taskkill /F /IM VBCSCompiler.exe 2>nul
taskkill /F /IM dotnet.exe 2>nul

echo.
echo Aguardando 3 segundos...
timeout /t 3 /nobreak >nul

echo.
echo Limpando arquivos antigos...
if exist "bin\Release\net48\main.exe" (
    echo Deletando main.exe antigo...
    del /F /Q "bin\Release\net48\main.exe" 2>nul
    timeout /t 1 /nobreak >nul
)
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj

echo.
echo Aguardando mais 2 segundos...
timeout /t 2 /nobreak >nul

echo.
echo Compilando .NET Framework 4.8...
dotnet build -c Release

if errorlevel 1 (
    echo.
    echo ERRO na compilacao! Verifique se algum arquivo esta aberto.
    pause
    exit /b 1
)

echo.
echo ========================================
echo SUCESSO! Executavel gerado em:
echo bin\Release\net48\main.exe
echo ========================================
echo.
pause
