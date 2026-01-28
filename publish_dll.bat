@echo off
echo Matando processos...
taskkill /F /IM main.exe 2>nul
taskkill /F /IM MSBuild.exe 2>nul
taskkill /F /IM VBCSCompiler.exe 2>nul
taskkill /F /IM dotnet.exe 2>nul

echo.
echo Aguardando 3 segundos...
timeout /t 3 /nobreak >nul

set OUTPUT_DIR=C:\temp\gosharp_dll_%RANDOM%

echo.
echo Compilando com Costura.Fody (dependencias embarcadas)...
dotnet build -c Release

if errorlevel 1 (
    echo.
    echo ERRO na compilacao!
    pause
    exit /b 1
)

echo.
echo Copiando arquivos para: %OUTPUT_DIR%
mkdir "%OUTPUT_DIR%" 2>nul
copy "bin\Release\net48\main.dll" "%OUTPUT_DIR%\main.dll" /Y
copy "bin\Release\net48\main.exe" "%OUTPUT_DIR%\main.exe" /Y
copy "bin\Release\net48\main.pdb" "%OUTPUT_DIR%\main.pdb" /Y 2>nul

echo.
echo ========================================
echo SUCESSO! Arquivos gerados em:
echo %OUTPUT_DIR%
echo ========================================
echo.
echo Copiando para pasta dll_output...
if exist dll_output rmdir /s /q dll_output
mkdir dll_output
xcopy "%OUTPUT_DIR%\*.*" "dll_output\" /E /I /Y

echo.
echo ========================================
echo Arquivos em: %CD%\dll_output
echo.
echo .NET Framework 4.8 Assembly - Funciona standalone!
echo.
echo Para usar no PowerShell (LoadFrom):
echo $asm = [System.Reflection.Assembly]::LoadFrom("%CD%\dll_output\main.dll")
echo $type = $asm.GetType("gosharp.Program")
echo $method = $type.GetMethod("Main")
echo $method.Invoke($null, @(,@("encryptdir", "-d", "C:\test")))
echo.
echo OU carregar em memoria (Load):
echo $bytes = [System.IO.File]::ReadAllBytes("%CD%\dll_output\main.dll")
echo $asm = [System.Reflection.Assembly]::Load($bytes)
echo $type = $asm.GetType("gosharp.Program")
echo $method = $type.GetMethod("Main")
echo $method.Invoke($null, @(,@("encryptdir", "-d", "C:\test")))
echo ========================================
echo.
pause
