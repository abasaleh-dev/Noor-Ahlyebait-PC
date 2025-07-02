using FluentAssertions;
using NoorAhlulBayt.Common.Services;
using Xunit;

namespace NoorAhlulBayt.Tests;

public class ContentFilterServiceTests : IDisposable
{
    private readonly ContentFilterService _contentFilterService;

    public ContentFilterServiceTests()
    {
        // Initialize without NSFW model for basic testing
        _contentFilterService = new ContentFilterService();
    }

    [Theory]
    [InlineData("This is a clean sentence.", false)]
    [InlineData("This contains damn word.", true)]
    [InlineData("What the hell is this?", true)]
    [InlineData("This is crap content.", true)]
    [InlineData("You are so stupid.", true)]
    [InlineData("Don't be an idiot.", true)]
    [InlineData("That's moronic behavior.", true)]
    [InlineData("This is dumb.", true)]
    [InlineData("", false)]
    public void CheckProfanity_BasicWords_ShouldDetectCorrectly(string content, bool expectedBlocked)
    {
        // Act
        var result = _contentFilterService.CheckProfanity(content);

        // Assert
        result.IsBlocked.Should().Be(expectedBlocked);
        if (expectedBlocked)
        {
            result.Reason.Should().Be("Profanity detected");
            result.FilterType.Should().Be(FilterType.Profanity);
            result.BlockedContent.Should().NotBeEmpty();
        }
    }

    [Theory]
    [InlineData("Visit adult websites for content.", true)]
    [InlineData("This contains porn references.", true)]
    [InlineData("XXX rated material here.", true)]
    [InlineData("Sex education is important.", true)]
    [InlineData("Nude art galleries.", true)]
    [InlineData("Naked truth about politics.", true)]
    [InlineData("Gambling is prohibited.", true)]
    [InlineData("Casino games are fun.", true)]
    [InlineData("Place your bet now.", true)]
    [InlineData("Poker night with friends.", true)]
    [InlineData("This is completely clean content.", false)]
    public void CheckProfanity_RegexPatterns_ShouldDetectCorrectly(string content, bool expectedBlocked)
    {
        // Act
        var result = _contentFilterService.CheckProfanity(content);

        // Assert
        result.IsBlocked.Should().Be(expectedBlocked);
        if (expectedBlocked)
        {
            result.Reason.Should().Be("Inappropriate content detected");
            result.FilterType.Should().Be(FilterType.Profanity);
        }
    }

    [Theory]
    [InlineData("https://pornhub.com/video/123", true)]
    [InlineData("https://xvideos.com/watch/456", true)]
    [InlineData("https://casino.com/games", true)]
    [InlineData("https://bet365.com/sports", true)]
    [InlineData("https://brazzers.com/scene/123", true)]
    [InlineData("https://playboy.com/gallery", true)]
    [InlineData("https://adultfriendfinder.com/profile", true)]
    [InlineData("https://ashley-madison.com/signup", true)]
    [InlineData("https://google.com/search", false)]
    [InlineData("https://wikipedia.org/article", false)]
    [InlineData("https://stackoverflow.com/questions", false)]
    [InlineData("", false)]
    [InlineData("invalid-url", false)]
    public void CheckDomain_BlacklistedSites_ShouldBlockCorrectly(string url, bool expectedBlocked)
    {
        // Act
        var result = _contentFilterService.CheckDomain(url);

        // Assert
        result.IsBlocked.Should().Be(expectedBlocked);
        if (expectedBlocked)
        {
            result.Reason.Should().Contain("family safety");
            result.FilterType.Should().Be(FilterType.Domain);
            result.BlockedContent.Should().NotBeEmpty();
        }
    }

    [Theory]
    [InlineData("https://xxxvideos.net/watch/123", true)]
    [InlineData("http://sexchat.com/room/456", true)]
    [InlineData("https://nudephotos.org/gallery", true)]
    [InlineData("http://adultcontent.tv/stream", true)]
    [InlineData("https://eroticstories.com/read/789", true)]
    [InlineData("http://casinogames.net/slots", true)]
    [InlineData("https://pokertournament.com/play", true)]
    [InlineData("http://alcoholdelivery.com/order", true)]
    [InlineData("https://cleansite.com/page", false)]
    [InlineData("https://educational.org/content", false)]
    public void CheckDomain_SuspiciousPatterns_ShouldBlockCorrectly(string url, bool expectedBlocked)
    {
        // Act
        var result = _contentFilterService.CheckDomain(url);

        // Assert
        result.IsBlocked.Should().Be(expectedBlocked);
        if (expectedBlocked)
        {
            result.Reason.Should().Contain("inappropriate content pattern");
            result.FilterType.Should().Be(FilterType.Domain);
            result.BlockedContent.Should().NotBeEmpty();
        }
    }

    [Theory]
    [InlineData("https://googleads.g.doubleclick.net/pagead", true)]
    [InlineData("https://facebook.com/tr?id=123", true)]
    [InlineData("https://google-analytics.com/collect", true)]
    [InlineData("https://googletagmanager.com/gtm.js", true)]
    [InlineData("https://example.com/content.js", false)]
    [InlineData("https://cdn.jsdelivr.net/library.js", false)]
    public void CheckAdBlock_TrackingUrls_ShouldBlockCorrectly(string url, bool expectedBlocked)
    {
        // Act
        var result = _contentFilterService.CheckAdBlock(url);

        // Assert
        result.IsBlocked.Should().Be(expectedBlocked);
        if (expectedBlocked)
        {
            result.FilterType.Should().Be(FilterType.AdBlock);
        }
    }

    [Theory]
    [InlineData("https://google.com/search?q=test", "https://google.com/search?q=test&safe=strict")]
    [InlineData("https://bing.com/search?q=example", "https://bing.com/search?q=example&adlt=strict")]
    [InlineData("https://duckduckgo.com/?q=query", "https://duckduckgo.com/?q=query&safe-search=strict")]
    [InlineData("https://yahoo.com/search?p=term", "https://yahoo.com/search?p=term&vm=r")]
    [InlineData("https://example.com/page", "https://example.com/page")]
    public void EnforceSafeSearch_SearchEngines_ShouldAddSafeSearchParameters(string originalUrl, string expectedUrl)
    {
        // Act
        var result = _contentFilterService.EnforceSafeSearch(originalUrl);

        // Assert
        result.Should().Be(expectedUrl);
    }

    public void Dispose()
    {
        _contentFilterService?.Dispose();
    }
}
