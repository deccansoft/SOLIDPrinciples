// ===== READ-ONLY SERVICES =====

// Reporting service - ONLY needs read access
public class ProductReportService
{
    private readonly IQueryRepository<Product> _repository; // ✅ Only read methods available

    public ProductReportService(IQueryRepository<Product> repository)
    {
        _repository = repository;
    }

    public async Task<SalesReport> GenerateSalesReportAsync(DateTime from, DateTime to)
    {
        var products = await _repository.FindAsync(p => p.CreatedAt >= from && p.CreatedAt <= to);

        return new SalesReport
        {
            TotalProducts = products.Count(),
            TotalValue = products.Sum(p => p.Price * p.StockQuantity),
            ByCategory = products
                .GroupBy(p => p.Category?.Name ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count()),
        };
    }

    public async Task<PaginatedResult<ProductSummaryDto>> GetProductListAsync(
        int page,
        int pageSize
    )
    {
        var result = await _repository.GetPagedAsync(
            page,
            pageSize,
            orderBy: p => p.Name,
            ascending: true
        );

        return new PaginatedResult<ProductSummaryDto>
        {
            Items = result.Items.Select(p => new ProductSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Category = p.Category?.Name,
            }),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
        };
    }

    // ❌ Cannot call _repository.DeleteAsync() - method doesn't exist on IQueryRepository!
    // ❌ Cannot call _repository.UpdateAsync() - method doesn't exist on IQueryRepository!
}

// Search service - uses specification pattern for complex queries
public class ProductSearchService
{
    private readonly ISpecificationRepository<Product> _repository;

    public ProductSearchService(ISpecificationRepository<Product> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Product>> SearchAsync(ProductSearchCriteria criteria)
    {
        var spec = new ProductSearchSpecification(criteria);
        return await _repository.FindAsync(spec);
    }
}

// Custom specification
public class ProductSearchSpecification : Specification<Product>
{
    public ProductSearchSpecification(ProductSearchCriteria criteria)
    {
        // Build dynamic criteria
        Expression<Func<Product, bool>> filter = p => true;

        if (!string.IsNullOrEmpty(criteria.SearchTerm))
        {
            var term = criteria.SearchTerm.ToLower();
            filter = p => p.Name.ToLower().Contains(term) || p.Description.ToLower().Contains(term);
        }

        Criteria = filter;

        if (criteria.CategoryId.HasValue)
        {
            var categoryFilter = Criteria;
            Criteria = p =>
                categoryFilter.Compile()(p) && p.CategoryId == criteria.CategoryId.Value;
        }

        if (criteria.MinPrice.HasValue)
        {
            Criteria = CombineWithAnd(Criteria, p => p.Price >= criteria.MinPrice.Value);
        }

        if (criteria.MaxPrice.HasValue)
        {
            Criteria = CombineWithAnd(Criteria, p => p.Price <= criteria.MaxPrice.Value);
        }

        // Include related data
        AddInclude(p => p.Category!);

        // Ordering
        if (criteria.SortBy == "price")
            OrderBy = p => p.Price;
        else
            OrderBy = p => p.Name;

        // Pagination
        if (criteria.Page.HasValue && criteria.PageSize.HasValue)
        {
            Skip = (criteria.Page.Value - 1) * criteria.PageSize.Value;
            Take = criteria.PageSize.Value;
        }
    }

    private Expression<Func<Product, bool>> CombineWithAnd(
        Expression<Func<Product, bool>> left,
        Expression<Func<Product, bool>> right
    )
    {
        var param = Expression.Parameter(typeof(Product), "p");
        var combined = Expression.AndAlso(
            Expression.Invoke(left, param),
            Expression.Invoke(right, param)
        );
        return Expression.Lambda<Func<Product, bool>>(combined, param);
    }
}

public class ProductSearchCriteria
{
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? SortBy { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}
