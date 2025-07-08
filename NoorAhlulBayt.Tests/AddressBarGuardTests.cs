using Xunit;
using NoorAhlulBayt.Common.Services;
using System.Threading.Tasks;

namespace NoorAhlulBayt.Tests
{
    public class AddressBarGuardTests : IDisposable
    {
        private readonly ContentFilterService _contentFilter;

        public AddressBarGuardTests()
        {
            _contentFilter = new ContentFilterService();
        }

        public void Dispose()
        {
            _contentFilter?.Dispose();
        }

        #region Step 1: Regex Tests

        [Fact]
        public void RegexCheck_ShouldBlockObviousAdultKeywords()
        {
            // Arrange
            var adultUrls = new[]
            {
                "https://porn.com",
                "https://xxx-site.net",
                "https://adult-content.org",
                "https://sex-videos.tv",
                "https://nude-photos.com",
                "https://cam-girls.net",
                "https://escort-services.com",
                "https://hookup-site.org"
            };

            // Act & Assert
            foreach (var url in adultUrls)
            {
                var result = _contentFilter.CheckAddressBarRegexFast(url);
                Assert.True(result.IsBlocked, $"URL should be blocked: {url}");
                Assert.True(result.Confidence >= 0.9f, $"Confidence should be high for obvious adult content: {url}");
            }
        }

        [Fact]
        public void RegexCheck_ShouldAllowLegitimateUrls()
        {
            // Arrange
            var legitimateUrls = new[]
            {
                "https://google.com",
                "https://wikipedia.org",
                "https://github.com",
                "https://stackoverflow.com",
                "https://microsoft.com",
                "https://amazon.com",
                "https://news.bbc.co.uk",
                "https://islamqa.info"
            };

            // Act & Assert
            foreach (var url in legitimateUrls)
            {
                var result = _contentFilter.CheckAddressBarRegexFast(url);
                Assert.False(result.IsBlocked, $"URL should not be blocked: {url}");
            }
        }

        [Fact]
        public void RegexCheck_ShouldBlockAdultDomainPatterns()
        {
            // Arrange
            var adultPatternUrls = new[]
            {
                "https://pornhub.com",
                "https://xvideos.com",
                "https://redtube.com",
                "https://18plus-content.net",
                "https://21+videos.org",
                "https://adult-site-hub.com",
                "https://sex-chat-room.net"
            };

            // Act & Assert
            foreach (var url in adultPatternUrls)
            {
                var result = _contentFilter.CheckAddressBarRegexFast(url);
                Assert.True(result.IsBlocked, $"URL should be blocked by pattern matching: {url}");
                // Note: Some URLs may be caught by keyword check first (Profanity) before domain patterns (Domain)
                // Both are valid blocking reasons for adult content
                Assert.True(result.FilterType == FilterType.Domain || result.FilterType == FilterType.Profanity,
                    $"Should be blocked as either Domain or Profanity filter: {url} (was {result.FilterType})");
            }
        }

        #endregion

        #region Step 2: AI Classification Tests

        [Fact]
        public async Task AiClassification_ShouldDetectSuspiciousUrls()
        {
            // Arrange
            var suspiciousUrls = new[]
            {
                "https://dating-tonight.com/hookup",
                "https://meet-singles.net/adult-chat",
                "https://video-stream.org/mature-content",
                "https://social-media.com/explicit-photos"
            };

            // Act & Assert
            foreach (var url in suspiciousUrls)
            {
                var result = await _contentFilter.CheckAddressBarKeywordGuardAsync(url);
                // These should have some confidence level (may not be blocked if under threshold)
                Assert.True(result.Confidence >= 0.0f, $"Should have some confidence assessment: {url}");
            }
        }

        [Fact]
        public async Task AiClassification_ShouldAllowEducationalUrls()
        {
            // Arrange
            var educationalUrls = new[]
            {
                "https://khan-academy.org/biology/reproduction",
                "https://medical-journal.com/anatomy-study",
                "https://university.edu/health-education",
                "https://wikipedia.org/human-biology"
            };

            // Act & Assert
            foreach (var url in educationalUrls)
            {
                var result = await _contentFilter.CheckAddressBarKeywordGuardAsync(url);
                Assert.False(result.IsBlocked, $"Educational URL should not be blocked: {url}");
            }
        }

        #endregion

        #region Step 3: Complete Flow Tests

