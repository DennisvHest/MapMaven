using MapMaven.Utility;

namespace MapMaven;

public partial class App : Application
{
    private readonly HostedServiceExecutor _hostedServiceExecutor;

    public App(HostedServiceExecutor hostedServiceExecutor)
    {
        _hostedServiceExecutor = hostedServiceExecutor;

        InitializeComponent();

        MainPage = new MainPage();
    }

    protected override void OnStart()
    {
        Task.Run(() => _hostedServiceExecutor.StartAsync(new()));
        base.OnStart();
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = base.CreateWindow(activationState);
        if (window != null)
        {
            window.Title = "Map Maven";
        }

        return window;
    }
}
