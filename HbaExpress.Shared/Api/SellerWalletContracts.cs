namespace Seller_MP_Dashboard.Api;

// ============================================================
// Portefeuille vendeur (BFF /seller/wallet) : soldes, mouvements, retraits.
// ============================================================

/// <summary>Soldes du portefeuille : à venir (en attente de livraison) + principal (retirable).</summary>
public record SellerWalletDto(decimal PendingBalance, decimal AvailableBalance, string Currency)
{
    /// <summary>Montant retenu (débité du solde principal) en attente de validation admin. JSON « pendingWithdrawal ».</summary>
    public decimal PendingWithdrawal { get; init; }
}

/// <summary>Une ligne du grand livre du portefeuille.</summary>
public record WalletTxDto(
    Guid Id, string Account, string Direction, decimal Amount, string Currency,
    string Reason, string? ReferenceType, Guid? ReferenceId, DateTime CreatedAtUtc);

/// <summary>Une demande de retrait.</summary>
public record WithdrawalDto(
    Guid Id, decimal Amount, string Currency, string Status,
    string? ProviderRef, string? FailureReason, DateTime CreatedAtUtc, DateTime? CompletedAtUtc);

/// <summary>Domaine Portefeuille du BFF Vendeur.</summary>
public interface ISellerWalletApi
{
    Task<SellerWalletDto> GetWalletAsync();
    Task<IReadOnlyList<WalletTxDto>> GetWalletTransactionsAsync();
    Task<IReadOnlyList<WithdrawalDto>> ListWithdrawalsAsync();
    Task<WithdrawalDto> RequestWithdrawalAsync(decimal amount);
}
