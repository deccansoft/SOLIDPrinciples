[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    // Controller depends on abstraction too!
    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<ActionResult<OrderResult>> PlaceOrder(PlaceOrderRequest request)
    {
        var result = await _orderService.PlaceOrderAsync(request);

        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetOrder), new { id = result.OrderId }, result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrder(int id)
    {
        var order = await _orderService.GetOrderAsync(id);

        if (order == null)
            return NotFound();

        return Ok(order);
    }

    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<IEnumerable<Order>>> GetCustomerOrders(int customerId)
    {
        var orders = await _orderService.GetCustomerOrdersAsync(customerId);
        return Ok(orders);
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<OrderResult>> CancelOrder(int id)
    {
        var result = await _orderService.CancelOrderAsync(id);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{id}/refund")]
    public async Task<ActionResult<OrderResult>> RefundOrder(int id)
    {
        var result = await _orderService.RefundOrderAsync(id);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
