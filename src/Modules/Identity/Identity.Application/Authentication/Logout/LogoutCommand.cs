using Identity.Domain.Events;
using Identity.Domain.Errors;
using Identity.Domain.Repositories;
using MediatR;
using SharedKernel.Primitives;

namespace Identity.Application.Authentication.Logout;

public sealed record LogoutCommand(Guid UserId) : IRequest<Result>;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result> {
    private readonly IUserRepository _userRepository;
    private readonly IPublisher _publisher;

    public LogoutCommandHandler(IUserRepository userRepository, IPublisher publisher) {
        _userRepository = userRepository;
        _publisher = publisher;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken) {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        // Publish trực tiếp qua MediatR (in-process) —
        // Identity BC không dùng Outbox vì UserLoggedOut là audit event
        await _publisher.Publish(UserLoggedOutEvent.Create(user.Id), cancellationToken);

        return Result.Success();
    }
}