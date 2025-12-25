namespace ArchiveWeb.Domain.Exceptions;

public class LetterNotFoundException : ArchiveException
{
    public LetterNotFoundException(char letter) 
        : base($"Буква '{letter}' не найдена", "LETTER_NOT_FOUND", 404) { }
}

