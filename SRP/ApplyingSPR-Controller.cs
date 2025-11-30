[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;
    private readonly IProductMapper _mapper;

    public ProductsController(IProductRepository repository, IProductMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
    {
        var products = await _repository.GetAllActiveAsync();
        var productDtos = _mapper.ToDtoList(products);
        return Ok(productDtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        var product = await _repository.GetByIdAsync(id);
        
        if (product == null)
            return NotFound();

        return Ok(_mapper.ToDto(product));
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductDto createDto)
    {
        var product = _mapper.ToEntity(createDto);
        await _repository.AddAsync(product);
        
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, _mapper.ToDto(product));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, UpdateProductDto updateDto)
    {
        var product = await _repository.GetByIdAsync(id);
        
        if (product == null)
            return NotFound();

        _mapper.UpdateEntity(product, updateDto);
        await _repository.UpdateAsync(product);
        
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        await _repository.DeleteAsync(id);
        return NoContent();
    }
}
