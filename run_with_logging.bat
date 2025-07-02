@echo off
echo Starting Noor-e-AhlulBayt Browser with comprehensive logging...
echo.
echo This will launch the browser with:
echo - Debug console window for real-time logging
echo - Log files saved to the Logs directory
echo - Comprehensive startup and WebView2 diagnostics
echo.
echo Press any key to continue...
pause > nul

echo.
echo Launching browser...
cd /d "%~dp0"
start "" "NoorAhlulBayt.Browser\bin\Debug\net9.0-windows\NoorAhlulBayt.Browser.exe"

echo.
echo Browser launched. Check the debug console window and log files for diagnostic information.
echo Log files are saved in: NoorAhlulBayt.Browser\bin\Debug\net9.0-windows\Logs\
echo.
echo Press any key to exit this script...
pause > nul
