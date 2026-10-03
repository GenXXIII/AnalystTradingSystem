namespace XauAi.Application;

/// <summary>
/// Provides a stable way for tests and composition roots to locate this assembly.
/// </summary>
public static class AssemblyReference
{
    public static readonly System.Reflection.Assembly Assembly = typeof(AssemblyReference).Assembly;
}
