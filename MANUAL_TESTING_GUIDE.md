# Manual Testing Guide - Noor-e-AhlulBayt Islamic Browser

## 🚨 CRITICAL: NSFW Model Setup Test

### Test 1: Missing NSFW Model Behavior
**Purpose**: Verify the app properly handles missing core component

**Steps**:
1. **Ensure NSFW model is NOT present**:
   ```
   Navigate to: NoorAhlulBayt.Browser\Models\AI\
   Verify: nsfw-model.onnx file does NOT exist
   ```

2. **Start the browser**:
   ```
   dotnet run --project NoorAhlulBayt.Browser
   ```

3. **Expected Behavior**:
   - ✅ Warning dialog should appear with title: "Critical Component Missing"
   - ✅ Dialog should explain NSFW model is required for family safety
   - ✅ Dialog should provide download link: https://github.com/iola1999/nsfw-detect-onnx/releases
   - ✅ Dialog should offer choice: Continue (NOT RECOMMENDED) or Exit
   - ✅ If user clicks "No" → Application should exit gracefully
   - ✅ If user clicks "Yes" → Application continues with warning in logs

### Test 2: NSFW Model Present
**Purpose**: Verify normal startup with model

**Steps**:
1. **Download and install NSFW model**:
   ```
   1. Go to: https://github.com/iola1999/nsfw-detect-onnx/releases
   2. Download: nsfw-detect-onnx-v1.0.0.zip (or latest)
   3. Extract nsfw-model.onnx
   4. Copy to: NoorAhlulBayt.Browser\Models\AI\nsfw-model.onnx
   5. Verify file size: ~25-30 MB
   ```

2. **Start the browser**:
   ```
   dotnet run --project NoorAhlulBayt.Browser
   ```

3. **Expected Behavior**:
   - ✅ No warning dialog should appear
   - ✅ Application starts normally
   - ✅ Check logs for: "NSFW Model: Loaded"

## 🔍 Content Filtering Tests

### Test 3: Profanity Text Filtering
**Purpose**: Test text-based content blocking

**Test Cases**:
1. **Navigate to pages with profanity**:
   ```
   Create test HTML file with content:
   - "This is damn annoying content"
   - "What the hell is happening"
   - "This website is crap"
   - "Don't be so stupid"
   ```

2. **Expected Behavior**:
   - ✅ Content should be blocked with overlay
   - ✅ Reason: "Profanity detected" or "Inappropriate content detected"
   - ✅ User should see blocking message

### Test 4: Inappropriate Content Pattern Detection
**Purpose**: Test regex-based content filtering

**Test Cases**:
1. **Navigate to pages containing**:
   ```
   - "Adult entertainment websites"
   - "XXX rated content"
   - "Casino gambling games"
   - "Poker betting online"
   - "Nude art galleries"
   ```

2. **Expected Behavior**:
   - ✅ Content blocked with "Inappropriate content detected"
   - ✅ Blocking overlay appears

### Test 5: Domain Blacklist Testing
**Purpose**: Test hardcoded domain blocking

**Test Cases**:
1. **Try to navigate to blocked domains**:
   ```
   - https://pornhub.com
   - https://xvideos.com
   - https://casino.com
   - https://bet365.com
   ```

2. **Expected Behavior**:
   - ✅ Navigation should be blocked
   - ✅ Message: "Domain is blacklisted"
   - ✅ Page should not load

### Test 6: Safe Content Verification
**Purpose**: Ensure legitimate content is not blocked

**Test Cases**:
1. **Navigate to safe websites**:
   ```
   - https://wikipedia.org
   - https://stackoverflow.com
   - https://github.com
   - https://google.com
   - Islamic educational websites
   ```

2. **Expected Behavior**:
   - ✅ All content should load normally
   - ✅ No blocking overlays
   - ✅ No false positives

## 🖼️ NSFW Image Detection Tests

### Test 7: URL-Based Image Filtering
**Purpose**: Test basic URL pattern detection

