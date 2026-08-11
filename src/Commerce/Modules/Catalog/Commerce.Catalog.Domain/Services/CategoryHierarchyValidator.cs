namespace Commerce.Catalog.Domain.Services;

public static class CategoryHierarchyValidator
{
    public static bool WouldCreateCycle(
        int categoryId,
        int? newParentCategoryId,
        Func<int, int?> getParentId)
    {
        if (newParentCategoryId is null)
        {
            return false;
        }

        if (newParentCategoryId.Value == categoryId)
        {
            return true;
        }

        var currentId = newParentCategoryId;
        var visited = new HashSet<int> { categoryId };

        while (currentId.HasValue)
        {
            if (!visited.Add(currentId.Value))
            {
                return true;
            }

            if (currentId.Value == categoryId)
            {
                return true;
            }

            currentId = getParentId(currentId.Value);
        }

        return false;
    }
}
