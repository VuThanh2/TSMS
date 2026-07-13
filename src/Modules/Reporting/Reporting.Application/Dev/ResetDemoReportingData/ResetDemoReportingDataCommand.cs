using MediatR;
using Reporting.Application.Common.Interfaces;
using SharedKernel.Primitives;

namespace Reporting.Application.Dev.ResetDemoReportingData;

public sealed record ResetDemoReportingDataCommand : IRequest<Result<ResetDemoReportingDataOutputDto>>;

public sealed class ResetDemoReportingDataCommandHandler
    : IRequestHandler<ResetDemoReportingDataCommand, Result<ResetDemoReportingDataOutputDto>> {
    private readonly IReportingDataResetter _reportingDataResetter;

    public ResetDemoReportingDataCommandHandler(IReportingDataResetter reportingDataResetter) {
        _reportingDataResetter = reportingDataResetter;
    }

    public async Task<Result<ResetDemoReportingDataOutputDto>> Handle(
        ResetDemoReportingDataCommand request,
        CancellationToken cancellationToken) {
        await _reportingDataResetter.ClearAllAsync(cancellationToken);

        return Result.Success(new ResetDemoReportingDataOutputDto(true));
    }
}