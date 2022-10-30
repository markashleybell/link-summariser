using System.Net;
using AngleSharp;
using AngleSharp.Io;
using Azure.AI.TextAnalytics;

namespace LinkSummariser;

public static class Functions
{
    public const string UserAgent = "MAB Link Summariser/0.0.1";

    public static readonly HttpClient HttpClient = new();

    public static readonly HttpClientHandler HttpClientHandler = new() {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };

    public static readonly IConfiguration AngleSharpConfig;

    public static readonly IBrowsingContext AngleSharpBrowsingContext;

    static Functions()
    {
        HttpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(UserAgent);

        var requester = new DefaultHttpRequester();

        requester.Headers["User-Agent"] = UserAgent;

        AngleSharpConfig = Configuration.Default
            .With(requester)
            .WithDefaultLoader()
            .WithDefaultCookies();

        AngleSharpBrowsingContext = BrowsingContext.New(AngleSharpConfig);
    }

    public static async Task<Dictionary<string, (IEnumerable<string> SummarySentences, TextAnalyticsError? Error)>> TrySummarise(
        this TextAnalyticsClient client,
        IDictionary<string, string?> documents)
    {
        var batchInput = documents
            .Where(d => !string.IsNullOrWhiteSpace(d.Value))
            .Select(d => new TextDocumentInput(d.Key, d.Value));

        var actions = new TextAnalyticsActions {
            ExtractSummaryActions = new List<ExtractSummaryAction>() {
                new ExtractSummaryAction()
            }
        };

        var operation = await client.StartAnalyzeActionsAsync(batchInput, actions);

        await operation.WaitForCompletionAsync();

        var results = new Dictionary<string, (IEnumerable<string> SummarySentences, TextAnalyticsError? Error)>();

        await foreach (var item in operation.Value)
        {
            foreach (var summaryResult in item.ExtractSummaryResults)
            {
                if (summaryResult.HasError)
                {
                    // This means something went fundamentally wrong, e.g. service unavailable
                    throw new Exception($"{summaryResult.Error.ErrorCode}: {summaryResult.Error.Message}");
                }

                foreach (var document in summaryResult.DocumentsResults)
                {
                    var result = !document.HasError
                        ? (document.Sentences.Select(s => s.Text), default(TextAnalyticsError?))
                        : (Enumerable.Empty<string>(), document.Error);

                    results.Add(document.Id, result);
                }
            }
        }

        return results;
    }

    public static async Task<Article> GetContent(
        this string url)
    {
        var uri = new Uri(url);

        var msg = new HttpRequestMessage {
            Method = System.Net.Http.HttpMethod.Head,
            RequestUri = uri
        };

        try
        {
            var check = await HttpClient.SendAsync(msg);

            if (!check.IsSuccessStatusCode)
            {
                return new Article(uri, null, check.StatusCode, check.ReasonPhrase);
            }
        }
        catch (HttpRequestException e) when (e.InnerException is WebException)
        {
            return new Article(uri, null, null, e.InnerException.Message);
        }

        var doc = await AngleSharpBrowsingContext.OpenAsync(url);

        /*
        Only return paragraphs with a certain number of spaces,
        to try and filter out bad use of the <p> tag for e.g.
        headers/decorative text/UI controls (very common...)
        */
        var text = doc.QuerySelectorAll("p")
            .Select(p => p.TextContent.Trim())
            .Where(t => t.Count(char.IsWhiteSpace) > 5);

        return new Article(uri, doc.Title, HttpStatusCode.OK, "OK")
            .With(content: string.Join(" ", text));
    }
}
