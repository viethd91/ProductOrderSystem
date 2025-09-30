using Microsoft.EntityFrameworkCore;
using Orders.API.Domain.Entities;
using Orders.API.Domain.Enums;
using Orders.API.Domain.Interfaces;
using Orders.API.Infrastructure.Data;

namespace Orders.API.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderContext _context;
    public OrderRepository(OrderContext context) => _context = context;

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<List<Order>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.Orders.Include(o => o.OrderItems).ToListAsync(cancellationToken);

    public Task<List<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        _context.Orders.Include(o => o.OrderItems)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<List<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default) =>
        _context.Orders.Include(o => o.OrderItems)
            .Where(o => o.Status == status)
            .ToListAsync(cancellationToken);

    public Task<List<Order>> GetPendingOrdersByProductIdAsync(Guid productId, CancellationToken cancellationToken = default) =>
        _context.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.Status == OrderStatus.Pending && o.OrderItems.Any(i => i.ProductId == productId))
            .ToListAsync(cancellationToken);

    public Task<bool> IsOrderNumberTakenAsync(string orderNumber, CancellationToken cancellationToken = default) =>
        _context.Orders.AnyAsync(o => o.OrderNumber == orderNumber, cancellationToken);

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default) =>
        await _context.Orders.AddAsync(order, cancellationToken);

    public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _context.Orders.Update(order);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}