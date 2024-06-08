namespace MyDi;

interface IMainService
{
    void DoSomething();
}

interface ISubService
{
    void DoSomething();
}

class MainService : IMainService
{
    private readonly ISubService _subService;

    public MainService(ISubService subService)
    {
        _subService = subService;
    }

    public void DoSomething()
    {
        Console.WriteLine("MainService is doing something");
        _subService.DoSomething();
    }
}

class SubService : ISubService
{
    public void DoSomething()
    {
        Console.WriteLine("SubService is doing something");
    }
}