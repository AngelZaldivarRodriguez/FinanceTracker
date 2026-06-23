using MediatR;

namespace FinanceTracker.API.Features.Import;

public record ImportBbvaCommand(Guid UserId, Stream PdfStream) : IRequest<ImportBbvaResponse>;

public record ImportBbvaResponse(int Imported, int Skipped, List<string> Errors);
