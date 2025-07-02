# Comprehensive Testing Guide for Noor-e-AhlulBayt Islamic Browser

## Overview

This guide provides comprehensive testing procedures for all content filtering features, with special emphasis on image/video filtering as the most critical feature.

## Prerequisites

### 1. NSFW Model Setup (CRITICAL)
**Status**: ⚠️ **REQUIRED** - Model must be downloaded manually

```bash
# 1. Download NSFW detection model
# Go to: https://github.com/iola1999/nsfw-detect-onnx/releases
# Download: nsfw-detect-onnx-v1.0.0.zip (or latest version)
# Extract and copy nsfw-model.onnx to:
# NoorAhlulBayt.Browser/Models/AI/nsfw-model.onnx

# 2. Verify model installation
ls -la "NoorAhlulBayt.Browser/Models/AI/nsfw-model.onnx"
# File should be ~25-30 MB
```

### 2. Build and Test Setup
```bash
# Build the solution
dotnet build

# Run automated tests
dotnet test NoorAhlulBayt.Tests --verbosity normal

# Run with coverage (optional)
dotnet test NoorAhlulBayt.Tests --collect:"XPlat Code Coverage"
```

## Testing Categories

## 1. NSFW Image Detection Testing (HIGHEST PRIORITY)

### A. Automated Testing
```bash
# Run NSFW-specific tests
dotnet test NoorAhlulBayt.Tests --filter "Category=NSFW" --verbosity detailed
```

### B. Manual Testing Procedures

#### Test Case 1: Model Verification
1. **Start the browser application**
2. **Check logs** for NSFW model loading status:
   - Look for: "NSFW Model: Loaded" or "NSFW Model: Not found"
   - Location: Application logs or console output

#### Test Case 2: Safe Image Testing
**Test URLs** (Safe content):
```
https://picsum.photos/300/200?random=1
https://via.placeholder.com/400x300/0000FF/FFFFFF?text=Test+Image
https://httpbin.org/image/jpeg
```

**Expected Behavior**:
- Images should load normally
- No blocking overlay should appear
- No NSFW warnings in logs

#### Test Case 3: URL-Based Filtering (Fallback)
**Test URLs** (Should be blocked by URL analysis):
```
https://example.com/adult-content.jpg
https://example.com/xxx-image.png  
https://example.com/porn-thumbnail.gif
https://example.com/nude-photo.jpg
```

**Expected Behavior**:
- Images should be blocked with overlay
- Reason: "URL contains inappropriate keywords"
- Blocking message should appear

#### Test Case 4: Real-World Image Testing
**Safe Test Images**:
- Nature photography sites
- News websites with appropriate images
- Educational content with diagrams

**Monitoring Points**:
- Check browser console for NSFW analysis logs
- Verify confidence scores in logs
- Ensure no false positives on family-safe content

### C. Performance Testing
```bash
# Test multiple images simultaneously
# Navigate to image-heavy websites like:
# - News sites with photo galleries
# - Educational sites with illustrations
# - Shopping sites with product images

# Monitor:
# - CPU usage during image analysis
# - Memory consumption
# - Response time for image loading
```

## 2. Profanity Filtering Testing

### A. Automated Testing
```bash
dotnet test NoorAhlulBayt.Tests --filter "TestCategory=Profanity"
```

### B. Manual Testing

#### Test Case 1: Basic Profanity Detection
**Test Content**:
```
Navigate to pages or enter text containing:
- "This is damn annoying"
- "What the hell is this?"
- "This content is crap"
- "Don't be stupid"
```

**Expected**: Content should be blocked with profanity warning

#### Test Case 2: Regex Pattern Detection
**Test Content**:
```
- "Adult entertainment websites"
- "XXX rated content here"
- "Casino gambling games"
- "Poker betting sites"
```

**Expected**: Content blocked with "Inappropriate content detected"

