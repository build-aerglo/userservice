using Microsoft.Extensions.Configuration;
using Moq;
using UserService.Application.Services;

namespace UserService.Application.Tests.Services;

[TestFixture]
public class EncryptionServiceTests
{
    private EncryptionService _encryptionService = null!;
    private const string TestKey = "TestSecure32CharacterKeyHere123";

    [SetUp]
    public void Setup()
    {
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Encryption:Key"]).Returns(TestKey);

        _encryptionService = new EncryptionService(mockConfig.Object);
    }

    [Test]
    public void Encrypt_ShouldReturnBase64String()
    {
        // Arrange
        var plainText = "TestPassword123!";

        // Act
        var encrypted = _encryptionService.Encrypt(plainText);

        // Assert
        Assert.That(encrypted, Is.Not.Null);
        Assert.That(encrypted, Is.Not.Empty);
        Assert.DoesNotThrow(() => Convert.FromBase64String(encrypted));
    }

    [Test]
    public void Decrypt_ShouldReturnOriginalText()
    {
        // Arrange
        var plainText = "TestPassword123!";
        var encrypted = _encryptionService.Encrypt(plainText);

        // Act
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        Assert.That(decrypted, Is.EqualTo(plainText));
    }

    [Test]
    public void Encrypt_ShouldProduceDifferentOutputsForSameInput()
    {
        // Arrange - Due to random IV, same input should produce different encrypted values
        var plainText = "TestPassword123!";

        // Act
        var encrypted1 = _encryptionService.Encrypt(plainText);
        var encrypted2 = _encryptionService.Encrypt(plainText);

        // Assert
        Assert.That(encrypted1, Is.Not.EqualTo(encrypted2));
    }

    [Test]
    public void Decrypt_ShouldThrow_WhenInvalidBase64()
    {
        // Arrange
        var invalidEncrypted = "not-valid-base64!!!";

        // Act & Assert
        Assert.Throws<FormatException>(() => _encryptionService.Decrypt(invalidEncrypted));
    }

    [Test]
    public void Decrypt_ShouldThrow_WhenDataTooShort()
    {
        // Arrange - Valid base64 but too short for IV extraction
        var shortData = Convert.ToBase64String(new byte[8]);

        // Act & Assert - Throws OverflowException when data is too short to extract IV
        Assert.Throws<OverflowException>(() => _encryptionService.Decrypt(shortData));
    }

    [Test]
    public void EncryptDecrypt_ShouldHandleSpecialCharacters()
    {
        // Arrange
        var plainText = "P@ssw0rd!#$%^&*()_+-=[]{}|;':\",./<>?`~";

        // Act
        var encrypted = _encryptionService.Encrypt(plainText);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        Assert.That(decrypted, Is.EqualTo(plainText));
    }

    [Test]
    public void EncryptDecrypt_ShouldHandleUnicodeCharacters()
    {
        // Arrange
        var plainText = "パスワード密码كلمة المرور";

        // Act
        var encrypted = _encryptionService.Encrypt(plainText);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        Assert.That(decrypted, Is.EqualTo(plainText));
    }

    [Test]
    public void EncryptDecrypt_ShouldHandleLongText()
    {
        // Arrange
        var plainText = new string('a', 10000);

        // Act
        var encrypted = _encryptionService.Encrypt(plainText);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        Assert.That(decrypted, Is.EqualTo(plainText));
    }

    [Test]
    public void EncryptDecrypt_ShouldHandleEmptyString()
    {
        // Arrange
        var plainText = "";

        // Act
        var encrypted = _encryptionService.Encrypt(plainText);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        Assert.That(decrypted, Is.EqualTo(plainText));
    }

    [Test]
    public void Constructor_ShouldThrow_WhenKeyNotConfigured()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Encryption:Key"]).Returns((string?)null);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new EncryptionService(mockConfig.Object));
    }
}
