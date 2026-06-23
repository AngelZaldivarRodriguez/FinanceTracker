using System.Globalization;
using System.Text.RegularExpressions;
using FinanceTracker.API.Domain.Enums;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace FinanceTracker.API.Infrastructure.PdfParsing;

public static partial class BbvaStatementParser
{
    // Formato de fecha del estado de cuenta BBVA Mexico: 05/MAY
    [GeneratedRegex(@"^\d{2}/[A-Z]{3}$")]
    private static partial Regex DatePattern();

    // Numeros con formato mexicano: 1,234.56 o 234.56
    [GeneratedRegex(@"^[\d,]+\.\d{2}$")]
    private static partial Regex AmountPattern();

    private static readonly Dictionary<string, int> MonthMap = new()
    {
        ["ENE"] = 1, ["FEB"] = 2, ["MAR"] = 3, ["ABR"] = 4,
        ["MAY"] = 5, ["JUN"] = 6, ["JUL"] = 7, ["AGO"] = 8,
        ["SEP"] = 9, ["OCT"] = 10, ["NOV"] = 11, ["DIC"] = 12
    };

    public static List<BbvaParsedTransaction> Parse(Stream pdfStream)
    {
        var results = new List<BbvaParsedTransaction>();

        using var document = PdfDocument.Open(pdfStream);

        foreach (var page in document.GetPages())
        {
            var lines = GroupWordsIntoLines(page.GetWords());
            double? cargosX = null;
            double? abonosX = null;

            // Buscar la linea de encabezado para calibrar columnas
            foreach (var line in lines)
            {
                var text = string.Join(" ", line.Select(w => w.Text));
                if (text.Contains("CARGOS") && text.Contains("ABONOS"))
                {
                    cargosX = line.FirstOrDefault(w => w.Text == "CARGOS")?.BoundingBox.Left;
                    abonosX = line.FirstOrDefault(w => w.Text == "ABONOS")?.BoundingBox.Left;
                    break;
                }
            }

            if (cargosX is null || abonosX is null)
                continue;

            // Procesar lineas de transacciones
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.Count < 3)
                    continue;

                // Las transacciones empiezan con dos fechas: DD/MMM DD/MMM
                if (!DatePattern().IsMatch(line[0].Text) || !DatePattern().IsMatch(line[1].Text))
                    continue;

                var transactionWords = line.Skip(2).ToList();
                var amounts = transactionWords.Where(w => AmountPattern().IsMatch(w.Text)).ToList();
                var descWords = transactionWords.Where(w => !AmountPattern().IsMatch(w.Text)).ToList();

                if (amounts.Count == 0)
                    continue;

                // La primera cantidad es siempre el monto (cargo o abono)
                // Las siguientes son saldos — las ignoramos
                var amountWord = amounts[0];
                var amount = ParseAmount(amountWord.Text);
                var type = ClassifyByColumn(amountWord.BoundingBox.Left, cargosX.Value, abonosX.Value);

                var description = string.Join(" ", descWords.Select(w => w.Text)).Trim();

                // La segunda linea contiene la referencia
                string? reference = null;
                if (i + 1 < lines.Count)
                {
                    var nextLine = lines[i + 1];
                    var refWord = nextLine.FirstOrDefault(w => w.Text == "Referencia");
                    if (refWord != null)
                    {
                        var refValue = nextLine.SkipWhile(w => w.Text != "Referencia").Skip(1).FirstOrDefault();
                        reference = refValue?.Text;
                    }
                }

                var date = ParseDate(line[0].Text);
                if (date is null || amount <= 0 || string.IsNullOrWhiteSpace(description))
                    continue;

                results.Add(new BbvaParsedTransaction(date.Value, description, amount, type, reference));
            }
        }

        return results;
    }

    private static List<List<Word>> GroupWordsIntoLines(IEnumerable<Word> words)
    {
        const double lineThreshold = 3.0;

        var grouped = words
            .GroupBy(w => Math.Round(w.BoundingBox.Bottom / lineThreshold) * lineThreshold)
            .OrderByDescending(g => g.Key)
            .Select(g => g.OrderBy(w => w.BoundingBox.Left).ToList())
            .ToList();

        return grouped;
    }

    private static TransactionType ClassifyByColumn(double wordX, double cargosX, double abonosX)
    {
        // El punto medio entre las dos columnas define el umbral
        var midpoint = (cargosX + abonosX) / 2;
        return wordX >= midpoint ? TransactionType.Income : TransactionType.Expense;
    }

    private static decimal ParseAmount(string text)
    {
        var clean = text.Replace(",", "");
        return decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : 0;
    }

    private static DateTime? ParseDate(string text)
    {
        // Formato: 05/MAY
        var parts = text.Split('/');
        if (parts.Length != 2) return null;
        if (!int.TryParse(parts[0], out var day)) return null;
        if (!MonthMap.TryGetValue(parts[1], out var month)) return null;

        var year = month > DateTime.UtcNow.Month ? DateTime.UtcNow.Year - 1 : DateTime.UtcNow.Year;
        return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
    }
}
