
using MediatR;
using Products.API.Application.DTOs;
using Products.API.Application.Extensions;
using Products.API.Application.Queries;
using Products.API.Domain.Interfaces;

namespace Products.API.Application.Handlers;

// Add IMapper as a constructor parameter and assign to a private readonly field
public class GetLowStockProductsQueryHandler(
    IProductRepository repository) : IRequestHandler<GetLowStockProductsQuery, List<ProductDto>>
{

    public async Task<List<ProductDto>> Handle(GetLowStockProductsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var products = await repository.GetLowStockProductsAsync(request.Threshold, cancellationToken);

        if (!request.IncludeDeleted)
        {
            products = products.Where(p => !p.IsDeleted).ToList();
        }

        if (!request.IncludeOutOfStock)
        {
            products = products.Where(p => p.Stock > 0).ToList();
        }

        // Use the private _mapper field instead of mapper
        var productDtos = products.Select(p => p.ToDto()).ToList();

        return productDtos;
    }
}