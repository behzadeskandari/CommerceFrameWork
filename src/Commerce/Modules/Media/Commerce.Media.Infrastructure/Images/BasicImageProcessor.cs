using Commerce.Media.Contracts.Images;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace Commerce.Media.Infrastructure.Images;

public sealed class BasicImageProcessor : IImageProcessor
{
    public async Task<ImageDimensions?> GetDimensionsAsync(Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        var imageInfo = await Image.IdentifyAsync(content, cancellationToken).ConfigureAwait(false);
        return imageInfo is null ? null : new ImageDimensions(imageInfo.Width, imageInfo.Height);
    }

    public async Task<Stream?> GenerateThumbnailAsync(
        Stream content,
        int maxWidth,
        int maxHeight,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        content.Position = 0;

        using var image = await Image.LoadAsync(content, cancellationToken).ConfigureAwait(false);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(maxWidth, maxHeight)
        }));

        var output = new MemoryStream();
        await image.SaveAsync(output, new JpegEncoder { Quality = 85 }, cancellationToken).ConfigureAwait(false);
        output.Position = 0;
        return output;
    }
}
