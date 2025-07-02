using System;

namespace NoorAhlulBayt.Common.Services
{
    /// <summary>
    /// Service for injecting content filtering JavaScript into web pages
    /// </summary>
    public class ContentInjectionService
    {
        /// <summary>
        /// Gets the JavaScript code for content filtering with enhanced video thumbnail detection
        /// </summary>
        /// <returns>JavaScript code as string</returns>
        public string GetContentFilterScript()
        {
            return @"
(function() {
    'use strict';
    
    // Configuration
    const CONFIG = {
        checkInterval: 1000,
        imageCheckDelay: 500,
        maxRetries: 3
    };
    
    // Global variables
    let processedElements = new Set();
    let imageCheckQueue = [];
    let isProcessingQueue = false;
    let observer = null;
    
    // Initialize content filtering
    function initializeContentFilter() {
        console.log('Noor AhlulBayt Content Filter: Initializing...');
        
        // Process existing content
        processImages();
        processVideos();
        
        // Set up mutation observer for dynamic content
        setupMutationObserver();
        
        // Set up periodic checks
        setInterval(() => {
            processImages();
            processVideos();
        }, CONFIG.checkInterval);
        
        console.log('Noor AhlulBayt Content Filter: Initialized successfully');
    }
    
    // Function to process images and background images
    function processImages() {
        // Process regular img elements
        const images = document.querySelectorAll('img[src]:not([data-noor-processed])');
        
        for (const img of images) {
            if (processedElements.has(img)) continue;
            
            processedElements.add(img);
            img.setAttribute('data-noor-processed', 'true');
            
            // Skip data URLs and very small images (likely icons)
            if (img.src.startsWith('data:') || 
                (img.naturalWidth > 0 && img.naturalWidth < 50 && img.naturalHeight < 50)) {
                continue;
            }
            
            // Add to queue for checking
            imageCheckQueue.push({ element: img, src: img.src, type: 'img' });
        }
        
        // Process elements with background images
        const elementsWithBg = document.querySelectorAll('*:not([data-noor-bg-processed])');
        for (const element of elementsWithBg) {
            if (processedElements.has(element)) continue;
            
            const computedStyle = window.getComputedStyle(element);
            const bgImage = computedStyle.backgroundImage;
            
            if (bgImage && bgImage !== 'none' && bgImage.includes('url(')) {
                element.setAttribute('data-noor-bg-processed', 'true');
                processedElements.add(element);
                
                // Extract URL from background-image
                const urlPattern = /url\(['""]?([^'""]+)['""]?\)/;
                const urlMatch = bgImage.match(urlPattern);
                if (urlMatch && urlMatch[1] && !urlMatch[1].startsWith('data:')) {
                    const bgUrl = urlMatch[1];
                    
                    // Skip very small background images (likely decorative)
                    if (element.offsetWidth > 100 && element.offsetHeight > 100) {
                        imageCheckQueue.push({ element: element, src: bgUrl, type: 'background' });
                    }
                }
            }
        }
        
        // Process video poster images
        const videoPosters = document.querySelectorAll('video[poster]:not([data-noor-poster-processed])');
        for (const video of videoPosters) {
            if (video.poster && !video.poster.startsWith('data:')) {
                video.setAttribute('data-noor-poster-processed', 'true');
                imageCheckQueue.push({ element: video, src: video.poster, type: 'poster' });
            }
        }
        
        processImageQueue();
    }
    
    // Function to process image queue
    async function processImageQueue() {
        if (isProcessingQueue || imageCheckQueue.length === 0) return;
        
        isProcessingQueue = true;
        
        while (imageCheckQueue.length > 0) {
            const item = imageCheckQueue.shift();
            
            try {
                const result = await checkWithBackend('nsfw', item.src);
                if (result.isBlocked) {
                    switch (item.type) {
                        case 'img':
                            blockImage(item.element, result.reason || 'Inappropriate content detected');
                            break;
                        case 'background':
                            blockBackgroundImage(item.element, result.reason || 'Inappropriate background image detected');
                            break;
                        case 'poster':
                            blockVideo(item.element, result.reason || 'Video thumbnail contains inappropriate content');
                            break;
                    }
                }
            } catch (error) {
                console.warn('NSFW check failed for:', item.type, item.src, error);
            }
            
            // Small delay to prevent overwhelming the backend
            await new Promise(resolve => setTimeout(resolve, 100));
        }
        
        isProcessingQueue = false;
    }
    
    // Function to block background images
    function blockBackgroundImage(element, reason) {
        element.style.backgroundImage = 'none';
        element.style.backgroundColor = '#f0f0f0';
        element.style.border = '2px dashed #ccc';
        element.style.position = 'relative';
        
        // Add overlay message
        const overlay = document.createElement('div');
        overlay.style.cssText = 
            'position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%); ' +
            'background: rgba(240, 240, 240, 0.9); padding: 10px; border-radius: 5px; ' +
            'font-family: Arial, sans-serif; font-size: 12px; color: #666; text-align: center; ' +
            'z-index: 1000; cursor: pointer;';
        overlay.textContent = 'Background blocked';
        overlay.title = reason;
        overlay.onclick = function() {
            alert('Background image blocked: ' + reason);
        };
        
        element.appendChild(overlay);
    }
    
    // Function to process videos and their thumbnails
    async function processVideos() {
        const videos = document.querySelectorAll('video:not([data-noor-processed])');
        
        for (const video of videos) {
            if (processedElements.has(video)) continue;
            
            processedElements.add(video);
            video.setAttribute('data-noor-processed', 'true');
            
            // Check video poster/thumbnail first
            if (video.poster && !video.poster.startsWith('data:')) {
                try {
                    const posterResult = await checkWithBackend('nsfw', video.poster);
                    if (posterResult.isBlocked) {
                        blockVideo(video, posterResult.reason || 'Video thumbnail contains inappropriate content');
                        continue; // Skip further processing if thumbnail is blocked
                    }
                } catch (error) {
                    console.warn('Video poster check failed:', video.poster, error);
                }
            }
            
            // Get video source for URL-based filtering
            let videoSrc = video.src;
            if (!videoSrc) {
                const source = video.querySelector('source');
                if (source) videoSrc = source.src;
            }
            
            if (videoSrc && !videoSrc.startsWith('data:')) {
                try {
                    const result = await checkWithBackend('video', videoSrc);
                    if (result.isBlocked) {
                        blockVideo(video, result.reason || 'Inappropriate video content detected');
                    }
                } catch (error) {
                    console.warn('Video URL check failed:', videoSrc, error);
                }
            }
            
            // Set up thumbnail capture for videos without poster
            if (!video.poster && videoSrc) {
                setupVideoThumbnailCapture(video);
            }
        }
    }
    
    // Function to capture and analyze video thumbnails
    function setupVideoThumbnailCapture(video) {
        const captureFrame = async () => {
            if (video.videoWidth === 0 || video.videoHeight === 0) return;
            
            try {
                // Create canvas to capture frame
                const canvas = document.createElement('canvas');
                const ctx = canvas.getContext('2d');
                canvas.width = Math.min(video.videoWidth, 640);
                canvas.height = Math.min(video.videoHeight, 480);
                
                // Draw current frame
                ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
                
                // Convert to blob and analyze
                canvas.toBlob(async (blob) => {
                    if (blob) {
                        const reader = new FileReader();
                        reader.onload = async (e) => {
                            try {
                                const result = await checkWithBackend('nsfw', e.target.result);
                                if (result.isBlocked) {
                                    blockVideo(video, result.reason || 'Video content contains inappropriate material');
                                }
                            } catch (error) {
                                console.warn('Video frame analysis failed:', error);
                            }
                        };
                        reader.readAsDataURL(blob);
                    }
                }, 'image/jpeg', 0.8);
            } catch (error) {
                console.warn('Video frame capture failed:', error);
            }
        };
        
        // Capture frame when video starts playing
        video.addEventListener('loadeddata', captureFrame, { once: true });
        video.addEventListener('play', captureFrame, { once: true });
    }

    // Function to set up mutation observer for dynamic content
    function setupMutationObserver() {
        if (observer) observer.disconnect();

        observer = new MutationObserver((mutations) => {
            let hasNewContent = false;

            mutations.forEach((mutation) => {
                if (mutation.type === 'childList') {
                    mutation.addedNodes.forEach((node) => {
                        if (node.nodeType === Node.ELEMENT_NODE) {
                            hasNewContent = true;
                        }
                    });
                }
            });

            if (hasNewContent) {
                setTimeout(() => {
                    processImages();
                    processVideos();
                }, CONFIG.imageCheckDelay);
            }
        });

        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
    }

    // Function to communicate with backend
    async function checkWithBackend(type, url) {
        return new Promise((resolve, reject) => {
            const message = {
                type: 'contentCheck',
                checkType: type,
                url: url,
                timestamp: Date.now()
            };

            // Send message to WebView2 backend
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage(JSON.stringify(message));

                // Set up response handler
                const responseHandler = (event) => {
                    try {
                        const response = JSON.parse(event.data);
                        if (response.timestamp === message.timestamp) {
                            window.removeEventListener('message', responseHandler);
                            resolve(response);
                        }
                    } catch (error) {
                        reject(error);
                    }
                };

                window.addEventListener('message', responseHandler);

                // Timeout after 5 seconds
                setTimeout(() => {
                    window.removeEventListener('message', responseHandler);
                    reject(new Error('Backend check timeout'));
                }, 5000);
            } else {
                reject(new Error('WebView2 not available'));
            }
        });
    }

    // Function to block images
    function blockImage(img, reason) {
        const container = document.createElement('div');
        container.style.cssText =
            'display: inline-block; width: ' + (img.width || 200) + 'px; height: ' + (img.height || 150) + 'px; ' +
            'background: #f0f0f0; border: 2px dashed #ccc; position: relative; text-align: center; ' +
            'font-family: Arial, sans-serif; font-size: 12px; color: #666; cursor: pointer;';

        const message = document.createElement('div');
        message.style.cssText =
            'position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%); ' +
            'padding: 10px; background: rgba(255, 255, 255, 0.9); border-radius: 5px;';
        message.textContent = 'Image blocked';
        message.title = reason;

        container.appendChild(message);
        container.onclick = function() {
            alert('Image blocked: ' + reason);
        };

        img.parentNode.replaceChild(container, img);
    }

    // Function to block videos
    function blockVideo(video, reason) {
        const container = document.createElement('div');
        container.style.cssText =
            'display: inline-block; width: ' + (video.width || 320) + 'px; height: ' + (video.height || 240) + 'px; ' +
            'background: #f0f0f0; border: 2px dashed #ccc; position: relative; text-align: center; ' +
            'font-family: Arial, sans-serif; font-size: 14px; color: #666; cursor: pointer;';

        const message = document.createElement('div');
        message.style.cssText =
            'position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%); ' +
            'padding: 15px; background: rgba(255, 255, 255, 0.9); border-radius: 5px;';
        message.textContent = 'Video blocked';
        message.title = reason;

        container.appendChild(message);
        container.onclick = function() {
            alert('Video blocked: ' + reason);
        };

        video.parentNode.replaceChild(container, video);
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeContentFilter);
    } else {
        initializeContentFilter();
    }
})();
";
        }

        /// <summary>
        /// Gets the CSS styles for blocked content
        /// </summary>
        /// <returns>CSS styles as string</returns>
        public string GetContentFilterStyles()
        {
            return @"
.noor-blocked-content {
    background: #f0f0f0 !important;
    border: 2px dashed #ccc !important;
    display: flex !important;
    align-items: center !important;
    justify-content: center !important;
    font-family: Arial, sans-serif !important;
    font-size: 12px !important;
    color: #666 !important;
    cursor: pointer !important;
    position: relative !important;
}

.noor-blocked-content:hover {
    background: #e0e0e0 !important;
    border-color: #999 !important;
}

.noor-blocked-overlay {
    position: absolute !important;
    top: 50% !important;
    left: 50% !important;
    transform: translate(-50%, -50%) !important;
    background: rgba(255, 255, 255, 0.9) !important;
    padding: 10px !important;
    border-radius: 5px !important;
    box-shadow: 0 2px 5px rgba(0,0,0,0.2) !important;
    z-index: 1000 !important;
}
";
        }
    }
}
