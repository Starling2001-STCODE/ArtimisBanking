namespace ArtemisBanking.Core.Domain.Enums;

public enum CreditCardTransactionType
{
	Purchase = 1,      // Consumo normal en comercio
	CashAdvance = 2,   // AVANCE de efectivo
	Payment = 3        // Pago que reduce la deuda
}

