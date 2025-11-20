namespace ArtemisBanking.Core.Domain.Enums;

public enum TransactionOrigin
{
    Cajero = 1,
    Transferencia = 2,
    Prestamo = 3,
    TarjetaCredito = 4,
    HermesPay = 5,
    AjusteManual = 6,
    System = 7
}