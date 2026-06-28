using Seller_MP_Dashboard.Api;

namespace Seller_MP_Dashboard.Pages;

public partial class Avis
{
    private IReadOnlyList<Review>? _reviews;
    private ProductRating? _rating;
    private Guid _replyingTo = Guid.Empty;
    private string _replyText = "";
    private string? _loadError;

    protected override async Task OnInitializedAsync() => await Reload();

    private async Task Reload()
    {
        try
        {
            _loadError = null;
            _reviews = await Api.ListReviewsAsync();        // GET /seller/reviews
            _rating = BuildRating(_reviews);               // note agrégée côté client
        }
        catch
        {
            _loadError = "Impossible de joindre le serveur (BFF injoignable ou session expirée).";
        }
    }

    private static ProductRating BuildRating(IReadOnlyList<Review> reviews)
    {
        var breakdown = new int[5];
        foreach (var r in reviews)
            breakdown[Math.Clamp(r.Rating - 1, 0, 4)]++;

        return new ProductRating
        {
            ProductId = Guid.Empty,
            Count = reviews.Count,
            Average = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0,
            Breakdown = breakdown
        };
    }

    private void StartReply(Review r) { _replyingTo = r.Id; _replyText = ""; }

    private async Task SubmitReply(Review r)
    {
        if (!string.IsNullOrWhiteSpace(_replyText))
            await Api.ReplyAsync(r.Id, _replyText);
        _replyingTo = Guid.Empty;
        await Reload();
    }

    private async Task Flag(Review r) { await Api.FlagAsync(r.Id); await Reload(); }
    private static string Stars(int n) => new string('★', Math.Clamp(n, 0, 5)) + new string('☆', 5 - Math.Clamp(n, 0, 5));
}
