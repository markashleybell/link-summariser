<Query Kind="Program">
  <Reference Relative="..\src\LinkSummariser\bin\Debug\net6.0\LinkSummariser.dll">D:\Src\Personal\link-summariser\src\LinkSummariser\bin\Debug\net6.0\LinkSummariser.dll</Reference>
  <NuGetReference>AngleSharp</NuGetReference>
  <NuGetReference Version="5.2.0-beta.3">Azure.AI.TextAnalytics</NuGetReference>
  <NuGetReference>Microsoft.Extensions.Configuration.Json</NuGetReference>
  <Namespace>AngleSharp</Namespace>
  <Namespace>AngleSharp.Attributes</Namespace>
  <Namespace>AngleSharp.Browser</Namespace>
  <Namespace>AngleSharp.Browser.Dom</Namespace>
  <Namespace>AngleSharp.Browser.Dom.Events</Namespace>
  <Namespace>AngleSharp.Common</Namespace>
  <Namespace>AngleSharp.Css</Namespace>
  <Namespace>AngleSharp.Css.Dom</Namespace>
  <Namespace>AngleSharp.Css.Parser</Namespace>
  <Namespace>AngleSharp.Dom</Namespace>
  <Namespace>AngleSharp.Dom.Events</Namespace>
  <Namespace>AngleSharp.Html</Namespace>
  <Namespace>AngleSharp.Html.Dom</Namespace>
  <Namespace>AngleSharp.Html.Dom.Events</Namespace>
  <Namespace>AngleSharp.Html.Forms</Namespace>
  <Namespace>AngleSharp.Html.Forms.Submitters</Namespace>
  <Namespace>AngleSharp.Html.InputTypes</Namespace>
  <Namespace>AngleSharp.Html.LinkRels</Namespace>
  <Namespace>AngleSharp.Html.Parser</Namespace>
  <Namespace>AngleSharp.Html.Parser.Tokens</Namespace>
  <Namespace>AngleSharp.Io</Namespace>
  <Namespace>AngleSharp.Io.Dom</Namespace>
  <Namespace>AngleSharp.Io.Processors</Namespace>
  <Namespace>AngleSharp.Mathml.Dom</Namespace>
  <Namespace>AngleSharp.Media</Namespace>
  <Namespace>AngleSharp.Media.Dom</Namespace>
  <Namespace>AngleSharp.Scripting</Namespace>
  <Namespace>AngleSharp.Svg.Dom</Namespace>
  <Namespace>AngleSharp.Text</Namespace>
  <Namespace>AngleSharp.Xhtml</Namespace>
  <Namespace>Azure</Namespace>
  <Namespace>Azure.AI.TextAnalytics</Namespace>
  <Namespace>Azure.AI.TextAnalytics.Models</Namespace>
  <Namespace>LinkSummariser</Namespace>
  <Namespace>Microsoft.Extensions.Azure</Namespace>
  <Namespace>System.Collections.Concurrent</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Microsoft.Extensions.Configuration</Namespace>
  <RuntimeVersion>6.0</RuntimeVersion>
</Query>

async Task Main()
{
    /* 
    AZ AI code based on https://learn.microsoft.com/en-us/azure/cognitive-services/language-service/summarization/quickstart
    
    Free tier supports 5000 transactions per month (should be plenty...)
    
    IMPORTANT NOTE: We currently need to use this *exact* version 
    of the Azure.AI.TextAnalytics package (5.2.0-beta.3), because 
    the stable 5.2.0 release removes the ExtractSummaryAction class 
    for some reason (and doesn't mention that in the changelog)...
    
    To be fair, I guess all of this is in preview! 
    
    It looks like this is being added back in here:
    
    https://github.com/Azure/azure-sdk-for-net/pull/32023
    
    But the latest package hasn't been released yet.
    */

    // Create an appsettings.TestHarness.json in the same folder as this query file
    var config = GetConfig("TestHarness");
    
    var credentials = new AzureKeyCredential(config["AzureAI:Key"]);
    var endpoint = new Uri(config["AzureAI:Endpoint"]);

    var client = new TextAnalyticsClient(endpoint, credentials);

    var progress = new DumpContainer("Ready").Dump();

    var articleData = new ConcurrentDictionary<string, Article>();

    var output = new DumpContainer(articleData).Dump(1);

    var articleUrls = new[] {
        "https://www.thereformedprogrammer.net/advanced-techniques-around-asp-net-core-users-and-their-claims/",
        "https://developers.redhat.com/articles/2022/01/25/disadvantages-microservices#more_complexity_in_exchange_for_more_flexibility",
        "https://jeremydmiller.com/2022/01/19/my-thoughts-on-code-modernization/"
    };

    foreach (var url in articleUrls)
    {
        progress.Content = "Fetching " + url;
        
        var a = await url.GetContent();
        
        articleData.TryAdd(url, a);
        
        progress.Content = "Retrieved " + url;
        
        output.Refresh();
    }
    
    progress.Content = "All content retrieved, summarising...";
    
    var content = articleData.ToDictionary(a => a.Key, a => a.Value.Content);
    
    var summaryData = await client.TrySummarise(content);
    
    progress.Content = "Summarisation complete";
    
    var errors = summaryData.Where(s => s.Value.Error != null);
    
    if (errors.Any())
    {
        errors.Dump("Errors");
    }
    
    var articles = articleData
        .Select(a => summaryData.TryGetValue(a.Key, out var v) 
            ? a.Value.With(summarySentences: v.SummarySentences)
            : a.Value);

    // articles.Dump();

    var result = articles.Select(a => new { 
        Url = a.Uri.ToString(),
        Summary = string.Join(Environment.NewLine + Environment.NewLine, a.SummarySentences) 
    });
    
    result.Dump();
}

public static Microsoft.Extensions.Configuration.IConfiguration GetConfig(string environmentName)
{
    var basePath = Path.GetDirectoryName(Util.CurrentQueryPath);
    
    var config = new ConfigurationBuilder()
        .SetBasePath(basePath)
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{environmentName}.json", true);
        
    return config.Build();
}
