@echo off
echo WebView2 Runtime Diagnostic Tool
echo ================================
echo.

echo Checking WebView2 Runtime installation...
echo.

REM Check if WebView2 is installed via registry
echo Checking registry for WebView2 installation...
reg query "HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" /v pv 2>nul
if %errorlevel% equ 0 (
    echo [SUCCESS] WebView2 Runtime found in registry (32-bit)
) else (
    echo [INFO] WebView2 Runtime not found in 32-bit registry
)

reg query "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" /v pv 2>nul
if %errorlevel% equ 0 (
    echo [SUCCESS] WebView2 Runtime found in registry (64-bit)
) else (
    echo [INFO] WebView2 Runtime not found in 64-bit registry
)

echo.
echo Checking for WebView2 files...

REM Check common installation paths
set "webview2_path1=%ProgramFiles(x86)%\Microsoft\EdgeWebView\Application"
set "webview2_path2=%ProgramFiles%\Microsoft\EdgeWebView\Application"

if exist "%webview2_path1%" (
    echo [SUCCESS] WebView2 files found at: %webview2_path1%
    dir "%webview2_path1%" /b
) else (
    echo [INFO] WebView2 files not found at: %webview2_path1%
)

if exist "%webview2_path2%" (
    echo [SUCCESS] WebView2 files found at: %webview2_path2%
    dir "%webview2_path2%" /b
) else (
    echo [INFO] WebView2 files not found at: %webview2_path2%
)

echo.
echo Checking Microsoft Edge installation (WebView2 can use Edge runtime)...
set "edge_path=%ProgramFiles(x86)%\Microsoft\Edge\Application"
if exist "%edge_path%" (
    echo [SUCCESS] Microsoft Edge found at: %edge_path%
) else (
    echo [INFO] Microsoft Edge not found at: %edge_path%
)

echo.
echo System Information:
echo OS Version: 
ver
echo.
echo Architecture:
wmic os get osarchitecture /value | find "OSArchitecture"
echo.

echo Attempting to run the browser application with error capture...
echo.
cd /d "%~dp0"
echo Current directory: %CD%
echo.

REM Try to run the application and capture any error output
echo Running: NoorAhlulBayt.Browser\bin\Debug\net9.0-windows\NoorAhlulBayt.Browser.exe
"NoorAhlulBayt.Browser\bin\Debug\net9.0-windows\NoorAhlulBayt.Browser.exe" 2>&1
echo Application exit code: %errorlevel%

echo.
echo Diagnostic complete. 
echo.
echo If WebView2 is not installed, download it from:
echo https://developer.microsoft.com/en-us/microsoft-edge/webview2/
echo.
pause
