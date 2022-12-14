using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.AspNetCore.SignalR;

namespace LinkSummariser.WebUI.Models
{
    public class ProcessorHub : Hub
    {
        private const string Update = nameof(Update);
        private const string Errors = nameof(Errors);
        private const string Results = nameof(Results);

        private readonly IConfiguration _cfg;

        public ProcessorHub(IConfiguration cfg) =>
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));

        public async Task Process(string data)
        {
            var urls = Regex.Split(data, @"\s+");

            if (urls.Length == 0)
            {
                await Clients.All.SendAsync(Update, "No link URLs submitted");
            }

            var articleData = new ConcurrentDictionary<string, Article>();

            foreach (var url in urls)
            {
                await Clients.All.SendAsync(Update, $"Fetching {url}");

                var a = await url.GetContent();

                articleData.TryAdd(url, a);

                await Clients.All.SendAsync(Update, $"Retrieved {url}");
            }

            var articlesWithNoContent = articleData
                .Where(a => string.IsNullOrWhiteSpace(a.Value.Content));

            if (articlesWithNoContent.Any())
            {
                var noContentErrors = articlesWithNoContent
                    .Select(a => $"No content found at {a.Key} (in paragraph tags)");

                await Clients.All.SendAsync(Errors, noContentErrors);
            }

            await Clients.All.SendAsync(Update, "All content retrieved, summarising...");

            var credentials = new AzureKeyCredential(_cfg["AzureAI:Key"]);
            var endpoint = new Uri(_cfg["AzureAI:Endpoint"]);

            var client = new TextAnalyticsClient(endpoint, credentials);

            var content = articleData.ToDictionary(a => a.Key, a => a.Value.Content);

            var summaryData = await client.TrySummarise(content);

            var errors = summaryData
                .Where(s => s.Value.Error != null)
                .Select(s => $"{s.Value.Error?.ErrorCode}: {s.Value.Error?.Message}");

            await Clients.All.SendAsync(Update, "Summarisation complete");

            if (errors.Any())
            {
                await Clients.All.SendAsync(Errors, errors);
            }

            var summaryDataNoErrors = summaryData
                .Where(s => s.Value.Error == null)
                .ToDictionary(s => s.Key, s => s.Value);

            var articles = articleData
                .Select(a => summaryDataNoErrors.TryGetValue(a.Key, out var v)
                ? a.Value.With(summarySentences: v.SummarySentences)
                : a.Value);

            var results = new {
                Summaries = articles.Select(a => new {
                    Url = a.Uri.ToString(),
                    a.SummarySentences
                })
            };

            await Clients.All.SendAsync(Results, results);
        }
    }
}
