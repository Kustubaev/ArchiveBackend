namespace ArchiveWeb.Domain.Exceptions;

public class BoxFullException : ArchiveException
{
    public BoxFullException(int boxNumber) 
        : base($"Коробка {boxNumber} заполнена", "BOX_FULL", 400) { }
}

