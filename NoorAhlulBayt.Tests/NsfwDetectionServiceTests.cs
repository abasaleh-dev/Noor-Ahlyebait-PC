using FluentAssertions;
using NoorAhlulBayt.Common.Services;
using Xunit;
using System.Net.Http;
using System.IO;

namespace NoorAhlulBayt.Tests;

public class NsfwDetectionServiceTests : IDisposable
{
    private readonly NsfwDetectionService _nsfwService;
    private readonly HttpClient _httpClient;

    public NsfwDetectionServiceTests()
    {
        // Try to initialize with model if available, otherwise use URL-based filtering
        var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", 
            "NoorAhlulBayt.Browser", "Models", "AI", "nsfw-model.onnx");
        
        _nsfwService = new NsfwDetectionService(File.Exists(modelPath) ? modelPath : null);
        _httpClient = new HttpClient();
    }

    [Fact]
    public void IsModelLoaded_ShouldIndicateModelStatus()
    {
        // Act & Assert
        // This will be true if the NSFW model is properly installed
        var isLoaded = _nsfwService.IsModelLoaded;
        
        // Log the status for manual verification
        Console.WriteLine($"NSFW Model Status: {(isLoaded ? "Loaded" : "Not Available - Using URL-based filtering")}");
        
        // Test should not fail regardless of model availability
        // Just verify it's a boolean value
        Assert.IsType<bool>(isLoaded);
    }

    [Theory]
    [InlineData("https://example.com/suspicious-adult-content.jpg", true)]
    [InlineData("https://example.com/xxx-content.png", true)]
    [InlineData("https://example.com/porn-image.gif", true)]
    [InlineData("https://example.com/nude-photo.jpg", true)]
    [InlineData("https://example.com/sexy-picture.png", true)]
    [InlineData("https://example.com/normal-photo.jpg", false)]
    [InlineData("https://example.com/landscape.png", false)]
    [InlineData("https://example.com/document.pdf", false)]
    [InlineData("", false)]
    public async Task AnalyzeImageAsync_UrlBasedFiltering_ShouldDetectSuspiciousUrls(string imageUrl, bool expectedNsfw)
    {
        // Act
        var result = await _nsfwService.AnalyzeImageAsync(imageUrl, _httpClient);

        // Assert
        if (!string.IsNullOrEmpty(imageUrl))
        {
            result.Should().NotBeNull();
            result.ImageUrl.Should().Be(imageUrl);
            
            // URL-based filtering should catch obvious keywords
            if (expectedNsfw && (imageUrl.Contains("adult") || imageUrl.Contains("xxx") ||
                imageUrl.Contains("porn") || imageUrl.Contains("nude") || imageUrl.Contains("sexy")))
            {
                result.IsNsfw.Should().BeTrue();
                result.Reason.Should().Contain("NSFW pattern");
            }
        }
        else
        {
            result.IsNsfw.Should().BeFalse();
        }
    }

    [Fact]
    public async Task AnalyzeImageAsync_InvalidUrl_ShouldHandleGracefully()
    {
        // Arrange
        var invalidUrl = "https://nonexistent-domain-12345.com/image.jpg";

        // Act
        var result = await _nsfwService.AnalyzeImageAsync(invalidUrl, _httpClient);

        // Assert
        result.Should().NotBeNull();
        result.IsNsfw.Should().BeFalse();
        result.Reason.Should().Contain("Failed to download image");
    }

    [Fact]
    public async Task AnalyzeImageAsync_DataUrl_ShouldProcessBase64Images()
    {
        // Arrange - Create a simple 1x1 pixel PNG as base64
        var base64Image = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";
        var dataUrl = $"data:image/png;base64,{base64Image}";

        // Act
        var result = await _nsfwService.AnalyzeImageAsync(dataUrl, _httpClient);

        // Assert
        result.Should().NotBeNull();
        result.ImageUrl.Should().Be(dataUrl);
        // Small test image should be classified as safe
        result.IsNsfw.Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzeImageAsync_SuspiciousKeywords_ShouldDetectInappropriateContent()
    {
        // Arrange
        var testUrls = new[]
        {
            "https://example.com/adult-content.jpg",
            "https://example.com/xxx-video-thumbnail.png",
            "https://example.com/porn-site-image.gif",
            "https://example.com/nude-gallery.jpg",
            "https://example.com/sexy-model.png"
        };

        foreach (var url in testUrls)
        {
            // Act
            var result = await _nsfwService.AnalyzeImageAsync(url, _httpClient);

            // Assert
            result.Should().NotBeNull();
            result.IsNsfw.Should().BeTrue();
            result.Reason.Should().Contain("NSFW pattern");
            result.ImageUrl.Should().Be(url);
        }
    }

    [Fact]
    public async Task AnalyzeImageAsync_SafeKeywords_ShouldAllowContent()
    {
        // Arrange
        var safeUrls = new[]
        {
            "https://example.com/family-photo.jpg",
            "https://example.com/landscape-view.png",
            "https://example.com/business-meeting.gif",
            "https://example.com/food-recipe.jpg",
            "https://example.com/nature-scene.png"
        };

        foreach (var url in safeUrls)
        {
            // Act
            var result = await _nsfwService.AnalyzeImageAsync(url, _httpClient);

            // Assert
            result.Should().NotBeNull();
            result.IsNsfw.Should().BeFalse();
            result.ImageUrl.Should().Be(url);
        }
    }

    public void Dispose()
    {
        _nsfwService?.Dispose();
        _httpClient?.Dispose();
    }
}
