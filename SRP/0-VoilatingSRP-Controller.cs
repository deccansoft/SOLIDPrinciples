//This controller has three responsibilities: HTTP handling, data access, and object mapping.
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
    {
        // Data Access Logic (should be in Repository)
        var products = await _context
            .Products.Where(p => p.IsActive)
            .Include(p => p.Category)
            .ToListAsync();

        // Mapping Logic (should be in Mapper)
        var productDtos = products
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                CategoryName = p.Category.Name,
                DisplayPrice = $"₹{p.Price:N2}",
            })
            .ToList();

        return Ok(productDtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        // Data Access Logic
        var product = await _context
            .Products.Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound();

        // Mapping Logic
        var productDto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            CategoryName = product.Category.Name,
            DisplayPrice = $"₹{product.Price:N2}",
        };

        return Ok(productDto);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductDto createDto)
    {
        // Mapping Logic (DTO to Entity)
        var product = new Product
        {
            Name = createDto.Name,
            Price = createDto.Price,
            CategoryId = createDto.CategoryId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        // Data Access Logic
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // More Mapping Logic (Entity to DTO)
        var productDto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            DisplayPrice = $"₹{product.Price:N2}",
        };

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, productDto);
    }
}
/*
Problems:
Controller knows about EF Core queries (data access details)
Mapping logic duplicated across methods
Hard to unit test (requires database)
Any change to mapping or data access requires modifying the controller
*/
