using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AddressFinder.Domain.Extensions;

public static partial class TextNormalizer
{
    private static readonly Regex MultiSpace = MultiSpaceRegex();
    private static readonly Regex PunctToSpace = PunctToSpaceRegex();

    public static string NormalizeForSearch(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // 1) trim + lower
        var s = input.Trim().ToLowerInvariant();

        // 2) retirer les diacritiques (accents)
        var formD = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        s = sb.ToString().Normalize(NormalizationForm.FormC);

        // 3) homogénéiser ponctuation -> espace, compacter espaces
        s = PunctToSpace.Replace(s, " ");
        s = MultiSpace.Replace(s, " ").Trim();

        return s;
    }

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex MultiSpaceRegex();
    [GeneratedRegex(@"[-’'.,/]", RegexOptions.Compiled)]
    private static partial Regex PunctToSpaceRegex();
}