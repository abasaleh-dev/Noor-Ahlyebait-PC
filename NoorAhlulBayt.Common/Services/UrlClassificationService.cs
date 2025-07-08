using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NoorAhlulBayt.Common.Services
{
    /// <summary>
    /// AI-based URL Classification Service for Address Bar Keyword Guard
    /// Analyzes URLs using multiple heuristic signals to determine adult content probability
    /// </summary>
    public class UrlClassificationService
    {
        private readonly Dictionary<string, float> _adultKeywordWeights;
        private readonly Dictionary<string, float> _safeKeywordWeights;
        private readonly HashSet<string> _knownAdultDomains;
        private readonly HashSet<string> _knownSafeDomains;
        private readonly List<Regex> _adultPatterns;
        private readonly List<Regex> _safePatterns;

        public UrlClassificationService()
        {
            _adultKeywordWeights = LoadAdultKeywordWeights();
            _safeKeywordWeights = LoadSafeKeywordWeights();
            _knownAdultDomains = LoadKnownAdultDomains();
            _knownSafeDomains = LoadKnownSafeDomains();
            _adultPatterns = CreateAdultPatterns();
            _safePatterns = CreateSafePatterns();
        }

        /// <summary>
        /// Classifies a URL and returns adult content probability
        /// </summary>
        /// <param name="url">URL to classify</param>
        /// <returns>Classification result with confidence score</returns>
        public async Task<UrlClassificationResult> ClassifyUrlAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
                return new UrlClassificationResult { IsAdult = false, Confidence = 0.0f, Reason = "Empty URL" };

            try
            {
                var uri = new Uri(url);
                var domain = uri.Host.ToLowerInvariant();
                var fullUrl = url.ToLowerInvariant();
                var path = uri.AbsolutePath.ToLowerInvariant();
                var query = uri.Query.ToLowerInvariant();

                float adultScore = 0.0f;
                float safeScore = 0.0f;
                var reasons = new List<string>();

                // 1. Check known domains (highest weight)
                if (_knownAdultDomains.Contains(domain))
                {
                    adultScore += 0.9f;
                    reasons.Add($"Known adult domain: {domain}");
                }
                else if (_knownSafeDomains.Contains(domain))
                {
                    safeScore += 0.8f;
                    reasons.Add($"Known safe domain: {domain}");
                }

                // 2. Domain analysis
                adultScore += AnalyzeDomainForAdultContent(domain, reasons);
                safeScore += AnalyzeDomainForSafeContent(domain, reasons);

                // 3. Path analysis
                adultScore += AnalyzePathForAdultContent(path, reasons);
                safeScore += AnalyzePathForSafeContent(path, reasons);

                // 4. Query parameter analysis
                adultScore += AnalyzeQueryForAdultContent(query, reasons);

                // 5. Pattern matching
                adultScore += AnalyzePatterns(_adultPatterns, fullUrl, "adult pattern", reasons);
                safeScore += AnalyzePatterns(_safePatterns, fullUrl, "safe pattern", reasons);

                // 6. Keyword analysis
                adultScore += AnalyzeKeywords(_adultKeywordWeights, fullUrl, reasons);
                safeScore += AnalyzeKeywords(_safeKeywordWeights, fullUrl, reasons);

                // Calculate final confidence
                float totalScore = adultScore + safeScore;
                float confidence = totalScore > 0 ? adultScore / totalScore : 0.0f;

                // Normalize confidence to 0-1 range
                confidence = Math.Max(0.0f, Math.Min(1.0f, confidence));

                return new UrlClassificationResult
                {
                    IsAdult = confidence > 0.5f,
                    Confidence = confidence,
                    Reason = string.Join("; ", reasons.Take(3)), // Top 3 reasons
                    AdultScore = adultScore,
                    SafeScore = safeScore
                };
            }
            catch (Exception ex)
            {
                return new UrlClassificationResult 
                { 
                    IsAdult = false, 
                    Confidence = 0.0f, 
                    Reason = $"Classification error: {ex.Message}" 
                };
            }
        }

        private float AnalyzeDomainForAdultContent(string domain, List<string> reasons)
        {
            float score = 0.0f;

            // Check for adult-related subdomains
            var adultSubdomains = new[] { "adult", "xxx", "porn", "sex", "nude", "cam", "live", "hot", "sexy" };
            foreach (var subdomain in adultSubdomains)
            {
                if (domain.StartsWith($"{subdomain}.") || domain.Contains($".{subdomain}."))
                {
                    score += 0.7f;
                    reasons.Add($"Adult subdomain: {subdomain}");
                    break;
                }
            }

            // Check for adult TLDs
            if (domain.EndsWith(".xxx") || domain.EndsWith(".adult"))
            {
                score += 0.9f;
                reasons.Add("Adult TLD");
            }

            // Check for suspicious domain patterns
            if (Regex.IsMatch(domain, @"\d{2,3}(plus|hot|sexy|adult)", RegexOptions.IgnoreCase))
            {
                score += 0.6f;
                reasons.Add("Age-restricted domain pattern");
            }

            return Math.Min(score, 1.0f);
        }

        private float AnalyzeDomainForSafeContent(string domain, List<string> reasons)
        {
            float score = 0.0f;

            // Educational domains
            var eduDomains = new[] { ".edu", ".ac.", ".university", ".school", ".college" };
            if (eduDomains.Any(edu => domain.Contains(edu)))
            {
                score += 0.8f;
                reasons.Add("Educational domain");
            }

            // Government domains
            if (domain.EndsWith(".gov") || domain.Contains(".gov."))
            {
                score += 0.9f;
                reasons.Add("Government domain");
            }

            // Religious domains
            var religiousDomains = new[] { "islam", "quran", "mosque", "church", "temple", "religious" };
            if (religiousDomains.Any(rel => domain.Contains(rel)))
            {
                score += 0.7f;
                reasons.Add("Religious domain");
            }

            return Math.Min(score, 1.0f);
        }

        private float AnalyzePathForAdultContent(string path, List<string> reasons)
        {
            float score = 0.0f;

            var adultPaths = new[] { "/adult/", "/porn/", "/xxx/", "/sex/", "/nude/", "/cam/", "/escort/" };
            foreach (var adultPath in adultPaths)
            {
                if (path.Contains(adultPath))
                {
                    score += 0.8f;
                    reasons.Add($"Adult path: {adultPath}");
                    break;
                }
            }

            return Math.Min(score, 1.0f);
        }

        private float AnalyzePathForSafeContent(string path, List<string> reasons)
        {
            float score = 0.0f;

            var safePaths = new[] { "/education/", "/learn/", "/help/", "/about/", "/contact/", "/news/" };
            foreach (var safePath in safePaths)
            {
                if (path.Contains(safePath))
                {
                    score += 0.3f;
                    reasons.Add($"Safe path: {safePath}");
                    break;
                }
            }

            return Math.Min(score, 1.0f);
        }

        private float AnalyzeQueryForAdultContent(string query, List<string> reasons)
        {
            float score = 0.0f;

            if (query.Contains("age=18") || query.Contains("adult=true") || query.Contains("nsfw=1"))
            {
                score += 0.7f;
                reasons.Add("Adult query parameters");
            }

            return Math.Min(score, 1.0f);
        }

        private float AnalyzePatterns(List<Regex> patterns, string url, string patternType, List<string> reasons)
        {
            float score = 0.0f;

            foreach (var pattern in patterns)
            {
                if (pattern.IsMatch(url))
                {
                    score += 0.5f;
                    reasons.Add($"Matched {patternType}");
                    break; // Only count first match to avoid over-scoring
                }
            }

            return Math.Min(score, 1.0f);
        }

        private float AnalyzeKeywords(Dictionary<string, float> keywords, string url, List<string> reasons)
        {
            float score = 0.0f;
            var foundKeywords = new List<string>();

            foreach (var keyword in keywords)
            {
                if (url.Contains(keyword.Key))
                {
                    score += keyword.Value;
                    foundKeywords.Add(keyword.Key);
                }
            }

            if (foundKeywords.Any())
            {
                reasons.Add($"Keywords: {string.Join(", ", foundKeywords.Take(3))}");
            }

            return Math.Min(score, 1.0f);
        }

        private Dictionary<string, float> LoadAdultKeywordWeights()
        {
            return new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
            {
                // High-weight adult keywords
                { "porn", 0.9f }, { "xxx", 0.9f }, { "sex", 0.8f }, { "nude", 0.8f },
                { "adult", 0.7f }, { "nsfw", 0.8f }, { "erotic", 0.7f }, { "fetish", 0.8f },
                { "cam", 0.6f }, { "webcam", 0.6f }, { "escort", 0.8f }, { "hookup", 0.7f },
                
                // Medium-weight keywords
                { "dating", 0.4f }, { "singles", 0.3f }, { "meet", 0.2f }, { "chat", 0.2f },
                { "live", 0.3f }, { "hot", 0.3f }, { "sexy", 0.5f }, { "mature", 0.4f },
                
                // Gambling keywords
                { "casino", 0.6f }, { "poker", 0.5f }, { "bet", 0.5f }, { "gambling", 0.7f },
                { "slots", 0.6f }, { "blackjack", 0.5f }
            };
        }

        private Dictionary<string, float> LoadSafeKeywordWeights()
        {
            return new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
            {
                // Educational keywords
                { "education", 0.6f }, { "learn", 0.5f }, { "study", 0.5f }, { "school", 0.6f },
                { "university", 0.7f }, { "college", 0.6f }, { "academic", 0.6f },

                // Religious keywords
                { "islam", 0.8f }, { "quran", 0.8f }, { "mosque", 0.7f }, { "prayer", 0.7f },
                { "religious", 0.6f }, { "faith", 0.5f }, { "spiritual", 0.5f },

                // General safe keywords
                { "news", 0.4f }, { "information", 0.3f }, { "help", 0.4f }, { "support", 0.3f },
                { "family", 0.5f }, { "children", 0.4f }, { "kids", 0.4f }
            };
        }

        private HashSet<string> LoadKnownAdultDomains()
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // Major adult sites
                "pornhub.com", "xvideos.com", "xnxx.com", "redtube.com", "youporn.com",
                "tube8.com", "spankbang.com", "xhamster.com", "brazzers.com", "bangbros.com",
                "realitykings.com", "naughtyamerica.com", "playboy.com", "penthouse.com",

                // Dating/hookup sites
                "adultfriendfinder.com", "ashley-madison.com", "tinder.com", "bumble.com",

                // Cam sites
                "chaturbate.com", "myfreecams.com", "cam4.com", "bongacams.com"
            };
        }

        private HashSet<string> LoadKnownSafeDomains()
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // Search engines
                "google.com", "bing.com", "yahoo.com", "duckduckgo.com",

                // Educational
                "wikipedia.org", "khan-academy.org", "coursera.org", "edx.org",

                // Islamic sites
                "quran.com", "islamqa.info", "islamweb.net", "islamhouse.com",
                "sunnah.com", "qiblafinder.withgoogle.com",

                // News
                "bbc.com", "cnn.com", "reuters.com", "ap.org",

                // Government
                "gov.uk", "usa.gov", "canada.ca"
            };
        }

        private List<Regex> CreateAdultPatterns()
        {
            return new List<Regex>
            {
                new Regex(@"\b(18|21)\+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new Regex(@"(red|you|x|porn)tube", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new Regex(@"(adult|sex|porn|xxx).*?(site|hub|tube|cam|chat)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new Regex(@"\b(free|live|hot|sexy).*?(cam|chat|girls?|women)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new Regex(@"(meet|find|local).*?(singles?|girls?|women|hookup)", RegexOptions.IgnoreCase | RegexOptions.Compiled)
            };
        }

        private List<Regex> CreateSafePatterns()
        {
            return new List<Regex>
            {
                new Regex(@"\b(learn|study|education|academic)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new Regex(@"\b(news|information|help|support)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new Regex(@"\b(family|children|kids|safe)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new Regex(@"\b(islam|quran|mosque|prayer|religious)", RegexOptions.IgnoreCase | RegexOptions.Compiled)
            };
        }
    }

    /// <summary>
    /// Result of URL classification analysis
    /// </summary>
    public class UrlClassificationResult
    {
        public bool IsAdult { get; set; }
        public float Confidence { get; set; }
        public string Reason { get; set; } = string.Empty;
        public float AdultScore { get; set; }
        public float SafeScore { get; set; }
    }
}
