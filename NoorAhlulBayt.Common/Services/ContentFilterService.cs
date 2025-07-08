using System.Text.RegularExpressions;
using System.Text.Json;

namespace NoorAhlulBayt.Common.Services;

public class ContentFilterService
{
    private readonly HashSet<string> _profanityWords;
    private readonly HashSet<string> _adBlockRules;
    private readonly List<Regex> _profanityRegexes;
    private readonly List<Regex> _adBlockRegexes;
    private readonly NsfwDetectionService _nsfwDetectionService;
    private readonly UrlClassificationService _urlClassificationService;
    private readonly HttpClient _httpClient;

    // Address Bar Keyword Guard thresholds
    private const float HIGH_CONFIDENCE_THRESHOLD = 0.8f;
    private const float MEDIUM_CONFIDENCE_THRESHOLD = 0.5f;

    public ContentFilterService(string? nsfwModelPath = null)
    {
        _profanityWords = LoadProfanityWords();
        _adBlockRules = LoadAdBlockRules();
        _profanityRegexes = CreateProfanityRegexes();
        _adBlockRegexes = CreateAdBlockRegexes();
        _nsfwDetectionService = new NsfwDetectionService(nsfwModelPath);
        _urlClassificationService = new UrlClassificationService();
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "NoorAhlulBayt-Browser/1.0");
    }

    /// <summary>
    /// Checks if content contains profanity
    /// </summary>
    /// <param name="content">Text content to check</param>
    /// <returns>FilterResult with details</returns>
    public FilterResult CheckProfanity(string content)
    {
        if (string.IsNullOrEmpty(content))
            return new FilterResult { IsBlocked = false };

        var lowerContent = content.ToLowerInvariant();

        // Check against word list
        foreach (var word in _profanityWords)
        {
            if (lowerContent.Contains(word))
            {
                return new FilterResult
                {
                    IsBlocked = true,
                    Reason = "Profanity detected",
                    BlockedContent = word,
                    FilterType = FilterType.Profanity
                };
            }
        }

        // Check against regex patterns
        foreach (var regex in _profanityRegexes)
        {
            var match = regex.Match(lowerContent);
            if (match.Success)
            {
                return new FilterResult
                {
                    IsBlocked = true,
                    Reason = "Inappropriate content detected",
                    BlockedContent = match.Value,
                    FilterType = FilterType.Profanity
                };
            }
        }

        return new FilterResult { IsBlocked = false };
    }

    /// <summary>
    /// Checks if a domain should be blocked based on whitelist/blacklist
    /// </summary>
    /// <param name="url">URL to check</param>
    /// <returns>FilterResult with domain check details</returns>
    public FilterResult CheckDomain(string url)
    {
        if (string.IsNullOrEmpty(url))
            return new FilterResult { IsBlocked = false };

        try
        {
            var uri = new Uri(url);
            var domain = uri.Host.ToLowerInvariant();

            // Check against blacklisted domains (comprehensive Islamic family-safe list)
            var blacklistedDomains = new[]
            {
                // Adult content sites
                "pornhub.com", "xvideos.com", "xnxx.com", "redtube.com", "youporn.com",
                "tube8.com", "spankbang.com", "xhamster.com", "brazzers.com", "bangbros.com",
                "realitykings.com", "naughtyamerica.com", "digitalplayground.com", "twistys.com",
                "playboy.com", "penthouse.com", "hustler.com", "vivid.com", "wicked.com",
                "kink.com", "burningangel.com", "evilangel.com", "teamskeet.com", "mofos.com",
                "fakehub.com", "sexart.com", "metart.com", "hegre.com", "femjoy.com",
                "eroticbeauty.com", "thelifeerotic.com", "stunning18.com", "nubiles.com",
                "18onlygirls.com", "babes.com", "anilos.com", "puremature.com", "mylf.com",

                // Gambling sites
                "casino.com", "bet365.com", "pokerstars.com", "888casino.com", "betfair.com",
                "ladbrokes.com", "williamhill.com", "paddypower.com", "coral.co.uk", "skybet.com",
                "betway.com", "unibet.com", "bwin.com", "partypoker.com", "fulltilt.com",
                "bovada.lv", "ignitioncasino.eu", "slots.lv", "cafecasino.lv", "bitstarz.com",

                // Dating/hookup sites
                "tinder.com", "bumble.com", "adultfriendfinder.com", "ashley-madison.com",
                "match.com", "eharmony.com", "pof.com", "okcupid.com", "zoosk.com",
                "badoo.com", "meetme.com", "skout.com", "grindr.com", "scruff.com",

                // Alcohol/drug related
                "budweiser.com", "corona.com", "heineken.com", "absolut.com", "bacardi.com",
                "jackdaniels.com", "johnniewalker.com", "smirnoff.com", "greygoose.com",
                "leafly.com", "weedmaps.com", "marijuana.com", "cannabis.com", "hightimes.com"
            };

            // Check exact domain matches
            foreach (var blacklisted in blacklistedDomains)
            {
                if (domain.Contains(blacklisted))
                {
                    return new FilterResult
                    {
                        IsBlocked = true,
                        Reason = $"Domain blocked for family safety: {blacklisted}",
                        BlockedContent = domain,
                        FilterType = FilterType.Domain
                    };
                }
            }

            // Check domain patterns for additional protection
            var suspiciousPatterns = new[]
            {
                "xxx", "porn", "sex", "nude", "naked", "adult", "erotic", "fetish",
                "casino", "poker", "bet", "gambling", "slots", "blackjack",
                "escort", "hookup", "dating", "cam", "webcam", "live",
                "alcohol", "beer", "wine", "vodka", "whiskey", "marijuana", "weed", "cannabis"
            };

            foreach (var pattern in suspiciousPatterns)
            {
                if (domain.Contains(pattern))
                {
                    return new FilterResult
                    {
                        IsBlocked = true,
                        Reason = $"Domain contains inappropriate content pattern: {pattern}",
                        BlockedContent = domain,
                        FilterType = FilterType.Domain
                    };
                }
            }

            return new FilterResult { IsBlocked = false };
        }
        catch (UriFormatException)
        {
            // Invalid URL format - don't block
            return new FilterResult { IsBlocked = false, Reason = "Invalid URL format" };
        }
    }

    /// <summary>
    /// Checks if an image URL contains NSFW content
    /// </summary>
    /// <param name="imageUrl">URL of the image to check</param>
    /// <returns>FilterResult with NSFW analysis</returns>
    public async Task<FilterResult> CheckNsfwImageAsync(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return new FilterResult { IsBlocked = false };

        try
        {
            var nsfwResult = await _nsfwDetectionService.AnalyzeImageAsync(imageUrl, _httpClient);

            if (nsfwResult.IsNsfw)
            {
                return new FilterResult
                {
                    IsBlocked = true,
                    Reason = nsfwResult.Reason,
                    BlockedContent = imageUrl,
                    FilterType = FilterType.NSFW,
                    Confidence = nsfwResult.Confidence
                };
            }

            return new FilterResult { IsBlocked = false };
        }
        catch (Exception ex)
        {
            // Log error but don't block - better to allow than false positive
            return new FilterResult
            {
                IsBlocked = false,
                Reason = $"NSFW check failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Checks multiple image URLs for NSFW content
    /// </summary>
    /// <param name="imageUrls">List of image URLs to check</param>
    /// <returns>List of FilterResults</returns>
    public async Task<List<FilterResult>> CheckNsfwImagesAsync(IEnumerable<string> imageUrls)
    {
        var tasks = imageUrls.Select(CheckNsfwImageAsync);
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    /// <summary>
    /// Checks if URL should be blocked by ad blocker
    /// </summary>
    /// <param name="url">URL to check</param>
    /// <returns>FilterResult with blocking details</returns>
    public FilterResult CheckAdBlock(string url)
    {
        if (string.IsNullOrEmpty(url))
            return new FilterResult { IsBlocked = false };

        var lowerUrl = url.ToLowerInvariant();

        // Check against ad block rules
        foreach (var rule in _adBlockRules)
        {
            if (lowerUrl.Contains(rule))
            {
                return new FilterResult
                {
                    IsBlocked = true,
                    Reason = "Ad/tracker blocked",
                    BlockedContent = rule,
                    FilterType = FilterType.AdBlock
                };
            }
        }

        // Check against regex patterns
        foreach (var regex in _adBlockRegexes)
        {
            var match = regex.Match(lowerUrl);
            if (match.Success)
            {
                return new FilterResult
                {
                    IsBlocked = true,
                    Reason = "Ad/tracker pattern blocked",
                    BlockedContent = match.Value,
                    FilterType = FilterType.AdBlock
                };
            }
        }

        return new FilterResult { IsBlocked = false };
    }

    /// <summary>
    /// Enhanced Address Bar Keyword Guard - Complete 3-Step Flow
    /// Step 1: Fast regex check, Step 2: AI classification, Step 3: Threshold decision
    /// </summary>
    /// <param name="url">URL to check</param>
    /// <returns>FilterResult with comprehensive analysis</returns>
    public async Task<FilterResult> CheckAddressBarKeywordGuardAsync(string url)
    {
        if (string.IsNullOrEmpty(url))
            return new FilterResult { IsBlocked = false };

        // Step 1: Fast regex check for obvious adult keywords and domains
        var regexResult = CheckAddressBarRegexFast(url);
        if (regexResult.IsBlocked)
        {
            return regexResult; // Block immediately if regex detects obvious adult content
        }

        // Step 2: AI classifier check (if regex passes)
        try
        {
            var classificationResult = await _urlClassificationService.ClassifyUrlAsync(url);

            // Step 3: Threshold decision
            if (classificationResult.Confidence >= HIGH_CONFIDENCE_THRESHOLD)
            {
                return new FilterResult
                {
                    IsBlocked = true,
                    Reason = $"Address Bar Guard: High confidence adult content ({classificationResult.Confidence:P0}) - {classificationResult.Reason}",
                    BlockedContent = url,
                    FilterType = FilterType.NSFW,
                    Confidence = classificationResult.Confidence
                };
            }
            else if (classificationResult.Confidence >= MEDIUM_CONFIDENCE_THRESHOLD)
            {
                // Medium confidence - could add warning or additional checks here
                return new FilterResult
                {
                    IsBlocked = false,
                    Reason = $"Address Bar Guard: Medium confidence ({classificationResult.Confidence:P0}) - allowing with caution",
                    FilterType = FilterType.NSFW,
                    Confidence = classificationResult.Confidence
                };
            }
            else
            {
                // Low confidence - allow
                return new FilterResult
                {
                    IsBlocked = false,
                    Reason = $"Address Bar Guard: Low risk ({classificationResult.Confidence:P0})",
                    Confidence = classificationResult.Confidence
                };
            }
        }
        catch (Exception ex)
        {
            // If AI classification fails, fall back to basic checks
            return new FilterResult
            {
                IsBlocked = false,
                Reason = $"Address Bar Guard: AI classification failed, allowing - {ex.Message}",
                Confidence = 0.0f
            };
        }
    }

    /// <summary>
    /// Enhanced Address Bar Keyword Guard - Step 1: Fast Regex Check
    /// Performs rapid URL filtering for obvious adult domains and keywords
    /// </summary>
    /// <param name="url">URL to check</param>
    /// <returns>FilterResult with fast regex analysis</returns>
    public FilterResult CheckAddressBarRegexFast(string url)
    {
        if (string.IsNullOrEmpty(url))
            return new FilterResult { IsBlocked = false };

        var lowerUrl = url.ToLowerInvariant();

        // Step 1: Fast regex check for obvious adult keywords and domains
        var adultKeywords = new[]
        {
            "adult", "porn", "xxx", "sex", "nude", "cam", "naked", "erotic",
            "fetish", "escort", "hookup", "dating", "nsfw", "mature", "explicit"
        };

        // Check for adult keywords in URL
        foreach (var keyword in adultKeywords)
        {
            if (lowerUrl.Contains(keyword))
            {
                return new FilterResult
                {
                    IsBlocked = true,
                    Reason = $"Address Bar Guard: Adult keyword detected - {keyword}",
                    BlockedContent = keyword,
                    FilterType = FilterType.Profanity,
                    Confidence = 0.95f // High confidence for obvious keywords
                };
            }
        }

        // Check against enhanced adult domain patterns
        var adultDomainPatterns = new[]
        {
            @"\b(porn|xxx|sex|nude|adult|cam|escort|hookup)\w*\.(com|net|org|tv|xxx)",
            @"\b\w*(porn|xxx|sex|nude|adult|cam|escort|hookup)\w*\.(com|net|org|tv|xxx)",
            @"(18|21)(\+|plus)",
            @"(red|you|x|porn)tube",
            @"xvideos\.(com|net|org)",
            @"pornhub\.(com|net|org)",
            @"redtube\.(com|net|org)",
            @"(adult|sex|porn|xxx).*?(site|hub|tube|cam|chat)"
        };

        foreach (var pattern in adultDomainPatterns)
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var match = regex.Match(lowerUrl);
            if (match.Success)
            {
                return new FilterResult
                {
                    IsBlocked = true,
                    Reason = $"Address Bar Guard: Adult domain pattern detected",
                    BlockedContent = match.Value,
                    FilterType = FilterType.Domain,
                    Confidence = 0.90f
                };
            }
        }

        return new FilterResult { IsBlocked = false };
    }

    /// <summary>
    /// Legacy method for backward compatibility
    /// </summary>
    /// <param name="url">URL to check</param>
    /// <returns>True if should be blocked</returns>
    public bool ShouldBlockUrl(string url)
    {
        return CheckAdBlock(url).IsBlocked;
    }

    /// <summary>
    /// Checks if domain is in whitelist
    /// </summary>
    /// <param name="url">URL to check</param>
    /// <param name="whitelistedDomains">JSON array of whitelisted domains</param>
    /// <returns>True if whitelisted</returns>
    public bool IsWhitelisted(string url, string whitelistedDomains)
    {
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(whitelistedDomains))
            return false;

        try
        {
            var domains = JsonSerializer.Deserialize<string[]>(whitelistedDomains);
            if (domains == null) return false;

            var uri = new Uri(url);
            var host = uri.Host.ToLowerInvariant();

            return domains.Any(domain =>
                host.Equals(domain.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith($".{domain.ToLowerInvariant()}", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if domain is in blacklist
    /// </summary>
    /// <param name="url">URL to check</param>
    /// <param name="blacklistedDomains">JSON array of blacklisted domains</param>
    /// <returns>True if blacklisted</returns>
    public bool IsBlacklisted(string url, string blacklistedDomains)
    {
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(blacklistedDomains))
            return false;

        try
        {
            var domains = JsonSerializer.Deserialize<string[]>(blacklistedDomains);
            if (domains == null) return false;

            var uri = new Uri(url);
            var host = uri.Host.ToLowerInvariant();

            return domains.Any(domain =>
                host.Equals(domain.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith($".{domain.ToLowerInvariant()}", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Enforces SafeSearch on search engine URLs
    /// </summary>
    /// <param name="url">Original URL</param>
    /// <returns>Modified URL with SafeSearch parameters</returns>
    public string EnforceSafeSearch(string url)
    {
        if (string.IsNullOrEmpty(url))
            return url;

        try
        {
            var uri = new Uri(url);
            var host = uri.Host.ToLowerInvariant();

            // Google Search
            if (host.Contains("google."))
            {
                return AddOrUpdateQueryParameter(url, "safe", "active");
            }

            // Bing Search
            if (host.Contains("bing."))
            {
                return AddOrUpdateQueryParameter(url, "adlt", "strict");
            }

            // DuckDuckGo
            if (host.Contains("duckduckgo."))
            {
                return AddOrUpdateQueryParameter(url, "safe_search", "strict");
            }
        }
        catch
        {
            // Return original URL if parsing fails
        }

        return url;
    }

    /// <summary>
    /// Helper method to add or update query parameters
    /// </summary>
    private string AddOrUpdateQueryParameter(string url, string paramName, string paramValue)
    {
        try
        {
            var uri = new Uri(url);
            var query = uri.Query;

            // Parse existing query parameters
            var queryParams = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(query))
            {
                query = query.TrimStart('?');
                var pairs = query.Split('&');
                foreach (var pair in pairs)
                {
                    var keyValue = pair.Split('=');
                    if (keyValue.Length == 2)
                    {
                        queryParams[Uri.UnescapeDataString(keyValue[0])] = Uri.UnescapeDataString(keyValue[1]);
                    }
                }
            }

            // Add or update the parameter
            queryParams[paramName] = paramValue;

            // Rebuild query string
            var newQuery = string.Join("&", queryParams.Select(kvp =>
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            var builder = new UriBuilder(uri) { Query = newQuery };
            return builder.ToString();
        }
        catch
        {
            return url;
        }
    }

    private HashSet<string> LoadProfanityWords()
    {
        // Comprehensive profanity word list for Islamic family-safe browsing
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Basic profanity
            "damn", "hell", "crap", "stupid", "idiot", "moron", "dumb", "suck", "sucks",
            "hate", "kill", "die", "death", "murder", "violence", "fight", "war",

            // Stronger profanity
            "fuck", "fucking", "fucked", "fucker", "shit", "shitting", "shitty", "bullshit",
            "bitch", "bitching", "ass", "asshole", "bastard", "piss", "pissed", "pissing",

            // Sexual content (comprehensive for Islamic family safety)
            "sex", "sexual", "sexy", "porn", "porno", "pornography", "nude", "naked", "nudity",
            "boobs", "breast", "tits", "nipple", "dick", "cock", "penis", "pussy", "vagina",
            "orgasm", "masturbate", "masturbation", "erotic", "fetish", "kinky", "horny",
            "slut", "whore", "prostitute", "escort", "stripper", "hooker", "brothel",
            "adult", "xxx", "18+", "nsfw", "mature", "explicit", "hardcore", "softcore",

            // Gambling and vice
            "casino", "gambling", "poker", "blackjack", "slots", "betting", "bet", "wager",
            "alcohol", "beer", "wine", "vodka", "whiskey", "drunk", "drinking", "bar",
            "marijuana", "weed", "cannabis", "drug", "drugs", "cocaine", "heroin", "meth",

            // Religious inappropriate (respectful to all faiths)
            "god damn", "goddamn", "jesus christ", "holy shit", "holy crap",
            "blasphemy", "sacrilege", "infidel", "heathen",

            // Dating/relationship inappropriate
            "hookup", "one night stand", "affair", "cheating", "mistress", "lover",
            "dating", "tinder", "bumble", "match", "flirt", "flirting",

            // Violence and inappropriate behavior
            "terrorist", "terrorism", "bomb", "explosion", "suicide", "self-harm",
            "abuse", "domestic violence", "rape", "assault", "harassment", "stalking"
        };
    }

    private HashSet<string> LoadAdBlockRules()
    {
        // Enhanced ad blocking rules based on EasyList and other sources
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Google Ads
            "doubleclick.net", "googleadservices.com", "googlesyndication.com",
            "googletagmanager.com", "google-analytics.com", "googletagservices.com",

            // Facebook/Meta
            "facebook.com/tr", "connect.facebook.net", "facebook.com/plugins",

            // Amazon
            "amazon-adsystem.com", "assoc-amazon.com",

            // Microsoft
            "bat.bing.com", "clarity.ms",

            // Other major ad networks
            "adsystem.com", "adsense.com", "adnxs.com", "rubiconproject.com",
            "pubmatic.com", "openx.net", "criteo.com", "outbrain.com", "taboola.com",

            // Analytics and tracking
            "hotjar.com", "fullstory.com", "loggly.com", "bugsnag.com",
            "sentry.io", "mixpanel.com", "segment.com", "amplitude.com",

            // Social media widgets
            "twitter.com/widgets", "platform.twitter.com", "instagram.com/embed",
            "youtube.com/embed", "linkedin.com/embed", "pinterest.com/js",

            // Common ad patterns
            "ads.", "ad.", "analytics.", "tracking.", "tracker.", "metrics.",
            "telemetry.", "beacon.", "pixel.", "tag.", "stats.", "counter."
        };
    }

    private List<Regex> CreateProfanityRegexes()
    {
        return new List<Regex>
        {
            // Pattern for detecting inappropriate content
            new Regex(@"\b(adult|porn|xxx|sex|nude|naked)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"\b(gambling|casino|bet|poker)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            // Add more patterns as needed
        };
    }

    private List<Regex> CreateAdBlockRegexes()
    {
        return new List<Regex>
        {
            // Ad server patterns
            new Regex(@"^https?://[^/]*\.ads?\.", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"^https?://ads?\.", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"^https?://[^/]*\.ad\.", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Analytics patterns
            new Regex(@"^https?://[^/]*analytics\.", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"^https?://[^/]*tracking\.", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"^https?://[^/]*tracker\.", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Metrics and telemetry
            new Regex(@"^https?://[^/]*metrics\.", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"^https?://[^/]*telemetry\.", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"^https?://[^/]*beacon\.", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Common ad URL patterns
            new Regex(@"/ads?[/_\-]", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"/banner[s]?[/_\-]", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"/popup[s]?[/_\-]", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"/track[/_\-]", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"/pixel[s]?[/_\-]", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Social media tracking
            new Regex(@"facebook\.com/tr\?", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"google-analytics\.com/collect", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"googletagmanager\.com/gtm\.js", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        };
    }

    /// <summary>
    /// Diagnostic method for testing Address Bar Keyword Guard functionality
    /// </summary>
    /// <param name="testUrl">URL to test</param>
    /// <returns>Detailed diagnostic information</returns>
    public async Task<AddressBarGuardDiagnostic> DiagnoseAddressBarGuardAsync(string testUrl)
    {
        var diagnostic = new AddressBarGuardDiagnostic
        {
            TestUrl = testUrl,
            Timestamp = DateTime.Now
        };

        try
        {
            // Step 1: Test regex check
            var regexResult = CheckAddressBarRegexFast(testUrl);
            diagnostic.RegexCheckResult = regexResult;
            diagnostic.RegexCheckTime = DateTime.Now;

            if (!regexResult.IsBlocked)
            {
                // Step 2: Test AI classification
                var classificationStart = DateTime.Now;
                var classificationResult = await _urlClassificationService.ClassifyUrlAsync(testUrl);
                diagnostic.AiClassificationTime = DateTime.Now - classificationStart;
                diagnostic.AiClassificationResult = classificationResult;

                // Step 3: Test complete flow
                var completeFlowStart = DateTime.Now;
                var completeResult = await CheckAddressBarKeywordGuardAsync(testUrl);
                diagnostic.CompleteFlowTime = DateTime.Now - completeFlowStart;
                diagnostic.CompleteFlowResult = completeResult;
            }
            else
            {
                diagnostic.AiClassificationResult = new UrlClassificationResult
                {
                    IsAdult = true,
                    Confidence = regexResult.Confidence,
                    Reason = "Blocked by regex, AI classification skipped"
                };
                diagnostic.CompleteFlowResult = regexResult;
            }

            diagnostic.IsSuccessful = true;
        }
        catch (Exception ex)
        {
            diagnostic.IsSuccessful = false;
            diagnostic.ErrorMessage = ex.Message;
            diagnostic.ErrorDetails = ex.ToString();
        }

        return diagnostic;
    }

    /// <summary>
    /// Get Address Bar Guard performance statistics
    /// </summary>
    /// <returns>Performance statistics</returns>
    public AddressBarGuardStats GetAddressBarGuardStats()
    {
        return new AddressBarGuardStats
        {
            IsNsfwModelLoaded = _nsfwDetectionService.IsModelLoaded,
            UrlClassificationServiceLoaded = _urlClassificationService != null,
            HighConfidenceThreshold = HIGH_CONFIDENCE_THRESHOLD,
            MediumConfidenceThreshold = MEDIUM_CONFIDENCE_THRESHOLD,
            ProfanityWordsCount = _profanityWords.Count,
            AdBlockRulesCount = _adBlockRules.Count,
            ProfanityRegexCount = _profanityRegexes.Count,
            AdBlockRegexCount = _adBlockRegexes.Count
        };
    }

    public void Dispose()
    {
        _nsfwDetectionService?.Dispose();
        _httpClient?.Dispose();
        // Note: UrlClassificationService doesn't implement IDisposable as it doesn't hold resources
    }
}

public class FilterResult
{
    public bool IsBlocked { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string BlockedContent { get; set; } = string.Empty;
    public FilterType FilterType { get; set; }
    public float Confidence { get; set; }
}

public enum FilterType
{
    None,
    Profanity,
    NSFW,
    AdBlock,
    Blacklist,
    Domain,
    TimeLimit,
    Azan
}

/// <summary>
/// Diagnostic information for Address Bar Keyword Guard testing
/// </summary>
public class AddressBarGuardDiagnostic
{
    public string TestUrl { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsSuccessful { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorDetails { get; set; } = string.Empty;

    // Step 1: Regex Check
    public FilterResult RegexCheckResult { get; set; } = new FilterResult();
    public DateTime RegexCheckTime { get; set; }

    // Step 2: AI Classification
    public UrlClassificationResult AiClassificationResult { get; set; } = new UrlClassificationResult();
    public TimeSpan AiClassificationTime { get; set; }

    // Step 3: Complete Flow
    public FilterResult CompleteFlowResult { get; set; } = new FilterResult();
    public TimeSpan CompleteFlowTime { get; set; }

    public override string ToString()
    {
        var result = $"Address Bar Guard Diagnostic for: {TestUrl}\n";
        result += $"Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss.fff}\n";
        result += $"Success: {IsSuccessful}\n";

        if (!IsSuccessful)
        {
            result += $"Error: {ErrorMessage}\n";
            return result;
        }

        result += $"\n--- Step 1: Regex Check ---\n";
        result += $"Blocked: {RegexCheckResult.IsBlocked}\n";
        result += $"Reason: {RegexCheckResult.Reason}\n";
        result += $"Confidence: {RegexCheckResult.Confidence:P1}\n";

        result += $"\n--- Step 2: AI Classification ---\n";
        result += $"Adult Content: {AiClassificationResult.IsAdult}\n";
        result += $"Confidence: {AiClassificationResult.Confidence:P1}\n";
        result += $"Reason: {AiClassificationResult.Reason}\n";
        result += $"Processing Time: {AiClassificationTime.TotalMilliseconds:F1}ms\n";

        result += $"\n--- Step 3: Final Decision ---\n";
        result += $"Final Blocked: {CompleteFlowResult.IsBlocked}\n";
        result += $"Final Reason: {CompleteFlowResult.Reason}\n";
        result += $"Final Confidence: {CompleteFlowResult.Confidence:P1}\n";
        result += $"Total Processing Time: {CompleteFlowTime.TotalMilliseconds:F1}ms\n";

        return result;
    }
}

/// <summary>
/// Performance statistics for Address Bar Keyword Guard
/// </summary>
public class AddressBarGuardStats
{
    public bool IsNsfwModelLoaded { get; set; }
    public bool UrlClassificationServiceLoaded { get; set; }
    public float HighConfidenceThreshold { get; set; }
    public float MediumConfidenceThreshold { get; set; }
    public int ProfanityWordsCount { get; set; }
    public int AdBlockRulesCount { get; set; }
    public int ProfanityRegexCount { get; set; }
    public int AdBlockRegexCount { get; set; }

    public override string ToString()
    {
        var result = "Address Bar Keyword Guard Statistics\n";
        result += "=====================================\n";
        result += $"NSFW Model Loaded: {IsNsfwModelLoaded}\n";
        result += $"URL Classification Service: {UrlClassificationServiceLoaded}\n";
        result += $"High Confidence Threshold: {HighConfidenceThreshold:P0}\n";
        result += $"Medium Confidence Threshold: {MediumConfidenceThreshold:P0}\n";
        result += $"Profanity Words: {ProfanityWordsCount}\n";
        result += $"Ad Block Rules: {AdBlockRulesCount}\n";
        result += $"Profanity Regex Patterns: {ProfanityRegexCount}\n";
        result += $"Ad Block Regex Patterns: {AdBlockRegexCount}\n";
        return result;
    }
}