**Test Cases**:
1. **Test suspicious image URLs** (these won't actually load, just test the filtering):
   ```
   - https://example.com/adult-content.jpg
   - https://example.com/xxx-image.png
   - https://example.com/porn-thumbnail.gif
   - https://example.com/nude-photo.jpg
   - https://example.com/sexy-model.png
   ```

2. **Expected Behavior**:
   - ✅ Images should be blocked
   - ✅ Reason should contain "NSFW pattern" or similar
   - ✅ Blocking overlay appears

### Test 8: Safe Image URLs
**Purpose**: Verify safe images are not blocked

**Test Cases**:
1. **Test safe image URLs**:
   ```
   - https://picsum.photos/300/200
   - https://via.placeholder.com/400x300
   - https://httpbin.org/image/jpeg
   ```

2. **Expected Behavior**:
   - ✅ Images should load normally
   - ✅ No blocking overlays

### Test 9: AI Model Image Analysis (If Model Present)
**Purpose**: Test actual AI-powered NSFW detection

**Test Cases**:
1. **Navigate to image-heavy websites**:
   ```
   - News websites with photo galleries
   - Educational sites with diagrams
   - Shopping sites with product images
   ```

2. **Monitor behavior**:
   - ✅ Check browser console for NSFW analysis logs
   - ✅ Verify confidence scores in logs
   - ✅ Ensure no false positives on family-safe content
   - ✅ Monitor CPU/memory usage during analysis

## 🚫 Ad Blocking Tests

### Test 10: Ad/Tracker Blocking
**Purpose**: Test ad and tracking script blocking

**Test Cases**:
1. **Navigate to ad-heavy websites**:
   ```
   - News websites with ads
   - Free content sites
   - Social media sites
   ```

2. **Check network tab in browser dev tools**:
   - ✅ Verify requests to ad domains are blocked
   - ✅ Look for blocked requests to:
     - googleads.g.doubleclick.net
     - facebook.com/tr
     - google-analytics.com
     - googletagmanager.com

## 🔍 Safe Search Tests

### Test 11: Search Engine Safe Search
**Purpose**: Test automatic safe search enforcement

**Test Cases**:
1. **Search on different engines**:
   ```
   - Google: https://google.com/search?q=test
   - Bing: https://bing.com/search?q=example
   - DuckDuckGo: https://duckduckgo.com/?q=query
   ```

2. **Check URL parameters**:
   - ✅ Google should add: &safe=strict
   - ✅ Bing should add: &adlt=strict
   - ✅ DuckDuckGo should add: &safe-search=strict

## 🕌 Islamic Features Tests

### Test 12: Prayer Time Integration
**Purpose**: Test prayer time blocking (if configured)

**Prerequisites**: Configure city in settings

**Test Cases**:
1. **During prayer time**:
   - ✅ Navigation should be blocked
   - ✅ Message: "Browsing is blocked during Azan"

2. **Outside prayer time**:
   - ✅ Normal browsing should work

## 📊 Performance Tests

### Test 13: System Resource Usage
**Purpose**: Monitor performance impact

**Test Cases**:
1. **Open multiple tabs with images**:
   ```
   - Open 5-10 tabs with image-heavy content
   - Monitor Task Manager
   ```

2. **Expected Behavior**:
   - ✅ CPU usage should remain reasonable (<50% sustained)
   - ✅ Memory usage should not exceed 500MB
   - ✅ Browser should remain responsive

## 🔧 Error Handling Tests

### Test 14: Network Issues
**Purpose**: Test behavior with poor connectivity

**Test Cases**:
1. **Disconnect internet temporarily**
2. **Try to navigate to websites**
3. **Expected Behavior**:
   - ✅ Graceful error handling
   - ✅ No application crashes
   - ✅ Appropriate error messages

### Test 15: Invalid Content
**Purpose**: Test handling of corrupted/invalid content

**Test Cases**:
1. **Navigate to pages with**:
   - Malformed HTML
   - Broken images
   - Invalid URLs

2. **Expected Behavior**:
   - ✅ No crashes
   - ✅ Graceful degradation
   - ✅ Filtering continues to work

## 📝 Testing Checklist

### Before Testing:
- [ ] Build solution successfully: `dotnet build`
- [ ] Check if NSFW model is present
- [ ] Clear browser cache/data
- [ ] Enable logging/diagnostics

### During Testing:
- [ ] Monitor application logs
- [ ] Check Windows Event Viewer for errors
- [ ] Monitor system resources
- [ ] Test with different user profiles

### After Testing:
- [ ] Document any issues found
- [ ] Verify all core features work
- [ ] Check log files for errors/warnings
- [ ] Validate Islamic family-safe requirements met

## 🚨 Critical Success Criteria

**The browser MUST**:
- ✅ Block inappropriate content effectively
- ✅ Handle missing NSFW model gracefully
- ✅ Maintain family-safe browsing environment
- ✅ Perform adequately (responsive UI)
- ✅ Not crash during normal usage
- ✅ Respect Islamic values and requirements

**If any critical criteria fail, the issue must be fixed before deployment.**
