**Document 3: MVP Requirements for Noor-e-AhlulBayt Windows Browser Suite**

---

## ✅ Purpose

Define the **Minimum Viable Product (MVP)** scope for the first launchable version on Windows. This MVP focuses on core value for family-safe browsing while being achievable quickly.

---

## ✅ MVP Core Features

### 1️⃣ Profile Management
- Single user profile (no multi-profile yet)
- PIN-protected settings screen
- Store PIN securely (DPAPI)

---

### 2️⃣ Profanity Filtering
- JavaScript scan of document body for keywords/regex
- Blocking overlay with reason
- Option to override with PIN

---

### 3️⃣ NSFW Image Filtering
- Capture thumbnail screenshot
- Local NSFWJS analysis
- Blocking overlay with reason
- PIN override

---

### 4️⃣ Ad and Tracker Blocking
- Parse EasyList/EasyPrivacy filters
- Intercept and block matching requests
- Default ON

---

### 5️⃣ Azan Blocking (Basic)
- Manual city entry
- Fetch prayer times from Aladhan API
- Schedule Azan blocking overlay in browser (10 min)
- Optional Adhan audio playback

---

### 6️⃣ Time Limits (Basic)
- Daily usage limit in minutes
- Countdown timer in UI
- Blocking overlay when limit hit

---

### 7️⃣ Bookmarks
- Add/edit/delete bookmarks
- Local storage (SQLite)

---

### 8️⃣ History Logging
- Log visited URLs per session
- Viewable in settings (PIN-protected)

---

### 9️⃣ Private/Incognito Mode
- Incognito tabs with no history or bookmarks saved
- Filters still enforced

---

### 10️⃣ Settings Screen
- Ad/Tracker blocking toggle
- Profanity/NSFW filtering toggle
- SafeSearch enforcement toggle (manual only)
- PIN management
- City for Azan times
- Daily time limit setting

---

### 11️⃣ UI and Theme
- Green/black Islamic UI
- Scheherazade font for Arabic content

---

### 12️⃣ Companion App (MVP Scope)
- Background monitoring for known browsers
- Notify/close detected forbidden browsers
- System tray icon with basic menu:
  - Settings
  - Enable/disable blocking
  - Exit

---

## ✅ MVP Excluded (Future Roadmap)

- Multiple profiles with separate settings
- Admin PIN to switch profiles
- SafeSearch auto-enforcement via URL rewriting
- Schedules for allowed hours
- Advanced filter list auto-updates
- Import/export settings
- Detailed usage logs and reports
- Quran reader, daily Hadith, prayer time widget
- Activity logging with charts

---

## ✅ MVP Goals

✅ Core safe-browsing for families  
✅ Enforced ad/tracker blocking  
✅ Basic parental control with PIN  
✅ Azan blocking aligned with prayer times  
✅ Clean, Islamic-themed UI  
✅ Lightweight, local storage only  
✅ Easy install and no telemetry  

---

**End of Document 3**

