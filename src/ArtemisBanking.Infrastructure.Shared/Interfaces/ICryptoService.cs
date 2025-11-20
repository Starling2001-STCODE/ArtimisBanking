using System;

namespace ArtemisBanking.Infrastructure.Shared.Interfaces;

public interface ICryptoService
{
    string ComputeSha256(string input);
}
