using Commerce.Framework.Data.Db;
using Commerce.Notifications.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Notifications.Infrastructure.Persistence;

public sealed class NotificationsModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new NotificationTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationLogConfiguration());
        modelBuilder.ApplyConfiguration(new InAppNotificationConfiguration());
    }
}
