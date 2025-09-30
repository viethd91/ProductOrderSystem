using FluentAssertions;
using Moq;
using Products.API.Application.Commands;
using Products.API.Application.DTOs;
using Products.API.Application.Handlers;
using Products.API.Application.Interfaces;
using Products.API.Domain.Entities;
using Products.API.Domain.Interfaces;
using Shared.IntegrationEvents;

namespace Products.UnitTests.Handlers;

/// <summary>
/// Unit tests for CreateProductCommandHandler
/// </summary>
public class CreateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _mockRepository;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _mockRepository = new Mock<IProductRepository>();
        _mockEventPublisher = new Mock<IEventPublisher>();

        _handler = new CreateProductCommandHandler(
            _mockRepository.Object,
            _mockEventPublisher.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesProduct()
    {
        var command = new CreateProductCommand("Test Product", 19.99m, 100);
        var cancellationToken = CancellationToken.None;

        _mockRepository
            .Setup(r => r.IsNameTakenAsync(command.Name, null, cancellationToken))
            .ReturnsAsync(false);

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), cancellationToken))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(cancellationToken))
            .ReturnsAsync(1);

        _mockEventPublisher
            .Setup(p => p.PublishAsync(It.IsAny<ProductCreatedIntegrationEvent>(), cancellationToken))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, cancellationToken);

        result.Should().NotBeNull();
        result.Name.Should().Be(command.Name);
        result.Price.Should().Be(command.Price);
        result.Stock.Should().Be(command.Stock);
        result.Id.Should().NotBe(Guid.Empty);

        _mockRepository.Verify(
            r => r.IsNameTakenAsync(command.Name, null, cancellationToken),
            Times.Once);
        _mockRepository.Verify(
            r => r.AddAsync(It.Is<Product>(p =>
                p.Name == command.Name &&
                p.Price == command.Price &&
                p.Stock == command.Stock), cancellationToken),
            Times.Once);
        _mockRepository.Verify(
            r => r.SaveChangesAsync(cancellationToken),
            Times.Once);
        _mockEventPublisher.Verify(
            p => p.PublishAsync(It.IsAny<ProductCreatedIntegrationEvent>(), cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesIntegrationEvent()
    {
        var command = new CreateProductCommand("Test Product", 19.99m, 100);
        var cancellationToken = CancellationToken.None;
        ProductCreatedIntegrationEvent capturedEvent = null!;

        _mockRepository
            .Setup(r => r.IsNameTakenAsync(command.Name, null, cancellationToken))
            .ReturnsAsync(false);
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), cancellationToken))
            .Returns(Task.CompletedTask);
        _mockRepository
            .Setup(r => r.SaveChangesAsync(cancellationToken))
            .ReturnsAsync(1);

        _mockEventPublisher
            .Setup(p => p.PublishAsync(It.IsAny<ProductCreatedIntegrationEvent>(), cancellationToken))
            .Callback<ProductCreatedIntegrationEvent, CancellationToken>((evt, _) => capturedEvent = evt)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, cancellationToken);
        Assert.NotNull(result);
        result.Should().NotBeNull();

        capturedEvent.Should().NotBeNull();
        capturedEvent.ProductId.Should().NotBe(Guid.Empty);
        capturedEvent.ProductName.Should().Be(command.Name);
        capturedEvent.Price.Should().Be(command.Price);
        capturedEvent.Stock.Should().Be(command.Stock);
    }

    [Fact]
    public async Task Handle_DuplicateProductName_ThrowsInvalidOperationException()
    {
        var command = new CreateProductCommand("Existing Product", 19.99m, 100);
        var cancellationToken = CancellationToken.None;

        _mockRepository
            .Setup(r => r.IsNameTakenAsync(command.Name, null, cancellationToken))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, cancellationToken));

        exception.Message.Should().Contain("already exists");

        _mockRepository.Verify(
            r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockRepository.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        _mockEventPublisher.Verify(
            p => p.PublishAsync(It.IsAny<ProductCreatedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_NullCommand_ThrowsArgumentNullException()
    {
        CreateProductCommand command = null!;
        var cancellationToken = CancellationToken.None;

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _handler.Handle(command, cancellationToken));

        _mockRepository.VerifyNoOtherCalls();
        _mockEventPublisher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_RepositoryThrowsException_DoesNotPublishEvent()
    {
        var command = new CreateProductCommand("Test Product", 19.99m, 100);
        var cancellationToken = CancellationToken.None;

        _mockRepository
            .Setup(r => r.IsNameTakenAsync(command.Name, null, cancellationToken))
            .ReturnsAsync(false);
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), cancellationToken))
            .Returns(Task.CompletedTask);
        _mockRepository
            .Setup(r => r.SaveChangesAsync(cancellationToken))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, cancellationToken));

        exception.Message.Should().Be("Database error");

        _mockEventPublisher.Verify(
            p => p.PublishAsync(It.IsAny<ProductCreatedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesProductWithCorrectProperties()
    {
        var command = new CreateProductCommand("Premium Widget", 49.99m, 50);
        var cancellationToken = CancellationToken.None;
        Product capturedProduct = null!;

        _mockRepository
            .Setup(r => r.IsNameTakenAsync(command.Name, null, cancellationToken))
            .ReturnsAsync(false);
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), cancellationToken))
            .Callback<Product, CancellationToken>((p, _) => capturedProduct = p)
            .Returns(Task.CompletedTask);
        _mockRepository
            .Setup(r => r.SaveChangesAsync(cancellationToken))
            .ReturnsAsync(1);

        var result = await _handler.Handle(command, cancellationToken);

        capturedProduct.Should().NotBeNull();
        capturedProduct.Name.Should().Be(command.Name);
        capturedProduct.Price.Should().Be(command.Price);
        capturedProduct.Stock.Should().Be(command.Stock);
        capturedProduct.Id.Should().NotBe(Guid.Empty);
        capturedProduct.IsDeleted.Should().BeFalse();

        result.Name.Should().Be(command.Name);
        result.Price.Should().Be(command.Price);
        result.Stock.Should().Be(command.Stock);
    }

    [Theory]
    [InlineData("Product A", 10.00, 5)]
    [InlineData("Product B", 99.99, 0)]
    [InlineData("Product C", 0.01, 1000)]
    public async Task Handle_VariousValidInputs_CreatesProductSuccessfully(string name, decimal price, int stock)
    {
        var command = new CreateProductCommand(name, price, stock);
        var cancellationToken = CancellationToken.None;

        _mockRepository
            .Setup(r => r.IsNameTakenAsync(command.Name, null, cancellationToken))
            .ReturnsAsync(false);
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), cancellationToken))
            .Returns(Task.CompletedTask);
        _mockRepository
            .Setup(r => r.SaveChangesAsync(cancellationToken))
            .ReturnsAsync(1);

        var result = await _handler.Handle(command, cancellationToken);

        result.Name.Should().Be(name);
        result.Price.Should().Be(price);
        result.Stock.Should().Be(stock);

        _mockRepository.Verify(
            r => r.AddAsync(It.Is<Product>(p =>
                p.Name == name &&
                p.Price == price &&
                p.Stock == stock), cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesEventWithCorrectData()
    {
        var command = new CreateProductCommand("Event Test Product", 25.50m, 75);
        var cancellationToken = CancellationToken.None;
        ProductCreatedIntegrationEvent capturedEvent = null!;

        _mockRepository
            .Setup(r => r.IsNameTakenAsync(command.Name, null, cancellationToken))
            .ReturnsAsync(false);
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), cancellationToken))
            .Returns(Task.CompletedTask);
        _mockRepository
            .Setup(r => r.SaveChangesAsync(cancellationToken))
            .ReturnsAsync(1);

        _mockEventPublisher
            .Setup(p => p.PublishAsync(It.IsAny<ProductCreatedIntegrationEvent>(), cancellationToken))
            .Callback<ProductCreatedIntegrationEvent, CancellationToken>((evt, _) => capturedEvent = evt)
            .Returns(Task.CompletedTask);

        await _handler.Handle(command, cancellationToken);

        capturedEvent.Should().NotBeNull();
        capturedEvent.ProductId.Should().NotBe(Guid.Empty);
        capturedEvent.ProductName.Should().Be(command.Name);
        capturedEvent.Price.Should().Be(command.Price);
    }
}