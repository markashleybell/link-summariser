namespace LinkSummariser.WebUI.Models;

public class IndexViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
