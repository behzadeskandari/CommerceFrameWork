namespace Commerce.Media.Infrastructure.Storage;

public sealed class MediaStorageOptions
{
    public const string SectionName = "Commerce:Media";

    public string StorageRoot { get; set; } = "App_Data/media";
}
