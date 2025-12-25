using ArchiveWeb.Application.DTOs.FileArchive;
using ArchiveWeb.Application.Models;
using ArchiveWeb.Domain.Entities;

namespace ArchiveWeb.Domain.Interfaces.Services;

public interface IFileArchiveService
{
    Task<FileArchiveDto> CreateFileArchiveAsync(Guid applicantId, CancellationToken cancellationToken = default);
    FilePosition CalculatePosition(char firstLetter, Letter letter, int effectiveBoxCapacity, CancellationToken cancellationToken = default);
    Task<Letter> GetLetterByValueAsync(char letterValue, CancellationToken cancellationToken = default);
    Task<Letter> GetOverflowLetterAsync(CancellationToken cancellationToken = default);
}