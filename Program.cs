using MyDi;

var diContainer = new SimpleContainer();

diContainer.Register<ISubService, SubService>(Lifetime.Singleton);
diContainer.Register<IMainService, MainService>(Lifetime.Transient);

var mainService = diContainer.Resolve<IMainService>();

mainService.DoSomething();