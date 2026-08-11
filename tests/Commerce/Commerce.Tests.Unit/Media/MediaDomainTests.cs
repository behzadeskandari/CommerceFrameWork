using Commerce.Media.Domain.Services;
using Xunit;

namespace Commerce.Tests.Unit.Media;

public sealed class StorageKeyGeneratorTests
{
    [Fact]
    public void Create_GeneratesStoreScopedKey()
    {
        var key = StorageKeyGenerator.Create(1, "jpg", new DateTime(2026, 8, 11, 0, 0, 0, DateTimeKind.Utc));
        Assert.StartsWith("media/stores/1/2026/08/", key);
        Assert.EndsWith(".jpg", key);
    }

    [Fact]
    public void ValidateStorageKey_RejectsTraversal()
    {
        Assert.Throws<ArgumentException>(() => StorageKeyGenerator.ValidateStorageKey("../secret"));
    }
}

public sealed class FileSignatureValidatorTests
{
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public void IsSupportedImage_DetectsPng()
    {
        Assert.True(FileSignatureValidator.IsSupportedImage(PngHeader));
    }

    [Fact]
    public void IsExecutable_DetectsMzHeader()
    {
        Assert.True(FileSignatureValidator.IsExecutable("MZ"u8.ToArray()));
    }
}

public sealed class FileNameSanitizerTests
{
    [Fact]
    public void SanitizeOriginalFileName_RemovesPathSegments()
    {
        var sanitized = FileNameSanitizer.SanitizeOriginalFileName(@"..\..\etc\passwd");
        Assert.DoesNotContain("..", sanitized);
        Assert.Equal("passwd", sanitized);
    }
}
