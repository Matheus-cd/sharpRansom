@echo off
echo Matando processos...
taskkill /F /IM main.exe 2>nul
taskkill /F /IM MSBuild.exe 2>nul
taskkill /F /IM VBCSCompiler.exe 2>nul
taskkill /F /IM dotnet.exe 2>nul

echo.
echo Aguardando 3 segundos...
timeout /t 3 /nobreak >nul

set OUTPUT_DIR=C:\temp\gosharp_%RANDOM%

echo.
echo Publicando para pasta temporaria: %OUTPUT_DIR%
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true /p:TrimMode=link -o "%OUTPUT_DIR%"

if errorlevel 1 (
    echo.
    echo ERRO na compilacao!
    pause
    exit /b 1
)

echo.
echo ========================================
echo SUCESSO! Executavel gerado em:
echo %OUTPUT_DIR%\main.exe
echo ========================================
echo.
echo Copiando para pasta do projeto...
copy "%OUTPUT_DIR%\main.exe" "main.exe" /Y

if errorlevel 1 (
    echo.
    echo Aviso: Nao foi possivel copiar para a pasta do projeto.
    echo Use o arquivo em: %OUTPUT_DIR%\main.exe
) else (
    echo.
    echo Executavel tambem copiado para: %CD%\main.exe
)

echo.
pause
