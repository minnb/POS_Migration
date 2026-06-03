namespace POS.Domain.Common.Exceptions;

public class PosNotFoundException : Exception
{
    public string ErrorCode { get; }

    public PosNotFoundException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
