using Microsoft.AspNetCore.Http;

namespace Identity.Application.Users.ImportUsersCsv;

public sealed record ImportUsersCsvInputDto(IFormFile File);

public sealed record ImportUsersCsvOutputDto(
    int SuccessCount,
    int FailureCount,
    IReadOnlyList<CsvRowError> Errors);

public sealed record CsvRowError(int RowNumber, string Reason);