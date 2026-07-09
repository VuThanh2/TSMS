namespace SharedInfrastructure.Email;

// Generic email sender — không thuộc BC nào, tương tự cách Outbox được đặt ở
// SharedInfrastructure.
public interface IEmailSender {
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}