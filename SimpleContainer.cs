using System.Reflection;

namespace MyDi;

public enum Lifetime
{
    Transient,
    Singleton
}

public class SimpleContainer
{
    private readonly Dictionary<Type, (Type, Lifetime)> _typeMapping = new Dictionary<Type, (Type, Lifetime)>();
    private readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();

    public void Register<TInterface, TImplementation>(Lifetime lifetime = Lifetime.Transient)
    {
        _typeMapping[typeof(TInterface)] = (typeof(TImplementation), lifetime);
    }

    public TInterface Resolve<TInterface>()
    {
        return (TInterface)Resolve(typeof(TInterface));
    }

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

        return implementationInstance;
    }
}
