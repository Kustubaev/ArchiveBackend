using System.ComponentModel.DataAnnotations;

namespace ArchiveWeb.Domain.Entities;

public abstract class EntityBase
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

