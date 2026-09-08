using Microsoft.Extensions.Logging.Abstractions;
using Moq;
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

    [Test]
    public void AdvancedSettingsViewModel_ChangesBarcodeModeThroughApplicationSettings()
    {
        var settings = new Mock<IApplicationSettings>();
        settings.SetupGet(value => value.IsBarcodeExtendedMode).Returns(false);
        var viewModel = new AdvancedSettingsViewModel(
            settings.Object,
            CreateSigningKeyViewModel(),
            Mock.Of<INavigationService>(),
            Mock.Of<IInventoryMaintenanceService>(),
            Mock.Of<IPopupService>());

        viewModel.IsBarcodeExtendedMode = true;

        settings.VerifySet(value => value.IsBarcodeExtendedMode = true, Times.Once);
    }

    [Test]
    public async Task SettingsViewModel_NavigateToAdvancedSettings_UsesAdvancedSettingsRoute()
    {
        var navigation = new Mock<INavigationService>();
        var viewModel = new SettingsViewModel(
            new AppearanceSettingsViewModel(Mock.Of<IApplicationSettings>()),
            CreateBackupViewModel(),
            navigation.Object);

        await viewModel.NavigateToAdvancedSettingsCommand.ExecuteAsync(null);

        navigation.Verify(value => value.GoToAsync(NavigationRoutes.AdvancedSettings), Times.Once);
    }

    private static BackupSettingsViewModel CreateBackupViewModel()
        => new(
            Mock.Of<IInventoryBackupWorkflowService>(),
            Mock.Of<INavigationService>(),
            Mock.Of<IFilePicker>(),
            Mock.Of<IPopupService>(),
            Mock.Of<IPopupDefinitionService>(),
            NullLogger<BackupSettingsViewModel>.Instance);

    private static BackupSigningKeySettingsViewModel CreateSigningKeyViewModel()
        => new(
            Mock.Of<IBackupSigningKeyTransferService>(),
            Mock.Of<IFilePicker>(),
            Mock.Of<IApplicationSettings>(),
            Mock.Of<IPopupService>(),
            Mock.Of<IPopupDefinitionService>(),
            NullLogger<BackupSigningKeySettingsViewModel>.Instance);
}
