namespace ArchiveWeb.Domain.Exceptions;

public class ArchiveException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }
    
    public ArchiveException(string message, string errorCode, int statusCode) 
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}

