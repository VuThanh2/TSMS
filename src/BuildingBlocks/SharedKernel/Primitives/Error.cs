namespace SharedKernel.Primitives;

/// Represents a domain or application error with a code and human-readable message.
public sealed class Error {
    public string Code { get; }
    public string Message { get; }

    private Error(string code, string message) {
        Code = code;
        Message = message;
    }

    public static Error Create(string code, string message) => new(code, message);

    public static readonly Error None = new(string.Empty, string.Empty);

    public override string ToString() => $"{Code}: {Message}";
}