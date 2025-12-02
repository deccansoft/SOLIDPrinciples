// ========== REPOSITORY IMPLEMENTATIONS ==========

// SQL Server Implementation
public class SqlServerOrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public SqlServerOrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<Order>> GetByCustomerIdAsync(int customerId)
    {
        return await _context
            .Orders.Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<Order> SaveAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Orders.AnyAsync(o => o.Id == id);
    }
}

// MongoDB Implementation (can swap without changing OrderService!)
public class MongoOrderRepository : IOrderRepository
{
    private readonly IMongoCollection<Order> _orders;

    public MongoOrderRepository(IMongoDatabase database)
    {
        _orders = database.GetCollection<Order>("orders");
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _orders.Find(o => o.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Order>> GetByCustomerIdAsync(int customerId)
    {
        return await _orders
            .Find(o => o.CustomerId == customerId)
            .SortByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<Order> SaveAsync(Order order)
    {
        await _orders.InsertOneAsync(order);
        return order;
    }

    public async Task UpdateAsync(Order order)
    {
        await _orders.ReplaceOneAsync(o => o.Id == order.Id, order);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _orders.Find(o => o.Id == id).AnyAsync();
    }
}
