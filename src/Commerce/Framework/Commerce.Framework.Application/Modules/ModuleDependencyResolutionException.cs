namespace Commerce.Framework.Application.Modules;

public sealed class ModuleDependencyResolutionException : Exception
{
    public ModuleDependencyResolutionException(string message) : base(message)
    {
    }
}
