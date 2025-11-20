using System;
using System.Threading;
using System.Threading.Tasks;
using ArtemisBanking.Infrastructure.Shared.Interfaces;
using Microsoft.Extensions.Logging;


namespace ArtemisBanking.Infrastructure.Shared.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendCreditCardLimitChangedAsync(
        string email,
        string cardLast4,
        decimal newLimit,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Credit card limit changed. Email: {Email}, Card: ****{Last4}, NewLimit: {NewLimit}",
            email,
            cardLast4,
            newLimit);

        return Task.CompletedTask;
    }
}
