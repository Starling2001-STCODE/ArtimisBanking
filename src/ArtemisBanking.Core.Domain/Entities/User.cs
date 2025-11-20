using ArtemisBanking.Core.Domain.Enums;
using ArtemisBanking.Core.Domain.Primitives;

namespace ArtemisBanking.Core.Domain.Entities;

public class User : BaseAuditableEntity
{
    public string Username { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string NationalId { get; private set; } = default!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public ICollection<SavingsAccount> SavingsAccounts { get; private set; } = new List<SavingsAccount>();

    protected User() { }

    private User(
        string username,
        string email,
        string firstName,
        string lastName,
        string nationalId,
        UserRole role)
    {
        Username = username;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        NationalId = nationalId;
        Role = role;
        IsActive = false;
    }

    public static User CreateClient(
        string username,
        string email,
        string firstName,
        string lastName,
        string nationalId)
    {
        return new User(username, email, firstName, lastName, nationalId, UserRole.Client);
    }

    public static User CreateAdmin(
        string username,
        string email,
        string firstName,
        string lastName,
        string nationalId)
    {
        return new User(username, email, firstName, lastName, nationalId, UserRole.Admin);
    }

    public static User CreateCashier(
        string username,
        string email,
        string firstName,
        string lastName,
        string nationalId)
    {
        return new User(username, email, firstName, lastName, nationalId, UserRole.Cashier);
    }

    public static User CreateMerchant(
        string username,
        string email,
        string firstName,
        string lastName,
        string nationalId)
    {
        return new User(username, email, firstName, lastName, nationalId, UserRole.Merchant);
    }

    public void Activate()
    {
        IsActive = true;
        SetUpdatedNow();
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedNow();
    }

    public string GetFullName() => $"{FirstName} {LastName}";
}