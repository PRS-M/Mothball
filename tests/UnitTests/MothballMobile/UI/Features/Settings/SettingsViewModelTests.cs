using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Maui.Storage;
using Moq;
using MothballMobile.Infrastructure.Backup;
using MothballMobile.UI.Features.Settings;

namespace Mothball.Tests.Unit.Mobile.UI.Features.Settings;

[TestFixture]
public sealed class SettingsViewModelTests
{
    [Test]
    public void Constructor_DefaultsBackupModeToZipWithPhotos()
    {
        var viewModel = CreateBackupViewModel();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.IsZipBackupMode, Is.True);
            Assert.That(viewModel.IsJsonBackupMode, Is.False);
        });
    }

    [Test]
    public async Task ExportToJsonCommand_WhenExportThrows_ShowsFailureAlert_AndDoesNotRethrow()
    {
        var backupWorkflows = new Mock<IInventoryBackupWorkflowService>();
        backupWorkflows.Setup(w => w.ExportJsonAsync()).ThrowsAsync(new InvalidOperationException("disk full"));
        var popupDefinitions = new Mock<IPopupDefinitionService>();
        popupDefinitions.Setup(p => p.BackupExportFailed("disk full"))
            .Returns(new AlertPopupDefinition("Export failed", "disk full"));
        var popup = new Mock<IPopupService>();
        var viewModel = new BackupSettingsViewModel(
            backupWorkflows.Object,
            Mock.Of<INavigationService>(),
            Mock.Of<IFilePicker>(),
            popup.Object,
            popupDefinitions.Object,
            NullLogger<BackupSettingsViewModel>.Instance);

        await viewModel.ExportToJsonCommand.ExecuteAsync(null);

        Assert.That(viewModel.HasError, Is.False);
        popup.Verify(p => p.ShowAlertAsync(
            It.Is<AlertPopupDefinition>(d => d.Message == "disk full")), Times.Once);
    }

    private static BackupSettingsViewModel CreateBackupViewModel()
        => new(
            Mock.Of<IInventoryBackupWorkflowService>(),
            Mock.Of<INavigationService>(),
            Mock.Of<IFilePicker>(),
            Mock.Of<IPopupService>(),
            Mock.Of<IPopupDefinitionService>(),
            NullLogger<BackupSettingsViewModel>.Instance);
}