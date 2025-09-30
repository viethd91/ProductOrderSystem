using System;
using System.Collections.Generic;
using Orders.API.Application.DTOs;

namespace Orders.API.Application.Extensions
{
    public static class CreateOrderItemDtoExtensions
    {
        /// <summary>
        /// Validates the CreateOrderItemDto and returns a list of validation error messages.
        /// </summary>
        /// <param name="item">The CreateOrderItemDto to validate.</param>
        /// <returns>List of validation error messages, empty if valid.</returns>
        public static List<string> GetValidationErrors(this CreateOrderItemDto? item)
        {
            var errors = new List<string>();

            if (item == null)
            {
                errors.Add("Order item is required.");
                return errors;
            }

            if (item.ProductId == Guid.Empty)
                errors.Add("Product ID is required.");

            if (string.IsNullOrWhiteSpace(item.ProductName))
                errors.Add("Product name is required.");

            if (item.ProductName != null && item.ProductName.Length > 200)
                errors.Add("Product name cannot exceed 200 characters.");

            if (item.Quantity <= 0)
                errors.Add("Quantity must be greater than zero.");

            if (item.UnitPrice < 0)
                errors.Add("Unit price cannot be negative.");

            if (item.TotalPrice != item.Quantity * item.UnitPrice)
                errors.Add("Total price does not match quantity * unit price.");

            return errors;
        }
    }
}
