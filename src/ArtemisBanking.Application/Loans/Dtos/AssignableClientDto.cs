namespace ArtemisBanking.Application.Loans.Dtos;

public class AssignableClientDto
{
    public Guid ClientId { get; set; }
    public string FullName { get; set; } = default!;
    public string NationalId { get; set; } = default!;
    public decimal CurrentDebt { get; set; }
    public decimal SystemAverageDebt { get; set; }
    public bool HasActiveLoan { get; set; }
}
