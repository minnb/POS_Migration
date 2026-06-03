namespace POS.Domain.Common.Exceptions;

public class PosBusinessException : Exception
{
    public string ErrorCode { get; }

    public PosBusinessException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
