using System;
using System.Security.Cryptography;
using System.Text;
using ArtemisBanking.Infrastructure.Shared.Interfaces;

namespace ArtemisBanking.Infrastructure.Shared.Services;

public class CryptoService : ICryptoService
{
    public string ComputeSha256(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be empty.", nameof(input));

        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = sha.ComputeHash(bytes);

        var builder = new StringBuilder(hashBytes.Length * 2);
        foreach (var b in hashBytes)
        {
            builder.Append(b.ToString("x2"));
        }

        return builder.ToString();
    }
}
