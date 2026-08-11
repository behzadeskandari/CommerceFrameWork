namespace Commerce.Media.Domain.Services;

public static class FileSignatureValidator
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47];
    private static readonly byte[] Gif87 = "GIF87a"u8.ToArray();
    private static readonly byte[] Gif89 = "GIF89a"u8.ToArray();
    private static readonly byte[] WebP = "RIFF"u8.ToArray();
    private static readonly byte[] WebPMarker = "WEBP"u8.ToArray();
    private static readonly byte[] Pdf = "%PDF"u8.ToArray();

    public static bool IsSupportedImage(ReadOnlySpan<byte> content) =>
        IsJpeg(content) || IsPng(content) || IsGif(content) || IsWebP(content);

    public static bool IsSupportedDocument(ReadOnlySpan<byte> content) =>
        IsPdf(content);

    public static bool IsExecutable(ReadOnlySpan<byte> content)
    {
        if (content.Length >= 2 && content[0] == 'M' && content[1] == 'Z')
        {
            return true;
        }

        if (content.Length >= 4 && content[0] == 0x7F && content[1] == (byte)'E' && content[2] == (byte)'L' && content[3] == (byte)'F')
        {
            return true;
        }

        return false;
    }

    public static string? DetectImageContentType(ReadOnlySpan<byte> content)
    {
        if (IsJpeg(content))
        {
            return "image/jpeg";
        }

        if (IsPng(content))
        {
            return "image/png";
        }

        if (IsGif(content))
        {
            return "image/gif";
        }

        if (IsWebP(content))
        {
            return "image/webp";
        }

        return null;
    }

    private static bool IsJpeg(ReadOnlySpan<byte> content) =>
        content.Length >= Jpeg.Length && content[..Jpeg.Length].SequenceEqual(Jpeg);

    private static bool IsPng(ReadOnlySpan<byte> content) =>
        content.Length >= Png.Length && content[..Png.Length].SequenceEqual(Png);

    private static bool IsGif(ReadOnlySpan<byte> content) =>
        (content.Length >= Gif87.Length && content[..Gif87.Length].SequenceEqual(Gif87)) ||
        (content.Length >= Gif89.Length && content[..Gif89.Length].SequenceEqual(Gif89));

    private static bool IsWebP(ReadOnlySpan<byte> content) =>
        content.Length >= 12 &&
        content[..WebP.Length].SequenceEqual(WebP) &&
        content[8..12].SequenceEqual(WebPMarker);

    private static bool IsPdf(ReadOnlySpan<byte> content) =>
        content.Length >= Pdf.Length && content[..Pdf.Length].SequenceEqual(Pdf);
}
