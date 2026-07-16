using System.IO;
using System.Windows;
using Chromassist.Core.Games.Th18;
using Chromassist.Core.Thcrap;
using Chromassist.Core.Tools;
using Chromassist.Presentation;

namespace Chromassist.App;

public partial class App : Application
{
    private MainWindowViewModel? _viewModel;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var localData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ThChromassist");
        var tools = Path.Combine(AppContext.BaseDirectory, "tools", "thtk");
        var locator = new Th18GameLocator();
        var validator = new Th18GameValidator(new ThcrapInspector());
        var extractor = new ThtkResourceExtractor(tools, Path.Combine(localData, "work"));
        _viewModel = new MainWindowViewModel(
            locator,
            validator,
            extractor,
            new LocalPatchBuilder(),
            new ThcrapSetupLauncher(),
            new ExecutablePicker(),
            new ContextImagePicker(),
            new UserNotificationService());

        var window = new MainWindow(_viewModel);
        MainWindow = window;
        window.Show();
        await _viewModel.InitializeAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        base.OnExit(e);
    }
}
