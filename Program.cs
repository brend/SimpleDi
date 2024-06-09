using MyDi;

// build the dependency injection container
var diContainer = new SimpleContainer();
diContainer.Register<IMainService, MainService>(lifetime: Lifetime.Transient);
diContainer.Register<ISubService, SubService>(
    configurator => configurator.Target.Message = "Dieser Dienst wurde mit Liebe konfiguriert",
    Lifetime.Singleton);

// instantiate the main service and run it
var mainService = diContainer.Resolve<IMainService>();
mainService.DoSomething();