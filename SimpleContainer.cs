using System.Reflection;

namespace SimpleDi;

/// <summary>
/// Service lifetime
/// </summary>
public enum Lifetime
{
    /// Transient services are created each time they are requested
    Transient,
    /// Singleton services are created once and shared
    Singleton
}

/// <summary>
/// Parameter type for service configuration
/// </summary>
public record Configurator<T>(T Target, SimpleContainer Container);

/// <summary>
///  Simple dependency injection container
/// </summary>
public class SimpleContainer
{
    /// Mapping service interfaces to implementations and lifetimes
    private readonly Dictionary<Type, (Type, Lifetime)> _typeMapping = new Dictionary<Type, (Type, Lifetime)>();
    /// Singleton instances
    private readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();
    /// Service configurations
    private readonly Dictionary<Type, object?> _configurations = new Dictionary<Type, object?>();

    /// <summary
    /// Register a service
    /// </summary>
    /// <typeparam name="TInterface">Service interface</typeparam>
    /// <typeparam name="TImplementation">Service implementation</typeparam>
    /// <param name="configure">Optional configuration action</param>
    /// <param name="lifetime">Service lifetime</param>
    public void Register<TInterface, TImplementation>(
        Action<Configurator<TImplementation>>? configure = null,
        Lifetime lifetime = Lifetime.Transient)
    {
        _typeMapping[typeof(TInterface)] = (typeof(TImplementation), lifetime);
        _configurations[typeof(TImplementation)] = configure;
    }

    /// <summary>
    /// Resolve a service
    /// </summary>
    /// <typeparam name="TInterface">Service interface type</typeparam>
    /// <returns>Service instance</returns>
    public TInterface Resolve<TInterface>()
    {
        return (TInterface)Resolve(typeof(TInterface));
    }

    /// <summary>
    /// Resolve a service
    /// </summary>
    /// <param name="interfaceType">Service interface type</param>
    /// <returns>Service instance</returns>
    private object Resolve(Type interfaceType)
    {
        if (_instances.ContainsKey(interfaceType))
        {
            return _instances[interfaceType];
        }

        if (!_typeMapping.ContainsKey(interfaceType))
        {
            throw new InvalidOperationException("No registration for " + interfaceType);
        }

        var (implementationType, lifetime) = _typeMapping[interfaceType];
        var constructor = implementationType.GetConstructors().First();
        var parameters = constructor.GetParameters();
        var parameterInstances = parameters.Select(parameter => Resolve(parameter.ParameterType)).ToArray();

        var implementationInstance = Activator.CreateInstance(implementationType, parameterInstances);

        if (implementationInstance == null)
        {
            throw new InvalidOperationException("Could not create instance of " + implementationType);
        }

        // Property Injection
        var properties = implementationType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(prop => prop.IsDefined(typeof(InjectAttribute), false));

        foreach (var property in properties)
        {
            var propertyValue = Resolve(property.PropertyType);
            property.SetValue(implementationInstance, propertyValue);
        }

        if (lifetime == Lifetime.Singleton)
        {
            _instances[interfaceType] = implementationInstance;
        }

        if (_configurations.TryGetValue(implementationType, out var configure) &&
            configure != null)
        {
            var configuratorType = typeof(Configurator<>).MakeGenericType(implementationType);
            var configurator = Activator.CreateInstance(configuratorType, implementationInstance, this);
            InvokeAction(configure, configurator!);
        }

        return implementationInstance;
    }

    static void InvokeAction(object actionObj, object parameter)
    {
        // Get the action's type (should be something like Action<T>)
        var actionType = actionObj.GetType();

        // Ensure it's actually an Action<T>
        if (actionType.IsGenericType && actionType.GetGenericTypeDefinition() == typeof(Action<>))
        {
            // Get the type of T
            var argumentType = actionType.GenericTypeArguments[0];

            // Check if the provided parameter matches T
            if (parameter.GetType() == argumentType)
            {
                // Create a delegate of the correct type and invoke it
                var action = Delegate.CreateDelegate(actionType, actionObj, "Invoke");
                action.DynamicInvoke(parameter);
            }
            else
            {
                throw new InvalidOperationException("Parameter type does not match the Action type parameter");
            }
        }
        else
        {
            throw new InvalidOperationException("Provided object is not an Action<T>");
        }
    }
}
