using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using MothballMobile.Infrastructure;

namespace UnitTests;

public class RetryServiceTests
{
    [Test]
    public async Task RetryAsync_ReturnsTrue_WhenFirstAttemptSucceeds()
    {
        var popup = new Mock<IPopupService>(MockBehavior.Strict);
        var service = new RetryService(popup.Object);
        async Task<bool> Attempt() => await Task.FromResult(true);

        var result = await service.RetryAsync(Attempt, "t","m","r","c");

        result.Should().BeTrue();
        popup.VerifyNoOtherCalls();
    }

    [Test]
    public async Task RetryAsync_RetriesAndSucceeds_WhenUserChoosesRetry()
    {
        var popup = new Mock<IPopupService>(MockBehavior.Strict);
        popup.Setup(p => p.ConfirmAsync("t","m","r","c")).ReturnsAsync(true);

        var service = new RetryService(popup.Object);
        var attempts = 0;
        async Task<bool> Attempt() => await Task.FromResult(++attempts >= 2);

        var result = await service.RetryAsync(Attempt, canceledTitle: "t", canceledMessage: "m", retryButton: "r", continueButton: "c");

        result.Should().BeTrue();
        attempts.Should().Be(2);
        popup.Verify(p => p.ConfirmAsync("t","m","r","c"), Times.Once);
        popup.VerifyNoOtherCalls();
    }

    [Test]
    public async Task RetryAsync_ShowsContinueAlert_WhenUserCancels()
    {
        var popup = new Mock<IPopupService>(MockBehavior.Strict);
        popup.Setup(p => p.ConfirmAsync("t","m","r","c")).ReturnsAsync(false);
        popup.Setup(p => p.ShowAlertAsync("no","cont", It.IsAny<string>())).Returns(Task.CompletedTask);

        var service = new RetryService(popup.Object);
        var attempts = 0;
        async Task<bool> Attempt() => await Task.FromResult(++attempts >= 2); // always false on first

        var result = await service.RetryAsync(Attempt, canceledTitle: "t", canceledMessage: "m", retryButton: "r", continueButton: "c", continueAlertTitle: "no", continueAlertMessage: "cont");

        result.Should().BeFalse();
        attempts.Should().Be(1);
        popup.Verify(p => p.ConfirmAsync("t","m","r","c"), Times.Once);
        popup.Verify(p => p.ShowAlertAsync("no","cont", It.IsAny<string>()), Times.Once);
        popup.VerifyNoOtherCalls();
    }
}
