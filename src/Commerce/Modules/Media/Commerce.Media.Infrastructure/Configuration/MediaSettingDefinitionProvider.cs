using Commerce.Framework.Contracts.Configuration;
using Commerce.Framework.Contracts.Security;
using Commerce.Media.Application;

namespace Commerce.Media.Infrastructure.Configuration;

public sealed class MediaSettingDefinitionProvider : ISettingDefinitionProvider
{
    public IReadOnlyList<SettingDefinition> GetDefinitions() =>
    [
        new(MediaSettingKeys.MaxUploadSize, "Maximum upload size in bytes.", SettingValueType.Integer, "10485760", "Commerce.Media"),
        new(MediaSettingKeys.MaxImageSize, "Maximum image upload size in bytes.", SettingValueType.Integer, "5242880", "Commerce.Media"),
        new(MediaSettingKeys.AllowedImageTypes, "Allowed image extensions (comma-separated).", SettingValueType.String, "jpg,jpeg,png,gif,webp", "Commerce.Media"),
        new(MediaSettingKeys.AllowedDocumentTypes, "Allowed document extensions (comma-separated).", SettingValueType.String, "pdf", "Commerce.Media"),
        new(MediaSettingKeys.ThumbnailMaxWidth, "Generated thumbnail max width in pixels.", SettingValueType.Integer, "320", "Commerce.Media"),
        new(MediaSettingKeys.ThumbnailMaxHeight, "Generated thumbnail max height in pixels.", SettingValueType.Integer, "320", "Commerce.Media")
    ];
}
