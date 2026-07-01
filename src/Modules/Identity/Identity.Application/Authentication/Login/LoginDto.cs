namespace Identity.Application.Authentication.Login;

public sealed record LoginInputDto(string Email, string Password);

public sealed record LoginOutputDto(string AccessToken);