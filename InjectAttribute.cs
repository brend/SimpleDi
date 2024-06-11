namespace SimpleDi;

/// <summary>
/// Attribute to mark properties for injection
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class InjectAttribute : Attribute { }