// Fat interface - every consumer gets ALL methods even if they only need a few
public interface IProductRepository
{
    // Read operations
    Task<Product?> GetByIdAsync(int id);
    Task<IEnumerable<Product>> GetAllAsync();
    Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId);
    Task<IEnumerable<Product>> SearchAsync(string searchTerm);
    Task<PaginatedResult<Product>> GetPagedAsync(int page, int pageSize);
    Task<int> CountAsync();
    Task<bool> ExistsAsync(int id);

    // Write operations
    Task<Product> AddAsync(Product product);
    Task AddRangeAsync(IEnumerable<Product> products);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id);
    Task SoftDeleteAsync(int id);
    Task RestoreAsync(int id);
    Task<int> SaveChangesAsync();
}

// Problem: A reporting service that ONLY reads data is forced to depend on write methods
public class ProductReportService
{
    private readonly IProductRepository _repository; // Has access to Delete, Update, etc.!

    public ProductReportService(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<ReportData> GenerateSalesReport()
    {
        // Only uses read methods, but has access to dangerous write methods
        var products = await _repository.GetAllAsync();
        // ... generate report
    }
}

/*
Problems:
Reporting service has access to DeleteAsync, UpdateAsync - dangerous!
Unit testing requires mocking methods that will never be used
Violates principle of least privilege
Interface changes for writes affect read-only consumers
*/
