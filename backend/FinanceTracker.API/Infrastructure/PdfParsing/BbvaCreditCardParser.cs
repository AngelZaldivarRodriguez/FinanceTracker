using System.Globalization;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace FinanceTracker.API.Infrastructure.PdfParsing;

public record ParsedRegularTransaction(
    DateTime OperationDate,
    DateTime ChargeDate,
    string Description,
    decimal Amount,
    bool IsCredit
);

public record ParsedMsiPromotion(
    string Description,
    DateTime PurchaseDate,
    decimal OriginalAmount,
    decimal PendingBalance,
    decimal MonthlyPayment,
    int TotalMonths,
    int PaidMonths
);

public record ParsedCreditCardStatement(
    string LastFourDigits,
    decimal CreditLimit,
    decimal AvailableCredit,
    decimal RegularBalance,
    decimal MsiBalance,
    decimal TotalBalance,
    decimal PaymentToAvoidInterest,
    decimal MinimumPayment,
    int CutoffDay,
    int PaymentDueDay,
    DateTime LastStatementDate,
    DateTime NextPaymentDueDate,
    List<ParsedMsiPromotion> Promotions,
    List<ParsedRegularTransaction> RegularTransactions
);

public static class BbvaCreditCardParser
{
    private static readonly Dictionary<string, int> MonthMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ene"] = 1, ["feb"] = 2, ["mar"] = 3, ["abr"] = 4,
        ["may"] = 5, ["jun"] = 6, ["jul"] = 7, ["ago"] = 8,
        ["sep"] = 9, ["oct"] = 10, ["nov"] = 11, ["dic"] = 12
    };

    public static ParsedCreditCardStatement Parse(Stream pdfStream)
    {
        using var document = PdfDocument.Open(pdfStream);

        // Build line-by-line text using GetWords() for proper column ordering
        var lines = new List<string>();
        foreach (var page in document.GetPages())
        {
            const double lineThreshold = 3.0;
            var grouped = page.GetWords()
                .GroupBy(w => Math.Round(w.BoundingBox.Bottom / lineThreshold) * lineThreshold)
                .OrderByDescending(g => g.Key)
                .Select(g => string.Join(" ", g.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)));
            lines.AddRange(grouped);
        }

        var fullText = string.Join("\n", lines);

        var lastFour = FindGroup(fullText, @"N[uú]mero de tarjeta:\s*\d+(\d{4})") ?? "0000";

        // Use number-after-label pattern (no $ required — handles multi-column layout)
        var creditLimit        = FindAmount(fullText, @"L[ií]mite de cr[eé]dito:");
        var availableCredit    = FindAmount(fullText, @"Cr[eé]dito disponible:");
        var regularBalance     = FindAmount(fullText, @"Saldo cargos regulares:");
        var msiBalance         = FindAmount(fullText, @"Saldo cargo a meses:");
        var totalBalance       = FindAmount(fullText, @"Saldo deudor total[:\d\s]*");
        var paymentNoInterest  = FindAmount(fullText, @"Pago para no generar intereses\s*:?\s*\d*\s*\$?");
        var minPayment         = FindAmount(fullText, @"Pago m[ií]nimo:[:\d\s]*(?!\+)");

        var cutoffDate = FindSpanishDate(fullText, @"Fecha de corte:\s*(\d{1,2}-\w{3}-\d{4})");
        var dueDate    = FindSpanishDate(fullText, @"Fecha l[ií]mite de pago[^,\n]*?(\d{1,2}-\w{3}-\d{4})");

        var promotions = ParseMsiTable(fullText);
        var regularTransactions = ParseRegularTransactions(fullText);

        var result = new ParsedCreditCardStatement(
            LastFourDigits: lastFour,
            CreditLimit: creditLimit,
            AvailableCredit: availableCredit,
            RegularBalance: regularBalance,
            MsiBalance: msiBalance,
            TotalBalance: totalBalance,
            PaymentToAvoidInterest: paymentNoInterest,
            MinimumPayment: minPayment,
            CutoffDay: cutoffDate?.Day ?? 20,
            PaymentDueDay: dueDate?.Day ?? 10,
            LastStatementDate: cutoffDate ?? DateTime.UtcNow,
            NextPaymentDueDate: dueDate ?? DateTime.UtcNow.AddDays(10),
            Promotions: promotions,
            RegularTransactions: regularTransactions
        );

        Validate(result);
        return result;
    }

    private static void Validate(ParsedCreditCardStatement r)
    {
        var errors = new List<string>();

        if (r.TotalBalance == 0)
            errors.Add($"Saldo deudor total = $0.00 — posible error de formato");

        if (r.CreditLimit == 0)
            errors.Add($"Límite de crédito = $0.00 — posible error de formato");

        if (r.PaymentToAvoidInterest == 0)
            errors.Add($"Pago para no generar intereses = $0.00 — posible error de formato");

        // TotalBalance debe ser aproximadamente RegularBalance + MsiBalance (tolerancia $1)
        var expectedTotal = r.RegularBalance + r.MsiBalance;
        if (expectedTotal > 0 && Math.Abs(r.TotalBalance - expectedTotal) > 1m)
            errors.Add($"Saldo total ({r.TotalBalance:C}) no coincide con regular ({r.RegularBalance:C}) + MSI ({r.MsiBalance:C}) — posible error de formato");

        // AvailableCredit + TotalBalance debe aproximarse al límite (tolerancia 5%)
        if (r.CreditLimit > 0)
        {
            var diff = Math.Abs(r.CreditLimit - (r.AvailableCredit + r.TotalBalance));
            if (diff / r.CreditLimit > 0.05m)
                errors.Add($"Disponible ({r.AvailableCredit:C}) + saldo ({r.TotalBalance:C}) no coincide con límite ({r.CreditLimit:C}) — posible error de formato");
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(
                "El PDF no pudo parsearse correctamente. Verifica que sea un estado de cuenta BBVA válido.\n" +
                string.Join("\n", errors));
    }

    // Finds first decimal amount (X,XXX.XX) after a label, skipping footnote digits
    private static decimal FindAmount(string text, string labelPattern)
    {
        // After the label, skip footnote number (optional 1-2 digits), then find first X.XX amount
        var pattern = labelPattern + @"[^.\n]*?([\d,]{2,}\.\d{2})";
        var val = FindGroup(text, pattern);
        return ParseNum(val);
    }

    private static string? FindGroup(string text, string pattern, int group = 1)
    {
        var m = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return m.Success ? m.Groups[group].Value.Trim() : null;
    }

    private static decimal ParseNum(string? s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        return decimal.TryParse(s.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }

    private static DateTime? FindSpanishDate(string text, string pattern)
    {
        var s = FindGroup(text, pattern);
        if (string.IsNullOrEmpty(s)) return null;
        var parts = s.Split('-');
        if (parts.Length != 3) return null;
        if (!int.TryParse(parts[0], out var day)) return null;
        if (!MonthMap.TryGetValue(parts[1], out var month)) return null;
        if (!int.TryParse(parts[2], out var year)) return null;
        return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
    }

    // Parses MSI table rows
    // Format: "13-oct-2025 AMAZON A MESES ; Tarjeta Digital ***2714 $9,750.00 $3,900.00 $650.00 9 de 15 0.00%"
    private static List<ParsedMsiPromotion> ParseMsiTable(string text)
    {
        var results = new List<ParsedMsiPromotion>();
        var pattern = @"(\d{2}-\w{3}-\d{4})\s+(.+?)\s+\$?([\d,]+\.\d{2})\s+\$?([\d,]+\.\d{2})\s+\$?([\d,]+\.\d{2})\s+(\d+)\s+de\s+(\d+)\s+[\d.]+%";
        var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);

        foreach (Match m in matches)
        {
            var date = FindSpanishDate(m.Groups[1].Value, @"(\d{2}-\w{3}-\d{4})");
            if (date is null) continue;

            results.Add(new ParsedMsiPromotion(
                Description: m.Groups[2].Value.Trim(),
                PurchaseDate: date.Value,
                OriginalAmount: ParseNum(m.Groups[3].Value),
                PendingBalance: ParseNum(m.Groups[4].Value),
                MonthlyPayment: ParseNum(m.Groups[5].Value),
                PaidMonths: int.Parse(m.Groups[6].Value),
                TotalMonths: int.Parse(m.Groups[7].Value)
            ));
        }

        return results;
    }

    // Parses regular transactions section between
    // "CARGOS,COMPRAS Y ABONOS REGULARES(NO A MESES)" and "TOTAL CARGOS"
    // Row format: "21-may-2026 21-may-2026 DESCRIPTION + $1,234.56"
    private static List<ParsedRegularTransaction> ParseRegularTransactions(string text)
    {
        var results = new List<ParsedRegularTransaction>();

        // Extract the section between section header and TOTAL CARGOS
        var sectionMatch = Regex.Match(
            text,
            @"CARGOS[,\s]*COMPRAS\s*Y\s*ABONOS\s*REGULARES[^(]*\(NO\s*A\s*MESES\)(.*?)TOTAL\s*CARGOS",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (!sectionMatch.Success) return results;

        var section = sectionMatch.Groups[1].Value;

        var rowPattern = @"(\d{2}-\w{3}-\d{4})\s+(\d{2}-\w{3}-\d{4})\s+(.+?)\s+([+\-])\s+\$([\d,]+\.?\d*)";
        var matches = Regex.Matches(section, rowPattern, RegexOptions.IgnoreCase);

        foreach (Match m in matches)
        {
            var opDate = FindSpanishDate(m.Groups[1].Value, @"(\d{2}-\w{3}-\d{4})");
            var chargeDate = FindSpanishDate(m.Groups[2].Value, @"(\d{2}-\w{3}-\d{4})");
            if (opDate is null || chargeDate is null) continue;

            var sign = m.Groups[4].Value;
            var amount = ParseNum(m.Groups[5].Value);
            var isCredit = sign == "-";

            results.Add(new ParsedRegularTransaction(
                OperationDate: opDate.Value,
                ChargeDate: chargeDate.Value,
                Description: m.Groups[3].Value.Trim(),
                Amount: amount,
                IsCredit: isCredit
            ));
        }

        return results;
    }
}
