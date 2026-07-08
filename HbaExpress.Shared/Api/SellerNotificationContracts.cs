using System.Text.Json.Serialization;

namespace Seller_MP_Dashboard.Api;

// ============================================================
// Notifications vendeur (BFF /seller/notifications).
// Le BFF renvoie le NotificationSummary du module Notifications :
//   { id, recipientUserId, channel, subject, body, relatedEntityType,
//     relatedEntityId, status, createdAtUtc, readAtUtc }
// On projette ici la vue utile à l'UI (titre, message, type, date, lu).
// ============================================================

/// <summary>Vue d'une notification pour le back-office vendeur.</summary>
public sealed record SellerNotification
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    /// <summary>Titre affiché (mappé sur « subject » côté BFF).</summary>
    [JsonPropertyName("subject")]
    public string Title { get; init; } = string.Empty;

    /// <summary>Corps du message (mappé sur « body » côté BFF).</summary>
    [JsonPropertyName("body")]
    public string Message { get; init; } = string.Empty;

    /// <summary>Canal / type (InApp, Email…) tel qu'exposé par le module.</summary>
    [JsonPropertyName("channel")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; init; }

    /// <summary>Date de lecture (null = non lue).</summary>
    [JsonPropertyName("readAtUtc")]
    public DateTime? ReadAtUtc { get; init; }

    /// <summary>Non lue tant que « readAtUtc » est absent.</summary>
    [JsonIgnore]
    public bool IsRead => ReadAtUtc is not null;
}

/// <summary>Domaine Notifications du BFF Vendeur.</summary>
public interface ISellerNotificationsApi
{
    Task<IReadOnlyList<SellerNotification>> ListNotificationsAsync();
    Task MarkNotificationReadAsync(Guid id);
    Task MarkAllNotificationsReadAsync();

    /// <summary>Enregistre le jeton d'appareil (FCM) pour recevoir les notifications push.</summary>
    Task RegisterDeviceAsync(string token, string platform);

    /// <summary>Retire le jeton d'appareil (à la déconnexion).</summary>
    Task UnregisterDeviceAsync(string token);
}
