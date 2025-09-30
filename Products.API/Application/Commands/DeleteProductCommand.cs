using MediatR;

namespace Products.API.Application.Commands;

/// <summary>
/// Command for deleting a product (soft delete)
/// Follows CQRS pattern with MediatR
/// </summary>
/// <param name="Id">Product unique identifier to delete</param>
public record DeleteProductCommand(Guid Id) : IRequest<bool>
{
    /// <summary>
    /// Reason for deletion (for audit purposes)
    /// </summary>
    public string? DeletionReason { get; init; }

    /// <summary>
    /// User or system performing the deletion
    /// </summary>
    public string? DeletedBy { get; init; }

    /// <summary>
    /// Whether to perform hard delete (completely remove from database)
    /// Default is false for soft delete
    /// </summary>
    public bool HardDelete { get; init; } = false;
}