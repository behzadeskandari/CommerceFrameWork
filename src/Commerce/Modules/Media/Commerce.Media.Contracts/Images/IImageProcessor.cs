namespace Commerce.Media.Contracts.Images;

public sealed record ImageDimensions(int Width, int Height);

public interface IImageProcessor
{
    Task<ImageDimensions?> GetDimensionsAsync(Stream content, CancellationToken cancellationToken = default);

    Task<Stream?> GenerateThumbnailAsync(
        Stream content,
        int maxWidth,
        int maxHeight,
        CancellationToken cancellationToken = default);
}
