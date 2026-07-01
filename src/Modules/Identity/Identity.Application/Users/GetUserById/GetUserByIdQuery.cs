using Identity.Application.Common.Mappers;
using Identity.Domain.Errors;
using Identity.Domain.Repositories;
using MediatR;
using SharedKernel.Primitives;

namespace Identity.Application.Users.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId) : IRequest<Result<GetUserByIdOutputDto>>;

// UC-09 (context): Admin xem chi tiết thông tin một người dùng theo ID
public sealed class GetUserByIdQueryHandler
    : IRequestHandler<GetUserByIdQuery, Result<GetUserByIdOutputDto>> {
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository) {
        _userRepository = userRepository;
    }

    public async Task<Result<GetUserByIdOutputDto>> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken) {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return Result.Failure<GetUserByIdOutputDto>(UserErrors.NotFound);

        return Result.Success(UserMapper.ToGetUserByIdOutputDto(user));
    }
}