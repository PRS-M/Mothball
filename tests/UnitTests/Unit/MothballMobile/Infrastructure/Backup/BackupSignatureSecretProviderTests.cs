using Microsoft.Maui.Storage;
using MothballMobile.Infrastructure.Backup;

namespace Mothball.Tests.Unit.Mobile.Infrastructure.Backup;

[TestFixture]
public class BackupSignatureSecretProviderTests
{
    [Test]
    public async Task GetOrCreateAsync_WhenSecureStorageSetFails_DoesNotCacheUnsavedSecret()
    {
        var secureStorage = new FailingOnceSecureStorage();
        var provider = new BackupSignatureSecretProvider(secureStorage);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await provider.GetOrCreateAsync());
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Is.EqualTo("Secure storage write failed."));

        var secret = await provider.GetOrCreateAsync();

        Assert.That(secret, Is.Not.Empty);
        Assert.That(secureStorage.SetAttempts, Is.EqualTo(2));
        Assert.That(secureStorage.StoredValue, Is.EqualTo(secret));
    }

    private sealed class FailingOnceSecureStorage : ISecureStorage
    {
        private bool failNextSet = true;

        public int SetAttempts { get; private set; }

        public string? StoredValue { get; private set; }

        public Task<string?> GetAsync(string key)
            => Task.FromResult(StoredValue);

        public Task SetAsync(string key, string value)
        {
            SetAttempts++;
            if (failNextSet)
            {
                failNextSet = false;
                throw new InvalidOperationException("Secure storage write failed.");
            }

            StoredValue = value;
            return Task.CompletedTask;
        }
    }
}