using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Orders.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Orders",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "dbo",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Orders",
                columns: new[] { "Id", "CreatedAt", "CustomerId", "CustomerName", "OrderDate", "OrderNumber", "TotalAmount", "UpdatedAt" },
                values: new object[] { new Guid("660e8400-e29b-41d4-a716-446655440001"), new DateTime(2024, 1, 15, 10, 30, 0, 0, DateTimeKind.Utc), new Guid("770e8400-e29b-41d4-a716-446655440001"), "John Doe", new DateTime(2024, 1, 15, 10, 30, 0, 0, DateTimeKind.Utc), "ORD-20240115103000", 1029.98m, new DateTime(2024, 1, 15, 10, 30, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Orders",
                columns: new[] { "Id", "CreatedAt", "CustomerId", "CustomerName", "OrderDate", "OrderNumber", "Status", "TotalAmount", "UpdatedAt" },
                values: new object[] { new Guid("660e8400-e29b-41d4-a716-446655440002"), new DateTime(2024, 1, 15, 11, 0, 0, 0, DateTimeKind.Utc), new Guid("770e8400-e29b-41d4-a716-446655440002"), "Jane Smith", new DateTime(2024, 1, 15, 11, 0, 0, 0, DateTimeKind.Utc), "ORD-20240115110000", 1, 39.98m, new DateTime(2024, 1, 15, 11, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "OrderItems",
                columns: new[] { "Id", "OrderId", "ProductId", "ProductName", "Quantity", "UnitPrice" },
                values: new object[,]
                {
                    { new Guid("880e8400-e29b-41d4-a716-446655440001"), new Guid("660e8400-e29b-41d4-a716-446655440001"), new Guid("550e8400-e29b-41d4-a716-446655440001"), "Sample Laptop", 1, 999.99m },
                    { new Guid("880e8400-e29b-41d4-a716-446655440002"), new Guid("660e8400-e29b-41d4-a716-446655440001"), new Guid("550e8400-e29b-41d4-a716-446655440002"), "Wireless Mouse", 1, 29.99m },
                    { new Guid("880e8400-e29b-41d4-a716-446655440003"), new Guid("660e8400-e29b-41d4-a716-446655440002"), new Guid("550e8400-e29b-41d4-a716-446655440002"), "Wireless Mouse", 1, 29.99m },
                    { new Guid("880e8400-e29b-41d4-a716-446655440004"), new Guid("660e8400-e29b-41d4-a716-446655440002"), new Guid("550e8400-e29b-41d4-a716-446655440003"), "USB Cable", 1, 9.99m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                schema: "dbo",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId_ProductId_Unique",
                schema: "dbo",
                table: "OrderItems",
                columns: new[] { "OrderId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductId",
                schema: "dbo",
                table: "OrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                schema: "dbo",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId_Status",
                schema: "dbo",
                table: "Orders",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderDate",
                schema: "dbo",
                table: "Orders",
                column: "OrderDate");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber_Unique",
                schema: "dbo",
                table: "Orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                schema: "dbo",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_OrderDate",
                schema: "dbo",
                table: "Orders",
                columns: new[] { "Status", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TotalAmount",
                schema: "dbo",
                table: "Orders",
                column: "TotalAmount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItems",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Orders",
                schema: "dbo");
        }
    }
}
