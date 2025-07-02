using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Collections.Concurrent;

namespace NoorAhlulBayt.Common.Services;

/// <summary>
/// Service for detecting NSFW (Not Safe For Work) content in images using ONNX model
/// </summary>
public class NsfwDetectionService : IDisposable
{
    private readonly InferenceSession? _session;
    private readonly string[] _categories = { "drawings", "hentai", "neutral", "porn", "sexy" };
    private readonly ConcurrentDictionary<string, NsfwResult> _cache = new();
    private readonly object _sessionLock = new();
    private bool _disposed = false;

    // Thresholds for different content types
    private const float PornThreshold = 0.7f;
    private const float SexyThreshold = 0.6f;
    private const float HentaiThreshold = 0.7f;
    private const float NeutralThreshold = 0.3f; // Minimum neutral score required

    public bool IsModelLoaded => _session != null;

    public NsfwDetectionService(string? modelPath = null)
    {
        try
        {
            // Try to load the model if path is provided
            if (!string.IsNullOrEmpty(modelPath) && File.Exists(modelPath))
            {
                var sessionOptions = new SessionOptions();
                sessionOptions.LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_WARNING;
                
                _session = new InferenceSession(modelPath, sessionOptions);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't throw - service can still work without model
            Console.WriteLine($"Failed to load NSFW detection model: {ex.Message}");
        }
    }

    /// <summary>
    /// Analyzes an image from a URL or data URL for NSFW content
    /// </summary>
    /// <param name="imageUrl">URL or data URL of the image to analyze</param>
    /// <param name="httpClient">HttpClient for downloading the image (not used for data URLs)</param>
    /// <returns>NSFW analysis result</returns>
    public async Task<NsfwResult> AnalyzeImageAsync(string imageUrl, HttpClient httpClient)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return new NsfwResult { IsNsfw = false, Reason = "Empty URL" };

        // Check cache first
        if (_cache.TryGetValue(imageUrl, out var cachedResult))
            return cachedResult;

        try
        {
            // If no model is loaded, use basic URL-based filtering
            if (_session == null)
            {
                var urlResult = AnalyzeImageUrl(imageUrl);
                _cache.TryAdd(imageUrl, urlResult);
                return urlResult;
            }

            // Handle data URLs (base64 encoded images from video frames)
            if (imageUrl.StartsWith("data:image/"))
            {
                try
                {
                    var base64Data = imageUrl.Substring(imageUrl.IndexOf(',') + 1);
                    var imageBytes = Convert.FromBase64String(base64Data);
                    using var stream = new MemoryStream(imageBytes);
                    var dataUrlResult = await AnalyzeImageStreamAsync(stream);
                    dataUrlResult.ImageUrl = imageUrl;
                    _cache.TryAdd(imageUrl, dataUrlResult);
                    return dataUrlResult;
                }
                catch (Exception ex)
                {
                    return new NsfwResult { IsNsfw = false, Reason = $"Failed to process data URL: {ex.Message}" };
                }
            }

            // Download and analyze the image
            using var response = await httpClient.GetAsync(imageUrl);
            if (!response.IsSuccessStatusCode)
            {
                return new NsfwResult { IsNsfw = false, Reason = "Failed to download image" };
            }

            using var imageStream = await response.Content.ReadAsStreamAsync();
            var result = await AnalyzeImageStreamAsync(imageStream);
            result.ImageUrl = imageUrl;

            // Cache the result
            _cache.TryAdd(imageUrl, result);
            return result;
        }
        catch (Exception ex)
        {
            var errorResult = new NsfwResult 
            { 
                IsNsfw = false, 
                Reason = $"Analysis error: {ex.Message}",
                ImageUrl = imageUrl
            };
            _cache.TryAdd(imageUrl, errorResult);
            return errorResult;
        }
    }

