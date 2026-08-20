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
        var viewModel = CreateViewModel();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.IsZipBackupMode, Is.True);
            Assert.That(viewModel.IsJsonBackupMode, Is.False);
        });
    }

    private static SettingsViewModel CreateViewModel()
        => new(
            Mock.Of<IInventoryBackupWorkflowService>(),
            Mock.Of<IBackupSigningKeyTransferService>(),
            Mock.Of<IFilePicker>(),
            Mock.Of<INavigationService>(),
            Mock.Of<IApplicationSettings>(),
            Mock.Of<IPopupService>(),
            Mock.Of<IPopupDefinitionService>(),
            NullLogger<SettingsViewModel>.Instance);
}