using FinanceTracker.API.Domain.Enums;

namespace FinanceTracker.API.Infrastructure.PdfParsing;

public static class CategoryMapper
{
    private static readonly (string[] Keywords, string CategoryName)[] Rules =
    [
        (["UBER EATS", "DIDI FOOD", "PAYCLIP*REST", "RAPPI", "DOORDASH"], "Comida"),
        (["DLO*UBER", "UBER", "DIDI"], "Transporte"),
        (["7ELEVEN", "7-ELEVEN", "OXXO", "EXTRA"], "Conveniencia"),
        (["NETFLIX", "SPOTIFY", "GOOGLE", "YOUTUBE", "APPLE", "DISNEY", "HBO", "AMAZON PRIME"], "Suscripciones"),
        (["TELEFONOS DE MEXICO", "TELMEX", "TELCEL", "AT&T", "TOTALPLAY", "MEGACABLE"], "Servicios"),
        (["FARMACIA", "FARMACIAS", "HOSPITAL", "CLINICA", "MEDICA", "DOCTOR"], "Salud"),
        (["SPEI RECIBIDO", "PAGO DE NOMINA", "NOMINA", "GBM", "STP RECIBIDO"], "Ingresos"),
        (["SPEI ENVIADO", "TRANSFERENCIA", "PAGO CUENTA DE TERCERO", "RECARGAS Y PAQUETES"], "Transferencias"),
        (["CETELEM", "TARJETA DE CREDITO", "PAGO TARJETA"], "Transferencias"),
    ];

    public static string Suggest(string description, TransactionType type)
    {
        var upper = description.ToUpperInvariant();

        foreach (var (keywords, category) in Rules)
        {
            if (keywords.Any(k => upper.Contains(k)))
                return category;
        }

        return type == TransactionType.Income ? "Ingresos" : "Otros";
    }
}
