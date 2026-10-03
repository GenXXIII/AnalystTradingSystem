namespace XauAi.Infrastructure;

/// <summary>
/// Provides a stable way for tests and the API composition root to locate this assembly.
/// </summary>
public static class AssemblyReference
{
    public static readonly System.Reflection.Assembly Assembly = typeof(AssemblyReference).Assembly;
}
