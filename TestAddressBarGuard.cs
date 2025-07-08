using NoorAhlulBayt.Common.Services;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var contentFilter = new ContentFilterService();
        
        // Test the exact URL from the image
        var testUrl = "https://www.google.com/search?q=nude+pictures&ca_esv=137b7";
        
        Console.WriteLine($"Testing URL: {testUrl}");
        Console.WriteLine("=".PadRight(50, '='));
        
        // Test Step 1: Regex check
        var regexResult = contentFilter.CheckAddressBarRegexFast(testUrl);
        Console.WriteLine($"Regex Check - Blocked: {regexResult.IsBlocked}");
        Console.WriteLine($"Regex Check - Reason: {regexResult.Reason}");
        Console.WriteLine($"Regex Check - Confidence: {regexResult.Confidence:P1}");
        
        Console.WriteLine();
        
        // Test Complete Flow
        var completeResult = await contentFilter.CheckAddressBarKeywordGuardAsync(testUrl);
        Console.WriteLine($"Complete Flow - Blocked: {completeResult.IsBlocked}");
        Console.WriteLine($"Complete Flow - Reason: {completeResult.Reason}");
        Console.WriteLine($"Complete Flow - Confidence: {completeResult.Confidence:P1}");
        
        Console.WriteLine();
        
        // Test other variations
        var testUrls = new[]
        {
            "https://www.google.com/search?q=nude",
            "https://www.google.com/search?q=porn",
            "https://www.google.com/search?q=sex",
            "https://example.com/nude-content",
            "https://pornhub.com",
            "https://www.google.com/search?q=islamic+prayer"
        };
        
        foreach (var url in testUrls)
        {
            var result = contentFilter.CheckAddressBarRegexFast(url);
            Console.WriteLine($"{url} -> Blocked: {result.IsBlocked} ({result.Reason})");
        }
        
        contentFilter.Dispose();
    }
}
