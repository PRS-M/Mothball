using CoreApp.Application.Contracts;
using CoreApp.Application.Features.Barcodes.Commands;
using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Domain.ValueObjects;
using Moq;

namespace Mothball.Tests.Unit.Core.Features.Barcodes;

[TestFixture]
public sealed class BarcodeAssignmentServiceTests
{
    [Test]
    public async Task UpdateContainerAsync_WhenBarcodeIsAvailable_AssignsAndPersistsIt()
    {
        var container = new Container(Guid.NewGuid(), "Archive box", "");
        var commands = new Mock<IInventoryCommandRepository>();
        var service = new BarcodeAssignmentService(commands.Object, Mock.Of<IInventoryQueryRepository>());
        var barcode = new Barcode("container-01", BarcodeSymbology.Code128);

        await service.UpdateContainerAsync(container, barcode);

        Assert.That(container.Barcode, Is.EqualTo(barcode));
        commands.Verify(repository => repository.UpdateContainerAsync(container), Times.Once);
    }

    [Test]
    public void UpdateItemAsync_WhenBarcodeBelongsToAnotherOwner_RejectsAssignment()
    {
        var item = new Item(Guid.NewGuid(), "Tape", "");
        var barcode = new Barcode("1234567890123", BarcodeSymbology.Ean13);
        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(repository => repository.FindBarcodeAsync(barcode.Value))
            .ReturnsAsync(new BarcodeLookupResult(BarcodeOwnerKind.Container, Guid.NewGuid(), "Archive box"));
        var commands = new Mock<IInventoryCommandRepository>();
        var service = new BarcodeAssignmentService(commands.Object, queries.Object);

        var action = () => service.UpdateItemAsync(item, barcode);

        Assert.That(action, Throws.TypeOf<BarcodeAlreadyAssignedException>().With.Message.Contains("Archive box"));
        Assert.That(item.Barcode, Is.Null);
        commands.Verify(repository => repository.UpdateItemAsync(It.IsAny<Item>()), Times.Never);
    }

    [Test]
    public async Task UpdateItemAsync_WhenBarcodeIsCleared_PersistsNullAssignment()
    {
        var item = new Item(Guid.NewGuid(), "Tape", "");
        item.UpdateBarcode(new Barcode("tape-01", BarcodeSymbology.Code39));
        var commands = new Mock<IInventoryCommandRepository>();
        var service = new BarcodeAssignmentService(commands.Object, Mock.Of<IInventoryQueryRepository>());

        await service.UpdateItemAsync(item, null);

        Assert.That(item.Barcode, Is.Null);
        commands.Verify(repository => repository.UpdateItemAsync(item), Times.Once);
    }
}