#### Test Case 3: Clean Content Verification
**Test Content**:
```
- "This is completely appropriate content"
- "Family-friendly educational material"
- "Islamic teachings and guidance"
```

**Expected**: Content should load normally

## 3. Domain Blacklist Testing

### Test Cases
```bash
# Test blocked domains (should be blocked):
https://pornhub.com
https://xvideos.com
https://casino.com
https://bet365.com

# Test allowed domains (should work):
https://google.com
https://wikipedia.org
https://stackoverflow.com
```

## 4. Ad Blocking Testing

### Test URLs
```bash
# Test ad/tracking URLs (should be blocked):
https://googleads.g.doubleclick.net/pagead/ads
https://facebook.com/tr?id=123456
https://google-analytics.com/collect
https://googletagmanager.com/gtm.js

# Test legitimate content (should work):
https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/css/bootstrap.min.css
```

## 5. Safe Search Enforcement Testing

### Test Cases
```bash
# Test search engines - verify safe search parameters are added:
# Google: https://google.com/search?q=test → should add &safe=strict
# Bing: https://bing.com/search?q=test → should add &adlt=strict
# DuckDuckGo: https://duckduckgo.com/?q=test → should add &safe-search=strict
```

## 6. Integration Testing

### Full Workflow Tests

#### Test Case 1: Complete Filtering Pipeline
1. Start browser
2. Navigate to mixed content page (images + text)
3. Verify all filters work together:
   - NSFW images blocked
   - Profanity in text blocked
   - Ads blocked
   - Safe content allowed

#### Test Case 2: Performance Under Load
1. Open multiple tabs
2. Navigate to image-heavy sites
3. Monitor system resources
4. Verify filtering still works correctly

## 7. Error Handling Testing

### Test Cases
1. **Network Issues**: Test with poor internet connection
2. **Invalid URLs**: Test with malformed image URLs
3. **Large Images**: Test with very large image files
4. **Corrupted Images**: Test with invalid image data

## Testing Tools and Resources

### Recommended Testing Websites
**Safe Content**:
- https://picsum.photos (placeholder images)
- https://httpbin.org/image (test images)
- https://via.placeholder.com (placeholder service)

**Educational Content**:
- Wikipedia articles with images
- Khan Academy educational content
- Islamic educational websites

### Monitoring Tools
1. **Browser Developer Tools**: Monitor network requests and console logs
2. **Windows Task Manager**: Monitor CPU and memory usage
3. **Application Logs**: Check Serilog output files

### Test Data Creation
```bash
# Create test image datasets:
mkdir TestData/Images/Safe
mkdir TestData/Images/Suspicious
mkdir TestData/Text/Clean
mkdir TestData/Text/Inappropriate

# Populate with appropriate test content
```

## Validation Criteria

### NSFW Detection Success Criteria
- ✅ Model loads successfully (if available)
- ✅ URL-based filtering works as fallback
- ✅ Safe images load without blocking
- ✅ Suspicious URLs are blocked
- ✅ Performance remains acceptable (<2s per image)
- ✅ No false positives on family-safe content

### Overall System Success Criteria
- ✅ All automated tests pass
- ✅ Manual test cases complete successfully
- ✅ No crashes or exceptions during normal use
- ✅ Acceptable performance (browsing feels responsive)
- ✅ Islamic family values maintained (conservative filtering)

## Troubleshooting

### Common Issues
1. **NSFW Model Not Loading**:
   - Verify file exists and is ~25-30 MB
   - Check file permissions
   - Review application logs

2. **False Positives**:
   - Review confidence thresholds
   - Check profanity word lists
   - Verify regex patterns

3. **Performance Issues**:
   - Monitor memory usage
   - Check for memory leaks
   - Optimize image processing

## Next Steps After Testing
1. Document all test results
2. Fix any identified issues
3. Optimize performance bottlenecks
4. Enhance filtering accuracy
5. Prepare for user acceptance testing