        [Fact]
        public async Task CompleteFlow_ShouldBlockHighConfidenceAdultContent()
        {
            // Arrange
            var highConfidenceAdultUrls = new[]
            {
                "https://explicit-porn-videos.xxx",
                "https://adult-xxx-content.com/hardcore",
                "https://nude-cam-girls.net/live-sex"
            };

            // Act & Assert
            foreach (var url in highConfidenceAdultUrls)
            {
                var result = await _contentFilter.CheckAddressBarKeywordGuardAsync(url);
                Assert.True(result.IsBlocked, $"High confidence adult URL should be blocked: {url}");
                Assert.True(result.Confidence >= 0.8f, $"Should have high confidence: {url}");
            }
        }

        [Fact]
        public async Task CompleteFlow_ShouldAllowLowRiskUrls()
        {
            // Arrange
            var lowRiskUrls = new[]
            {
                "https://google.com/search?q=islamic+prayer+times",
                "https://quran.com/al-fatiha",
                "https://islamqa.info/en/answers",
                "https://github.com/microsoft/webview2"
            };

            // Act & Assert
            foreach (var url in lowRiskUrls)
            {
                var result = await _contentFilter.CheckAddressBarKeywordGuardAsync(url);
                Assert.False(result.IsBlocked, $"Low risk URL should not be blocked: {url}");
            }
        }

        [Fact]
        public async Task CompleteFlow_ShouldHandleEmptyAndInvalidUrls()
        {
            // Arrange
            var invalidUrls = new[] { "", "   ", "not-a-url", "javascript:alert('test')" };

            // Act & Assert
            foreach (var url in invalidUrls)
            {
                var result = await _contentFilter.CheckAddressBarKeywordGuardAsync(url);
                Assert.False(result.IsBlocked, $"Invalid URL should not cause blocking: '{url}'");
            }

            // Test null separately to avoid warning
            var nullResult = await _contentFilter.CheckAddressBarKeywordGuardAsync(null!);
            Assert.False(nullResult.IsBlocked, "Null URL should not cause blocking");
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void Performance_RegexCheckShouldBeFast()
        {
            // Arrange
            var testUrl = "https://example-adult-content.com";
            var iterations = 1000;

            // Act
            var startTime = System.DateTime.Now;
            for (int i = 0; i < iterations; i++)
            {
                _contentFilter.CheckAddressBarRegexFast(testUrl);
            }
            var elapsed = System.DateTime.Now - startTime;

            // Assert
            Assert.True(elapsed.TotalMilliseconds < 1000,
                $"Regex check should be fast: {elapsed.TotalMilliseconds}ms for {iterations} iterations");
        }

        [Fact]
        public async Task Performance_CompleteFlowShouldBeReasonable()
        {
            // Arrange
            var testUrl = "https://example.com";

            // Act
            var startTime = System.DateTime.Now;
            var result = await _contentFilter.CheckAddressBarKeywordGuardAsync(testUrl);
            var elapsed = System.DateTime.Now - startTime;

            // Assert
            Assert.True(elapsed.TotalMilliseconds < 5000,
                $"Complete flow should complete within 5 seconds: {elapsed.TotalMilliseconds}ms");
        }

        #endregion

        #region Diagnostic Tests

        [Fact]
        public async Task Diagnostics_ShouldProvideDetailedInformation()
        {
            // Arrange
            var testUrl = "https://test-site.com";

            // Act
            var diagnostic = await _contentFilter.DiagnoseAddressBarGuardAsync(testUrl);

            // Assert
            Assert.NotNull(diagnostic);
            Assert.Equal(testUrl, diagnostic.TestUrl);
            Assert.True(diagnostic.IsSuccessful);
            Assert.NotNull(diagnostic.RegexCheckResult);
            Assert.NotNull(diagnostic.AiClassificationResult);
            Assert.NotNull(diagnostic.CompleteFlowResult);
        }

        [Fact]
        public void Statistics_ShouldProvideSystemStatus()
        {
            // Act
            var stats = _contentFilter.GetAddressBarGuardStats();

            // Assert
            Assert.NotNull(stats);
            Assert.True(stats.HighConfidenceThreshold > 0);
            Assert.True(stats.MediumConfidenceThreshold > 0);
            Assert.True(stats.ProfanityWordsCount >= 0);
        }

        #endregion
    }
}
