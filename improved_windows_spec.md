**Document 2: Improved Spec for Windows Platform (Final Requirements)**

---

## ✅ Project Overview

**Name:** Noor-e-AhlulBayt Islamic Family-Safe Browser Suite  
**Platform:** Windows 10/11 (x64)  
**Framework:** .NET 9 (WPF recommended)  
**Web Engine:** Microsoft Edge WebView2  
**Packaging:** MSIX/MSI  

---

## ✅ Components

1️⃣ **Noor-e-AhlulBayt Browser (Main App)**  
2️⃣ **Noor-e-AhlulBayt Companion (Background/Parental Control App)**  

---

## ✅ General Requirements

- Lightweight install (~10–20 MB)
- Green/black Islamic UI theme
- Scheherazade font for Arabic content
- Local SQLite storage for profiles, logs, settings
- Secure PIN storage using Windows DPAPI
- IPC with Named Pipes or local WebSockets
- Toast notifications

---

## ✅ Major Features Overview

- Profiles with PIN protection
- Profanity and NSFW filtering with overlays
- Ad and tracker blocking (EasyList/EasyPrivacy)
- SafeSearch enforcement
- Time limits and scheduled access
- Azan blocking with prayer times integration
- Bookmarks and history management
- Private/incognito mode
- Islamic resources tab (Quran, Hadith, Prayer times)
- System tray background monitoring (Companion App)
- Activity logging and reports
- Import/export settings

---

## ✅ Noor-e-AhlulBayt Browser App Features

### Profiles

- Multiple profiles with per-profile settings:
  - Filtering strictness
  - AdBlock toggle
  - SafeSearch toggle
  - Time limits (mins/day)
  - Allowed hours (schedule)
  - Whitelist/Blacklist domains
  - PIN protection
- Profile selection on launch with optional "Remember Last Profile"
- Admin PIN required for switching if enabled

---

### Filtering

- **Profanity Filtering:**
  - JS injection into WebView2
  - Keyword/regex scanning of document body
  - Overlay with reason, Go Back/Proceed (PIN) options

- **NSFW Image Filtering:**
  - Screenshot capture
  - Local NSFWJS or ONNX model analysis
  - Blocking overlay with PIN override

- **Ad/Tracker Blocking:**
  - EasyList/EasyPrivacy parsing
  - WebResourceRequested interception
  - Cosmetic filtering support
  - Default ON for all profiles
  - Statistics dashboard

- **SafeSearch Enforcement:**
  - Append SafeSearch parameters to URLs
  - Block search engines that don’t support SafeSearch

- **Whitelist/Blacklist Management:**
  - Per-profile settings
  - Enforced on all navigations

---

### Blocking Overlays

- Custom full-screen overlays for:
  - Profanity/NSFW blocks
  - Time limit enforcement
  - Azan blocking
- Includes reason and PIN override option

---

### Time Limits and Scheduling

- Daily usage limit per profile
- Allowed hours schedule
- Countdown timers in UI
- Overlay blocking when limits reached
- PIN override for parents

---

### Azan Blocking

- IPC from Companion triggers blocking overlay
- 10-minute countdown timer during prayer
- Optional Adhan audio playback
- Prayer times fetched via Aladhan API
- City auto-detect or manual selection

---

### Bookmarks and History

- Bookmarks with folder support
- History per profile
- PIN-protected viewing and editing

---

### Private/Incognito Mode

- Incognito tabs with no local storage
- Filtering still enforced
- Incognito badge in UI

---

### Islamic Resources Tab

- Quran reader in Scheherazade font
- Daily Hadith display
- Prayer time widget with city awareness

---

### Notifications

- Windows Toast notifications:
  - Approaching time limit
  - Azan start
  - Blocking events

---

### Settings UI

- General settings (default browser, theme)
- Profiles management (add/edit/delete, PINs)
- Ad/Tracker blocking toggles
- SafeSearch enforcement
- Time limits and schedules
- Azan blocking settings
- PIN management
- Import/export settings

---

## ✅ Noor-e-AhlulBayt Companion App Features

- Runs as system tray app or background service
- Monitors for other browsers:
  - chrome.exe, msedge.exe, firefox.exe, etc.
  - Option to auto-close or notify
- System tray notifications for blocking events
- Prayer times management:
  - Fetch from Aladhan API daily
  - Auto-detect or manual city selection
  - Schedule IPC triggers for Azan blocking
  - Adhan audio option
- Logs browser block attempts and Azan events
- PIN-protected settings and logs viewing

---

## ✅ Technical Details

- .NET 8+ with WPF/WinUI
- Microsoft.Web.WebView2
- SQLite for local data
- DPAPI for PIN encryption
- Named Pipes or local WebSockets for IPC
- HttpClient for Aladhan API
- Scheherazade font bundling
- MSIX/MSI packaging

---

## ✅ Performance Targets

- Profanity filter: ~10 ms
- NSFW check: ~500 ms
- Ad blocking rule check: ~5–10 ms/request
- Azan blocking command: <10 ms latency
- Memory:
  - Browser: ~50–200 MB (many tabs)
  - Companion: ~10–20 MB idle

---

## ✅ Packaging and Compliance

- MSIX for Microsoft Store or MSI for direct installs
- Optional auto-update of filter lists with consent
- Privacy policy: No telemetry, only local data storage

---

## ✅ End of Document 2