    /// <summary>
    /// Analyzes an image stream for NSFW content
    /// </summary>
    /// <param name="imageStream">Stream containing the image data</param>
    /// <returns>NSFW analysis result</returns>
    public async Task<NsfwResult> AnalyzeImageStreamAsync(Stream imageStream)
    {
        if (_session == null)
        {
            return new NsfwResult { IsNsfw = false, Reason = "NSFW model not loaded" };
        }

        try
        {
            // Load and preprocess the image
            using var image = await Image.LoadAsync<Rgb24>(imageStream);
            var inputTensor = PreprocessImage(image);

            // Run inference
            lock (_sessionLock)
            {
                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor(_session.InputMetadata.Keys.First(), inputTensor)
                };

                using var results = _session.Run(inputs);
                var output = results.First().AsEnumerable<float>().ToArray();

                return ProcessModelOutput(output);
            }
        }
        catch (Exception ex)
        {
            return new NsfwResult 
            { 
                IsNsfw = false, 
                Reason = $"Image analysis error: {ex.Message}" 
            };
        }
    }

    /// <summary>
    /// Basic URL-based NSFW filtering (fallback when model is not available)
    /// </summary>
    /// <param name="imageUrl">URL to analyze</param>
    /// <returns>Basic NSFW result based on URL patterns</returns>
    private NsfwResult AnalyzeImageUrl(string imageUrl)
    {
        var lowerUrl = imageUrl.ToLowerInvariant();
        
        // Check for obvious NSFW domains and patterns
        var nsfwPatterns = new[]
        {
            "porn", "xxx", "sex", "nude", "naked", "adult", "nsfw",
            "erotic", "fetish", "cam", "escort", "dating", "hookup"
        };

        foreach (var pattern in nsfwPatterns)
        {
            if (lowerUrl.Contains(pattern))
            {
                return new NsfwResult
                {
                    IsNsfw = true,
                    Reason = $"URL contains NSFW pattern: {pattern}",
                    ImageUrl = imageUrl,
                    Confidence = 0.9f
                };
            }
        }

        return new NsfwResult { IsNsfw = false, Reason = "URL appears safe", ImageUrl = imageUrl };
    }

    /// <summary>
    /// Preprocesses an image for the ONNX model
    /// </summary>
    /// <param name="image">Input image</param>
    /// <returns>Preprocessed tensor</returns>
    private DenseTensor<float> PreprocessImage(Image<Rgb24> image)
    {
        // Resize image to 299x299 (model input size)
        image.Mutate(x => x.Resize(299, 299));

        // Create tensor with shape [1, 299, 299, 3]
        var tensor = new DenseTensor<float>(new[] { 1, 299, 299, 3 });

        // Convert image to tensor and normalize to [0, 1]
        for (int y = 0; y < 299; y++)
        {
            for (int x = 0; x < 299; x++)
            {
                var pixel = image[x, y];
                tensor[0, y, x, 0] = pixel.R / 255.0f; // Red
                tensor[0, y, x, 1] = pixel.G / 255.0f; // Green
                tensor[0, y, x, 2] = pixel.B / 255.0f; // Blue
            }
        }

        return tensor;
    }

    /// <summary>
    /// Processes the model output to determine if content is NSFW
    /// </summary>
    /// <param name="output">Model output probabilities</param>
    /// <returns>NSFW analysis result</returns>
    private NsfwResult ProcessModelOutput(float[] output)
    {
        // Create dictionary of category scores
        var scores = new Dictionary<string, float>();
        for (int i = 0; i < _categories.Length && i < output.Length; i++)
        {
            scores[_categories[i]] = output[i];
        }

        // Determine if content is NSFW based on thresholds
        var pornScore = scores.GetValueOrDefault("porn", 0f);
        var sexyScore = scores.GetValueOrDefault("sexy", 0f);
        var hentaiScore = scores.GetValueOrDefault("hentai", 0f);
        var neutralScore = scores.GetValueOrDefault("neutral", 0f);

        bool isNsfw = false;
        string reason = "Content appears safe";
        float confidence = neutralScore;

        if (pornScore > PornThreshold)
        {
            isNsfw = true;
            reason = "Pornographic content detected";
            confidence = pornScore;
        }
        else if (hentaiScore > HentaiThreshold)
        {
            isNsfw = true;
            reason = "Hentai content detected";
            confidence = hentaiScore;
        }
        else if (sexyScore > SexyThreshold && neutralScore < NeutralThreshold)
        {
            isNsfw = true;
            reason = "Sexually suggestive content detected";
            confidence = sexyScore;
        }

        return new NsfwResult
        {
            IsNsfw = isNsfw,
            Reason = reason,
            Confidence = confidence,
            CategoryScores = scores
        };
    }

    /// <summary>
    /// Clears the analysis cache
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    /// <returns>Number of cached results</returns>
    public int GetCacheSize()
    {
        return _cache.Count;
    }

    /// <summary>
    /// Checks multiple images for NSFW content with batching for better performance
    /// </summary>
    /// <param name="imageUrls">List of image URLs to check</param>
    /// <param name="batchSize">Number of images to process concurrently (default: 5)</param>
    /// <returns>Dictionary mapping URLs to NSFW results</returns>
    public async Task<Dictionary<string, NsfwResult>> CheckNsfwImagesAsync(IEnumerable<string> imageUrls, int batchSize = 5)
    {
        var results = new Dictionary<string, NsfwResult>();
        var urlList = imageUrls.ToList();

        // Create HttpClient for batch processing
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(10);

        // Process in batches to avoid overwhelming the system
        for (int i = 0; i < urlList.Count; i += batchSize)
        {
            var batch = urlList.Skip(i).Take(batchSize);
            var tasks = batch.Select(async url =>
            {
                try
                {
                    var result = await AnalyzeImageAsync(url, httpClient);
                    return new { Url = url, Result = result, Success = true };
                }
                catch (Exception ex)
                {
                    return new {
                        Url = url,
                        Result = new NsfwResult { IsNsfw = false, Reason = $"Analysis failed: {ex.Message}" },
                        Success = false
                    };
                }
            });

            var completedTasks = await Task.WhenAll(tasks);
            foreach (var task in completedTasks)
            {
                results[task.Url] = task.Result;
            }

            // Small delay between batches to prevent overwhelming the system
            if (i + batchSize < urlList.Count)
            {
                await Task.Delay(50);
            }
        }

        return results;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _session?.Dispose();
            _cache.Clear();
            _disposed = true;
        }
    }
}

/// <summary>
/// Result of NSFW content analysis
/// </summary>
public class NsfwResult
{
    public bool IsNsfw { get; set; }
    public string Reason { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public string? ImageUrl { get; set; }
    public Dictionary<string, float>? CategoryScores { get; set; }
}
