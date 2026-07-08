using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seller_MP_Dashboard.Api;

/// <summary>
/// Convertisseur d'enum tolérant pour les réponses du BFF :
/// - accepte les valeurs en chaîne (insensible à la casse) ou numériques ;
/// - retombe sur la valeur par défaut de l'enum si la valeur est inconnue
///   (évite une exception quand le BFF introduit une valeur non prévue).
/// </summary>
public class TolerantEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                var s = reader.GetString();
                return Enum.TryParse<T>(s, ignoreCase: true, out var parsed) ? parsed : default;

            case JsonTokenType.Number when reader.TryGetInt32(out var n) && Enum.IsDefined(typeof(T), n):
                return (T)Enum.ToObject(typeof(T), n);

            default:
                return default;
        }
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
