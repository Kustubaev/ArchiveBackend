using ArchiveWeb.Domain.Entities;
using ArchiveWeb.Domain.Interfaces.Repositories;
using ArchiveWeb.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArchiveWeb.Infrastructure.Repositories;

public sealed class ApplicantRepository : IApplicantRepository
{
    private readonly ArchiveDbContext _context;

    public ApplicantRepository(ArchiveDbContext context)
    {
        _context = context;
    }

    public async Task<Applicant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Applicants
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Applicant?> GetByIdWithFileArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Applicants
            .Include(a => a.FileArchive)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<List<Applicant>> GetPendingApplicantsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.Applicants
            .Where(a => a.FileArchive == null)
            .OrderBy(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountPendingApplicantsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Applicants
            .Where(a => a.FileArchive == null)
            .CountAsync(cancellationToken);
    }

    public async Task<List<Applicant>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.Applicants
            .OrderBy(a => a.Surname)
            .ThenBy(a => a.FirstName)
            .ThenBy(a => a.Patronymic)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(a => a.FileArchive)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Applicants.CountAsync(cancellationToken);
    }

    public async Task<List<Applicant>> SearchAsync(string searchTerm, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        string term = searchTerm.Trim().ToLower();
        return await _context.Applicants
            .Where(a =>
                (a.Surname + " " + a.FirstName + " " + (a.Patronymic ?? string.Empty)).ToLower().Contains(term) ||
                a.Email.ToLower().Contains(term) ||
                a.PhoneNumber.Contains(term))
            .OrderBy(a => a.Surname)
            .ThenBy(a => a.FirstName)
            .ThenBy(a => a.Patronymic)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(a => a.FileArchive)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountSearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        string term = searchTerm.Trim().ToLower();
        return await _context.Applicants
            .Where(a =>
                (a.Surname + " " + a.FirstName + " " + (a.Patronymic ?? string.Empty)).ToLower().Contains(term) ||
                a.Email.ToLower().Contains(term) ||
                a.PhoneNumber.Contains(term))
            .CountAsync(cancellationToken);
    }

    public async Task<Applicant> AddAsync(Applicant applicant, CancellationToken cancellationToken = default)
    {
        await _context.Applicants.AddAsync(applicant, cancellationToken);
        return applicant;
    }

    public async Task AddRangeAsync(List<Applicant> applicants, CancellationToken cancellationToken = default)
    {
        await _context.Applicants.AddRangeAsync(applicants, cancellationToken);
    }

    public Task UpdateAsync(Applicant applicant, CancellationToken cancellationToken = default)
    {
        _context.Applicants.Update(applicant);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Applicant applicant, CancellationToken cancellationToken = default)
    {
        _context.Applicants.Remove(applicant);
        return Task.CompletedTask;
    }

    public Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        _context.Applicants.RemoveRange(_context.Applicants);
        return Task.CompletedTask;
    }
}

