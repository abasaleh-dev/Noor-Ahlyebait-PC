# Fonts for Noor-e-AhlulBayt Islamic Browser

This directory contains fonts used in the Islamic family-safe browser application.

## Scheherazade Font

### Required Font: Scheherazade-Regular.ttf

The browser uses the Scheherazade font for displaying Arabic text content with proper Islamic typography.

### Download Instructions

1. **Download the font from Google Fonts:**
   - Go to: https://fonts.google.com/specimen/Scheherazade+New
   - Click "Download family"
   - Extract the ZIP file

2. **Copy the font file:**
   - From the extracted files, copy `ScheherazadeNew-Regular.ttf` to this directory
   - Rename it to `Scheherazade-Regular.ttf`
   - The final path should be: `NoorAhlulBayt.Browser/Fonts/Scheherazade-Regular.ttf`

### Alternative Download

You can also download directly from SIL International:
- Go to: https://software.sil.org/scheherazade/
- Download the latest version
- Extract and copy the regular weight font file

### Font Features

- **Arabic Script Support**: Full support for Arabic, Persian, and Urdu text
- **Islamic Typography**: Designed specifically for Islamic texts
- **Unicode Compliance**: Supports all Arabic Unicode ranges
- **Readability**: Optimized for both print and screen display

### Usage in Application

The font is automatically applied to:
- Arabic text content in web pages
- Islamic menu items and labels
- Prayer time displays
- Any text marked with Arabic language attributes

### Fallback Behavior

If the Scheherazade font is not available:
- The browser will fall back to system Arabic fonts
- Text will still display correctly but may not have the Islamic styling
- A warning will be logged indicating the font is not found

### Technical Details

- **Format**: TrueType Font (.ttf)
- **Weight**: Regular (400)
- **Style**: Normal
- **Language Support**: Arabic, Persian, Urdu, and other Arabic-script languages
- **License**: SIL Open Font License (free for all use)

### Troubleshooting

If Arabic text is not displaying correctly:

1. **Check font location**: Ensure `Scheherazade-Regular.ttf` is in this exact directory
2. **Check file permissions**: Ensure the browser has read access to the font file
3. **Restart browser**: Restart the application after adding the font
4. **Check logs**: Look for font-related messages in the browser logs
