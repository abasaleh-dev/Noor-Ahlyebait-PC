# AI Models for Content Filtering

This directory contains AI models used for content filtering in the Noor AhlulBayt Islamic Browser.

## NSFW Detection Model

### Required Model: nsfw-model.onnx

The browser uses an ONNX-based NSFW detection model to identify and filter inappropriate images automatically.

### Download Instructions

1. **Download the model from GitHub:**
   - Go to: https://github.com/iola1999/nsfw-detect-onnx/releases
   - Download the latest release (usually named `nsfw-detect-onnx-v1.0.0.zip` or similar)
   - Extract the ZIP file

2. **Copy the model file:**
   - From the extracted files, copy `nsfw-model.onnx` to this directory
   - The final path should be: `NoorAhlulBayt.Browser/Models/AI/nsfw-model.onnx`

3. **Verify the model:**
   - The file size should be approximately 25-30 MB
   - The browser will automatically detect and use the model when available

### Model Information

- **Source:** GantMan/nsfw_model converted to ONNX format
- **Categories:** The model classifies images into 5 categories:
  - `drawings` - Drawings/illustrations
  - `hentai` - Animated adult content
  - `neutral` - Safe content
  - `porn` - Explicit adult content
  - `sexy` - Suggestive content

- **Thresholds:** The browser uses these confidence thresholds:
  - Porn: 0.7 (70%)
  - Sexy: 0.6 (60%)
  - Hentai: 0.7 (70%)
  - Neutral: 0.3 (30% - content below this is considered safe)

### Fallback Behavior

If the NSFW model is not available:
- The browser will continue to function normally
- Basic URL-based filtering will still work
- Ad blocking and domain filtering remain active
- A warning will be logged indicating the model is not found

### Privacy and Security

- **Local Processing:** All image analysis is performed locally on your device
- **No Data Transmission:** Images are never sent to external servers for analysis
- **Offline Operation:** The model works completely offline
- **Islamic Values:** The filtering is designed to align with Islamic family values

### Technical Details

- **Format:** ONNX (Open Neural Network Exchange)
- **Input:** 299x299 RGB images
- **Output:** Confidence scores for each category
- **Performance:** Optimized for real-time web browsing
- **Memory Usage:** Approximately 100-200 MB when loaded

### Troubleshooting

If the model is not working:

1. **Check file location:** Ensure `nsfw-model.onnx` is in this exact directory
2. **Check file size:** The model should be 25-30 MB
3. **Check permissions:** Ensure the browser has read access to the file
4. **Check logs:** Look for NSFW-related messages in the browser logs
5. **Restart browser:** Restart the application after adding the model

### Alternative Models

While this specific model is recommended, you can potentially use other ONNX-based NSFW detection models by:
1. Renaming them to `nsfw-model.onnx`
2. Ensuring they have the same input/output format
3. Adjusting the confidence thresholds in the code if needed

### Updates

Check the GitHub repository periodically for model updates:
- https://github.com/iola1999/nsfw-detect-onnx

Updated models may provide better accuracy or performance improvements.
