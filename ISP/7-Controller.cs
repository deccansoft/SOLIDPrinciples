[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IQueryRepository<Product> _queryRepo;
    private readonly IWriteRepository<Product> _writeRepo;
    private readonly ISoftDeleteRepository<Product> _archiveRepo;

    public ProductsController(
        IQueryRepository<Product> queryRepo,
        IWriteRepository<Product> writeRepo,
        ISoftDeleteRepository<Product> archiveRepo
    )
    {
        _queryRepo = queryRepo;
        _writeRepo = writeRepo;
        _archiveRepo = archiveRepo;
    }

    // Read operations use IQueryRepository
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<Product>>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10
    )
    {
        var result = await _queryRepo.GetPagedAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await _queryRepo.GetByIdAsync(id);
        if (product == null)
            return NotFound();
        return Ok(product);
    }

    // Write operations use IWriteRepository
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            CategoryId = dto.CategoryId,
        };

        var created = await _writeRepo.AddAsync(product);
        return CreatedAtAction(nameof(GetProduct), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, UpdateProductDto dto)
    {
        var product = await _queryRepo.GetByIdAsync(id);
        if (product == null)
            return NotFound();

        product.Name = dto.Name;
        product.Price = dto.Price;

        await _writeRepo.UpdateAsync(product);
        return NoContent();
    }

    // Archive operations use ISoftDeleteRepository
    [HttpDelete("{id}")]
    public async Task<IActionResult> ArchiveProduct(int id)
    {
        await _archiveRepo.SoftDeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> RestoreProduct(int id)
    {
        await _archiveRepo.RestoreAsync(id);
        return NoContent();
    }
}
