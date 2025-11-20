using System;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBanking.Infrastructure.Shared.Interfaces;

public interface INotificationService
{
    Task SendCreditCardLimitChangedAsync(
        string email,
        string cardLast4,
        decimal newLimit,
        CancellationToken cancellationToken = default);
}
