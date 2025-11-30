// Interface
public interface IProductMapper
{
    ProductDto ToDto(Product product);
    IEnumerable<ProductDto> ToDtoList(IEnumerable<Product> products);
    Product ToEntity(CreateProductDto createDto);
    void UpdateEntity(Product product, UpdateProductDto updateDto);
}

// Implementation
public class ProductMapper : IProductMapper
{
    public ProductDto ToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            CategoryName = product.Category?.Name ?? "Uncategorized",
            DisplayPrice = $"₹{product.Price:N2}"
        };
    }

    public IEnumerable<ProductDto> ToDtoList(IEnumerable<Product> products)
    {
        return products.Select(ToDto);
    }

    public Product ToEntity(CreateProductDto createDto)
    {
        return new Product
        {
            Name = createDto.Name,
            Price = createDto.Price,
            CategoryId = createDto.CategoryId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateEntity(Product product, UpdateProductDto updateDto)
    {
        product.Name = updateDto.Name;
        product.Price = updateDto.Price;
        product.CategoryId = updateDto.CategoryId;
        product.UpdatedAt = DateTime.UtcNow;
    }
}
