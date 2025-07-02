# Noor-e-AhlulBayt Browser - Comprehensive Logging System

## Overview

This document describes the comprehensive logging system implemented to diagnose UI display issues in the Islamic family-safe browser application.

## Features

### 1. Multi-Output Logging
- **Console Output**: Real-time logging displayed in a separate debug console window
- **File Logging**: Detailed logs saved to timestamped files in the `Logs` directory
- **Structured Logging**: Uses Serilog for professional-grade logging with proper formatting

### 2. Debug Console Window
- Automatically opens a separate console window when the application starts
- Shows real-time logging output for immediate debugging
- Console window title: "Noor-e-AhlulBayt Browser - Debug Console"

### 3. Log Categories

#### Application Startup (`Startup`)
- Application initialization steps
- Service initialization (HttpClient, Database, ContentFilter, PrayerTime)
- Exception handler registration
- Prayer timer setup

#### Window Events (`Window`)
- Window creation and initialization
- Visibility changes
- State changes (minimized, maximized, etc.)
- Size and location changes
- Activation/deactivation events

#### WebView2 Events (`WebView2`)
- WebView2 runtime availability and version
- Core WebView2 initialization
- Environment details (browser version, user data folder)
- Settings configuration
- Navigation events (starting, completed, failed)
- Content filtering actions

#### Content Filtering (`ContentFilter`)
- Ad blocking decisions
- Blacklist checking
- SafeSearch enforcement
- Time limit enforcement
- Prayer time blocking
- Profanity detection

### 4. Log Levels
- **DEBUG**: Detailed diagnostic information
- **INFO**: General information about application flow
- **WARNING**: Potentially problematic situations
- **ERROR**: Error conditions that don't stop the application
- **FATAL**: Critical errors that may cause application termination

### 5. Log Format

#### Console Output
```
[HH:mm:ss.fff LEVEL] SourceContext: Message
```

#### File Output
```
[yyyy-MM-dd HH:mm:ss.fff LEVEL] SourceContext (ThreadId): Message
Exception details (if any)
```

## Usage

### Running with Logging
1. Use the provided `run_with_logging.bat` script for easy startup
2. Or run the executable directly: `NoorAhlulBayt.Browser.exe`

### Log File Location
Log files are saved to: `NoorAhlulBayt.Browser\bin\Debug\net9.0-windows\Logs\`

File naming pattern: `NoorAhlulBayt.Browser_yyyyMMdd_HHmmss.log`

### Key Diagnostic Information

#### Application Startup Issues
Look for these log entries:
- `STARTUP: Application starting`
- `STARTUP: MainWindow constructor started`
- `STARTUP: InitializeComponent completed`
- `STARTUP: Database context configured`
- `STARTUP: MainWindow constructor completed successfully`

#### WebView2 Issues
Look for these log entries:
- `WEBVIEW2: Checking WebView2 availability`
- `WEBVIEW2: WebView2 version available`
- `WEBVIEW2: Initializing WebView2 core`
- `WEBVIEW2: WebView2 core initialized`
- `WEBVIEW2: WebView2 environment - BrowserVersionString`

#### Window Display Issues
Look for these log entries:
- `WINDOW: Constructor - Initializing MainWindow`
- `WINDOW: Window Properties`
- `WINDOW: Window State`
- `WINDOW: Loaded - MainWindow loaded event triggered`
- `WINDOW: IsVisibleChanged`

## Troubleshooting Common Issues

### 1. Application Not Starting
Check for:
- Fatal errors in application startup
- Missing dependencies
- Database connection issues
- Service initialization failures

### 2. UI Not Displaying
Check for:
- Window creation events
- Window visibility changes
- Window state information
- MainWindow loaded events

### 3. WebView2 Not Loading
Check for:
- WebView2 runtime availability
- WebView2 version information
- Core WebView2 initialization
- WebView2 environment setup

### 4. Navigation Issues
Check for:
- Navigation starting events
- Content filtering decisions
- Navigation completion status
- WebView2 error status

## Log Analysis Tips

1. **Start from the beginning**: Always check the application startup sequence first
2. **Follow the flow**: Trace through the logical sequence of events
3. **Look for exceptions**: Any exception logs indicate potential issues
4. **Check timing**: Use timestamps to identify slow operations
5. **Verify WebView2**: Ensure WebView2 runtime is properly installed and initialized

## Example Log Sequence (Successful Startup)

```
[10:30:15.123 INFO] Startup: Application starting - Arguments: 
[10:30:15.125 INFO] Window.Constructor: Initializing MainWindow
[10:30:15.130 INFO] Startup: InitializeComponent completed
[10:30:15.135 INFO] Startup: Database context configured
[10:30:15.140 INFO] Window.Loaded: MainWindow loaded event triggered
[10:30:15.145 INFO] WebView2: Checking WebView2 availability
[10:30:15.150 INFO] WebView2: WebView2 version available - 120.0.2210.144
[10:30:15.200 INFO] WebView2: WebView2 core initialized
[10:30:15.205 INFO] WebView2: WebView2 settings configured
[10:30:15.210 INFO] WebView2: NavigationStarting - URL: https://www.google.com
```

## Support

If you encounter issues not covered by the logging output, please:
1. Collect the complete log file
2. Note the exact steps that led to the issue
3. Include system information (OS version, WebView2 version)
4. Provide screenshots of any error messages
