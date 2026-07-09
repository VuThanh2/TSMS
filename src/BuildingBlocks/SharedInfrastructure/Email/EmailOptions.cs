namespace SharedInfrastructure.Email;

public sealed class EmailOptions {
    public const string SectionName = "Email";

    public string Host { get; init; } = string.Empty;
    public int Port { get; init; }
    public string SenderEmail { get; init; } = string.Empty;
    public string SenderName { get; init; } = "TSMS";
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public bool EnableSsl { get; init; } = true;
}