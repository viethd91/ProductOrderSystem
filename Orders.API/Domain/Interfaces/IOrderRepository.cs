using Orders.API.Domain.Entities;
using Orders.API.Domain.Enums;

namespace Orders.API.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Order>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<List<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);
    Task<List<Order>> GetPendingOrdersByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<bool> IsOrderNumberTakenAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}