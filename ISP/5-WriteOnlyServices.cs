// ===== WRITE-ONLY SERVICES =====

// Product management - needs write access
public class ProductManagementService
{
    private readonly IWriteRepository<Product> _writeRepository;
    private readonly IReadRepository<Product> _readRepository;

    public ProductManagementService(
        IWriteRepository<Product> writeRepository,
        IReadRepository<Product> readRepository
    )
    {
        _writeRepository = writeRepository;
        _readRepository = readRepository;
    }

    public async Task<Product> CreateProductAsync(CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            CategoryId = dto.CategoryId,
            StockQuantity = dto.StockQuantity,
        };

        return await _writeRepository.AddAsync(product);
    }

    public async Task UpdateProductAsync(int id, UpdateProductDto dto)
    {
        var product = await _readRepository.GetByIdAsync(id);
        if (product == null)
            throw new NotFoundException($"Product {id} not found");

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.CategoryId = dto.CategoryId;

        await _writeRepository.UpdateAsync(product);
    }

    public async Task DeleteProductAsync(int id)
    {
        if (!await _readRepository.ExistsAsync(id))
            throw new NotFoundException($"Product {id} not found");

        await _writeRepository.DeleteAsync(id);
    }
}

// Bulk import service - needs bulk write operations
public class ProductImportService
{
    private readonly IBulkWriteRepository<Product> _bulkRepository;

    public ProductImportService(IBulkWriteRepository<Product> bulkRepository)
    {
        _bulkRepository = bulkRepository;
    }

    public async Task ImportFromCsvAsync(Stream csvStream)
    {
        var products = ParseCsvToProducts(csvStream);
        await _bulkRepository.AddRangeAsync(products);
    }

    private IEnumerable<Product> ParseCsvToProducts(Stream stream)
    {
        // CSV parsing logic
        yield break;
    }
}

// Archive service - needs soft delete operations
public class ProductArchiveService
{
    private readonly ISoftDeleteRepository<Product> _softDeleteRepository;

    public ProductArchiveService(ISoftDeleteRepository<Product> softDeleteRepository)
    {
        _softDeleteRepository = softDeleteRepository;
    }

    public async Task ArchiveProductAsync(int id)
    {
        await _softDeleteRepository.SoftDeleteAsync(id);
    }

    public async Task RestoreProductAsync(int id)
    {
        await _softDeleteRepository.RestoreAsync(id);
    }

    public async Task<IEnumerable<Product>> GetArchivedProductsAsync()
    {
        return await _softDeleteRepository.GetDeletedAsync();
    }
}
