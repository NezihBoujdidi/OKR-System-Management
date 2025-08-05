namespace NXM.Tensai.Back.OKR.Domain;

public class PaymentResult
{
    public bool Success { get; }
    public string TransactionId { get; }
    public string ErrorMessage { get; }

    public PaymentResult(bool success, string transactionId, string errorMessage = null)
    {
        Success = success;
        TransactionId = transactionId;
        ErrorMessage = errorMessage;
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        var other = (PaymentResult)obj;
        return Success == other.Success &&
               TransactionId == other.TransactionId &&
               ErrorMessage == other.ErrorMessage;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Success, TransactionId, ErrorMessage);
    }
}
