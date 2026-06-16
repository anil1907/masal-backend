using System.Text;

namespace Application.Services.SmsService;

/// <summary>
/// Light E.164 normalization for Turkish numbers (MVP-grade).
/// Examples: "0543 348 86 68" -> "+905433488668", "5433488668" -> "+905433488668".
/// </summary>
public static class PhoneNumberNormalizer
{
    public static string Normalize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        bool hasPlus = raw.TrimStart().StartsWith('+');

        StringBuilder digits = new();
        foreach (char c in raw)
            if (char.IsDigit(c))
                digits.Append(c);

        string d = digits.ToString();
        if (d.Length == 0)
            return string.Empty;

        // Already in international form (came with a leading +)
        if (hasPlus)
            return "+" + d;

        // 0XXXXXXXXXX (national, 11 digits with leading 0) -> +90XXXXXXXXXX
        if (d.Length == 11 && d.StartsWith('0'))
            return "+90" + d[1..];

        // 90XXXXXXXXXX -> +90XXXXXXXXXX
        if (d.Length == 12 && d.StartsWith("90"))
            return "+" + d;

        // XXXXXXXXXX (10-digit subscriber number) -> +90XXXXXXXXXX
        if (d.Length == 10)
            return "+90" + d;

        // Fallback: assume the digits already carry a country code.
        return "+" + d;
    }
}
