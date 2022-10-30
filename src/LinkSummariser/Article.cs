using System.Net;

namespace LinkSummariser
{
    public class Article
    {
        public Article(
            Uri uri,
            string? title,
            HttpStatusCode? status,
            string? message)
            : this(
                uri,
                title,
                status,
                message,
                default,
                Enumerable.Empty<string>())
        { }

        public Article(
            Uri uri,
            string? title,
            HttpStatusCode? status,
            string? message,
            string? content,
            IEnumerable<string> summarySentences)
        {
            Uri = uri;
            Title = title;
            Status = status;
            Message = message;
            Content = content;
            SummarySentences = summarySentences;
        }

        public Uri Uri { get; }

        public string? Title { get; }

        public HttpStatusCode? Status { get; }

        public string? Message { get; }

        public string? Content { get; }

        public IEnumerable<string> SummarySentences { get; }

        public Article With(
            string? content = null,
            IEnumerable<string>? summarySentences = null) =>
            new(
                Uri,
                Title,
                Status,
                Message,
                content ?? Content,
                summarySentences ?? SummarySentences
            );
    }
}
