// Base entity interface
public interface IEntity
{
    int Id { get; }
}

// Soft-delete support
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}

// Audit support
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    string? CreatedBy { get; set; }
    DateTime? ModifiedAt { get; set; }
    string? ModifiedBy { get; set; }
}

// ========== READ-ONLY INTERFACES ==========

// Basic read operations
public interface IReadRepository<T>
    where T : class, IEntity
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<bool> ExistsAsync(int id);
    Task<int> CountAsync();
}

// Extended query operations (optional - clients can choose to depend on this or not)
public interface IQueryRepository<T> : IReadRepository<T>
    where T : class, IEntity
{
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<PaginatedResult<T>> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<T, bool>>? filter = null,
        Expression<Func<T, object>>? orderBy = null,
        bool ascending = true
    );
}

// Specification pattern support (for complex queries)
public interface ISpecificationRepository<T>
    where T : class, IEntity
{
    Task<IEnumerable<T>> FindAsync(ISpecification<T> specification);
    Task<T?> FirstOrDefaultAsync(ISpecification<T> specification);
    Task<int> CountAsync(ISpecification<T> specification);
}

// ========== WRITE-ONLY INTERFACES ==========

// Basic write operations
public interface IWriteRepository<T>
    where T : class, IEntity
{
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}

// Bulk operations (separate - not all consumers need bulk)
public interface IBulkWriteRepository<T>
    where T : class, IEntity
{
    Task AddRangeAsync(IEnumerable<T> entities);
    Task UpdateRangeAsync(IEnumerable<T> entities);
    Task DeleteRangeAsync(IEnumerable<int> ids);
}

// Soft delete operations (only for entities that support it)
public interface ISoftDeleteRepository<T>
    where T : class, IEntity, ISoftDeletable
{
    Task SoftDeleteAsync(int id);
    Task RestoreAsync(int id);
    Task<IEnumerable<T>> GetDeletedAsync();
}

// ========== COMBINED INTERFACE (for full CRUD when needed) ==========

public interface IRepository<T> : IReadRepository<T>, IWriteRepository<T>
    where T : class, IEntity
{
    // Combines read and write - use only when both are genuinely needed
}
