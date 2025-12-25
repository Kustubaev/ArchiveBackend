namespace ArchiveWeb.Domain.Exceptions;

public class InvalidSurnameException : ArchiveException
{
    public InvalidSurnameException(string surname) 
        : base($"Некорректная фамилия: {surname}", "INVALID_SURNAME", 400) { }
}

