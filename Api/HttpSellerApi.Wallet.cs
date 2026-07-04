using System.Net.Http.Json;

namespace Seller_MP_Dashboard.Api;

// Implémentation HTTP du domaine Portefeuille (/seller/wallet).
public partial class HttpSellerApi
{
    public async Task<SellerWalletDto> GetWalletAsync()
        => (await _http.GetFromJsonAsync<SellerWalletDto>("/seller/wallet"))
           ?? new SellerWalletDto(0m, 0m, "XOF");

    public async Task<IReadOnlyList<WalletTxDto>> GetWalletTransactionsAsync()
        => (await _http.GetFromJsonAsync<List<WalletTxDto>>("/seller/wallet/transactions")) ?? new();

    public async Task<IReadOnlyList<WithdrawalDto>> ListWithdrawalsAsync()
        => (await _http.GetFromJsonAsync<List<WithdrawalDto>>("/seller/wallet/withdrawals")) ?? new();

    public async Task<WithdrawalDto> RequestWithdrawalAsync(decimal amount)
        => await PostJson<WithdrawalDto>("/seller/wallet/withdraw", new { amount });
}
