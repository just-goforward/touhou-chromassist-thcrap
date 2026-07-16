using System.Collections.ObjectModel;
using Chromassist.Core.Imaging;
using Chromassist.Core.Models;
using Chromassist.Core.Presets;
using Chromassist.Core.Services;
using Chromassist.Presentation.Localization;

namespace Chromassist.Presentation;

public sealed class MainWindowViewModel : ObservableObject, IAsyncDisposable
{
    private readonly IGameLocator _gameLocator;
    private readonly IGameValidator _gameValidator;
    private readonly IResourceExtractor _resourceExtractor;
    private readonly IPatchBuilder _patchBuilder;
    private readonly IGameLauncher _gameLauncher;
    private readonly IPatchApplicationVerifier _patchVerifier;
    private readonly IExecutablePicker _executablePicker;
    private readonly IUserNotificationService _notifications;
    private readonly TextCatalog _texts;
    private GameItemViewModel? _selectedGame;
    private PresetKind _selectedVisionType = PresetKind.Protan;
    private double _strengthPercent = 50;
    private ColorPreset _currentPreset = PresetCatalog.Create(PresetKind.Protan, 50);
    private WizardStep _currentStep;
    private ExtractionResult? _extraction;
    private string? _preparedExecutable;
    private byte[]? _originalPreview;
    private byte[]? _adjustedPreview;
    private string _statusText;
    private bool _isBusy;
    private string? _generatedConfigurationName;
    private string? _generatedPatchDirectory;
    private PatchVerificationResult? _verificationResult;

    public MainWindowViewModel(
        IGameLocator gameLocator,
        IGameValidator gameValidator,
        IResourceExtractor resourceExtractor,
        IPatchBuilder patchBuilder,
        IGameLauncher gameLauncher,
        IPatchApplicationVerifier patchVerifier,
        IExecutablePicker executablePicker,
        IUserNotificationService notifications,
        TextCatalog? texts = null)
    {
        _gameLocator = gameLocator;
        _gameValidator = gameValidator;
        _resourceExtractor = resourceExtractor;
        _patchBuilder = patchBuilder;
        _gameLauncher = gameLauncher;
        _patchVerifier = patchVerifier;
        _executablePicker = executablePicker;
        _notifications = notifications;
        _texts = texts ?? new TextCatalog();
        _statusText = _texts["Ready"];

        ScanCommand = new AsyncRelayCommand(ScanAsync, () => !IsBusy);
        BrowseCommand = new AsyncRelayCommand(BrowseAsync, () => !IsBusy);
        BackCommand = new RelayCommand(GoBack, CanGoBack);
        NextCommand = new RelayCommand(GoNext, CanGoNext);
        ApplyAndLaunchCommand = new AsyncRelayCommand(ApplyAndLaunchAsync, CanApply);
    }

    public ObservableCollection<GameItemViewModel> Games { get; } = [];

    public IReadOnlyList<string> Languages { get; } = ["ko", "ja", "en"];

    public AsyncRelayCommand ScanCommand { get; }

    public AsyncRelayCommand BrowseCommand { get; }

    public RelayCommand BackCommand { get; }

    public RelayCommand NextCommand { get; }

    public AsyncRelayCommand ApplyAndLaunchCommand { get; }

    public GameItemViewModel? SelectedGame
    {
        get => _selectedGame;
        set
        {
            if (SetProperty(ref _selectedGame, value))
            {
                NotifyCommandStates();
                if (!IsBusy)
                {
                    _ = PrepareChangedSelectionAsync();
                }
            }
        }
    }

    public PresetKind SelectedVisionType
    {
        get => _selectedVisionType;
        set
        {
            if (value is PresetKind.Original || !SetProperty(ref _selectedVisionType, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsProtanSelected));
            OnPropertyChanged(nameof(IsDeutanSelected));
            OnPropertyChanged(nameof(IsTritanSelected));
            OnPropertyChanged(nameof(SelectedVisionTypeLabel));
            OnPropertyChanged(nameof(SelectedVisionTypeDescription));
            RefreshPresetAndPreview();
        }
    }

    public bool IsProtanSelected
    {
        get => SelectedVisionType == PresetKind.Protan;
        set { if (value) SelectedVisionType = PresetKind.Protan; }
    }

    public bool IsDeutanSelected
    {
        get => SelectedVisionType == PresetKind.Deutan;
        set { if (value) SelectedVisionType = PresetKind.Deutan; }
    }

    public bool IsTritanSelected
    {
        get => SelectedVisionType == PresetKind.Tritan;
        set { if (value) SelectedVisionType = PresetKind.Tritan; }
    }

    public double StrengthPercent
    {
        get => _strengthPercent;
        set
        {
            var rounded = Math.Round(Math.Clamp(value, 0, 100));
            if (SetProperty(ref _strengthPercent, rounded))
            {
                OnPropertyChanged(nameof(StrengthDisplay));
                RefreshPresetAndPreview();
            }
        }
    }

    public string SelectedLanguage
    {
        get => _texts.Language;
        set
        {
            if (!string.Equals(_texts.Language, value, StringComparison.OrdinalIgnoreCase))
            {
                _texts.Language = value;
                OnPropertyChanged();
                NotifyLocalizedProperties();
            }
        }
    }

    public byte[]? OriginalPreview
    {
        get => _originalPreview;
        private set => SetProperty(ref _originalPreview, value);
    }

    public byte[]? AdjustedPreview
    {
        get => _adjustedPreview;
        private set => SetProperty(ref _adjustedPreview, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                ScanCommand.NotifyCanExecuteChanged();
                BrowseCommand.NotifyCanExecuteChanged();
                NotifyCommandStates();
            }
        }
    }

    public string? GeneratedConfigurationName
    {
        get => _generatedConfigurationName;
        private set
        {
            if (SetProperty(ref _generatedConfigurationName, value))
            {
                OnPropertyChanged(nameof(DisplayedConfigurationName));
            }
        }
    }

    public string? GeneratedPatchDirectory
    {
        get => _generatedPatchDirectory;
        private set => SetProperty(ref _generatedPatchDirectory, value);
    }

    public PatchVerificationResult? VerificationResult
    {
        get => _verificationResult;
        private set
        {
            if (SetProperty(ref _verificationResult, value))
            {
                OnPropertyChanged(nameof(VerificationHeadline));
                OnPropertyChanged(nameof(VerificationDetails));
            }
        }
    }

    public bool IsGameStep => _currentStep == WizardStep.Game;
    public bool IsVisionStep => _currentStep == WizardStep.VisionType;
    public bool IsStrengthStep => _currentStep == WizardStep.StrengthPreview;
    public bool IsApplyStep => _currentStep == WizardStep.ApplyAndVerify;
    public bool IsNotApplyStep => _currentStep != WizardStep.ApplyAndVerify;
    public string StepProgressText => $"{(int)_currentStep + 1} / 4";
    public string AppTitle => _texts["AppTitle"];
    public string Subtitle => _texts["Subtitle"];
    public string CurrentStepTitle => _texts[$"Step{(int)_currentStep + 1}Title"];
    public string CurrentStepDescription => _texts[$"Step{(int)_currentStep + 1}Description"];
    public string RescanLabel => _texts["Rescan"];
    public string BrowseLabel => _texts["Browse"];
    public string BackLabel => _texts["Back"];
    public string NextLabel => _texts["Next"];
    public string ApplyLaunchLabel => _texts["ApplyLaunch"];
    public string OriginalLabel => _texts["Original"];
    public string AdjustedLabel => _texts["Adjusted"];
    public string ProtanLabel => _texts["Protan"];
    public string DeutanLabel => _texts["Deutan"];
    public string TritanLabel => _texts["Tritan"];
    public string ProtanDescription => _texts["ProtanDescription"];
    public string DeutanDescription => _texts["DeutanDescription"];
    public string TritanDescription => _texts["TritanDescription"];
    public string StrengthLabel => _texts["Strength"];
    public string StrengthLowLabel => _texts["StrengthLow"];
    public string StrengthHighLabel => _texts["StrengthHigh"];
    public string StrengthDisplay => $"{StrengthPercent:0}%";
    public string ExperimentalNotice => _texts["Experimental"];
    public string SelectedVisionTypeLabel => _texts[SelectedVisionType.ToString()];
    public string SelectedVisionTypeDescription => _texts[$"{SelectedVisionType}Description"];
    public string GameSummaryLabel => _texts["GameSummary"];
    public string VisionSummaryLabel => _texts["VisionSummary"];
    public string StrengthSummaryLabel => _texts["StrengthSummary"];
    public string ConfigSummaryLabel => _texts["ConfigSummary"];
    public string PendingConfigLabel => _texts["PendingConfig"];
    public string DisplayedConfigurationName => GeneratedConfigurationName ?? PendingConfigLabel;
    public string VerificationHeadline => VerificationResult is null
        ? _texts["VerificationPending"]
        : VerificationResult.AllExpectedFilesResolved
            ? _texts["VerificationRuntimeSuccess"]
            : VerificationResult.Success ? _texts["VerificationStackSuccess"] : _texts["VerificationFailed"];
    public string VerificationDetails => VerificationResult is null
        ? _texts["VerificationPendingDescription"]
        : string.Format(
            _texts["VerificationFiles"],
            VerificationResult.ResolvedFileCount,
            VerificationResult.ExpectedFileCount,
            VerificationResult.RunConfigurationLoaded ? _texts["Yes"] : _texts["No"],
            VerificationResult.PatchStackLoaded ? _texts["Yes"] : _texts["No"]);

    public Task InitializeAsync() => ScanAsync();

    public async ValueTask DisposeAsync()
    {
        if (_extraction is not null)
        {
            await _extraction.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task ScanAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            Games.Clear();
            await ResetExtractionAsync().ConfigureAwait(true);
            var installations = await _gameLocator.FindInstalledGamesAsync().ConfigureAwait(true);
            foreach (var installation in installations)
            {
                var item = new GameItemViewModel(installation);
                Games.Add(item);
                item.Validation = await _gameValidator.ValidateAsync(installation).ConfigureAwait(true);
            }

            SelectedGame = Games.FirstOrDefault(static game => game.IsSupported) ?? Games.FirstOrDefault();
            if (SelectedGame is null)
            {
                StatusText = _texts["NoGame"];
                return;
            }

            StatusText = SelectedGame.StatusText;
            await PreparePreviewAsync().ConfigureAwait(true);
        }).ConfigureAwait(true);
    }

    private async Task BrowseAsync()
    {
        var executable = _executablePicker.PickExecutable();
        if (string.IsNullOrWhiteSpace(executable))
        {
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            var installation = _gameLocator.FromExecutable(executable);
            if (installation is null)
            {
                StatusText = _texts["NoGame"];
                return;
            }

            var item = Games.FirstOrDefault(game =>
                string.Equals(game.Installation.ExecutablePath, installation.ExecutablePath, StringComparison.OrdinalIgnoreCase));
            if (item is null)
            {
                item = new GameItemViewModel(installation);
                Games.Add(item);
            }

            item.Validation = await _gameValidator.ValidateAsync(installation).ConfigureAwait(true);
            SelectedGame = item;
            StatusText = item.StatusText;
            await ResetExtractionAsync().ConfigureAwait(true);
            await PreparePreviewAsync().ConfigureAwait(true);
        }).ConfigureAwait(true);
    }

    private async Task PreparePreviewAsync()
    {
        if (SelectedGame?.Validation?.CanGeneratePatch != true)
        {
            NotifyCommandStates();
            return;
        }

        _extraction = await _resourceExtractor.ExtractBulletTexturesAsync(SelectedGame.Validation).ConfigureAwait(true);
        if (!_extraction.Success)
        {
            StatusText = string.Join(Environment.NewLine, _extraction.Diagnostics);
            return;
        }

        UpdatePreview();
        _preparedExecutable = SelectedGame.Installation.ExecutablePath;
        StatusText = string.Format(_texts["TexturesReady"], _extraction.Textures.Count);
        NotifyCommandStates();
    }

    private void RefreshPresetAndPreview()
    {
        _currentPreset = PresetCatalog.Create(SelectedVisionType, StrengthPercent);
        OnPropertyChanged(nameof(SelectedVisionTypeDescription));
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        var previewPath = _extraction?.Textures.FirstOrDefault()?.FilePath;
        if (previewPath is null || !File.Exists(previewPath))
        {
            OriginalPreview = null;
            AdjustedPreview = null;
            return;
        }

        var source = PngCodec.Read(previewPath);
        var adjusted = PresetTransformer.Transform(source, _currentPreset);
        OriginalPreview ??= Encode(source);
        AdjustedPreview = Encode(adjusted);
    }

    private void GoBack()
    {
        if (!CanGoBack())
        {
            return;
        }

        SetStep((WizardStep)((int)_currentStep - 1));
    }

    private void GoNext()
    {
        if (!CanGoNext())
        {
            return;
        }

        SetStep((WizardStep)((int)_currentStep + 1));
    }

    private void SetStep(WizardStep step)
    {
        _currentStep = step;
        foreach (var property in new[]
        {
            nameof(IsGameStep), nameof(IsVisionStep), nameof(IsStrengthStep), nameof(IsApplyStep), nameof(IsNotApplyStep),
            nameof(StepProgressText), nameof(CurrentStepTitle), nameof(CurrentStepDescription)
        })
        {
            OnPropertyChanged(property);
        }

        NotifyCommandStates();
    }

    private async Task ApplyAndLaunchAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            var validation = SelectedGame?.Validation;
            if (validation is null || _extraction is null)
            {
                return;
            }

            VerificationResult = null;
            StatusText = _texts["GeneratingPatch"];
            var result = await _patchBuilder.BuildAsync(validation, _extraction, _currentPreset).ConfigureAwait(true);
            if (!result.Success || result.RunConfigurationPath is null || result.PatchDirectory is null ||
                validation.Installation.ThcrapDirectory is null)
            {
                var message = string.Join(Environment.NewLine, result.Diagnostics.Prepend(result.Summary));
                StatusText = message;
                _notifications.ShowError(_texts["ErrorTitle"], message);
                return;
            }

            GeneratedConfigurationName = Path.GetFileName(result.RunConfigurationPath);
            GeneratedPatchDirectory = result.PatchDirectory;
            OnPropertyChanged(nameof(VerificationHeadline));
            StatusText = _texts["Launching"];
            var launch = await _gameLauncher.LaunchAsync(
                validation.Installation.ThcrapDirectory,
                result.RunConfigurationPath,
                validation.Installation.GameId).ConfigureAwait(true);

            StatusText = _texts["Verifying"];
            VerificationResult = await _patchVerifier.VerifyAsync(new PatchVerificationRequest(
                validation.Installation.ThcrapDirectory,
                result.RunConfigurationPath,
                result.PatchDirectory,
                _extraction.Textures.Select(static texture => texture.VirtualPath).ToArray(),
                launch.StartedAtUtc,
                TimeSpan.FromSeconds(20))).ConfigureAwait(true);
            StatusText = VerificationHeadline;
        }).ConfigureAwait(true);
    }

    private async Task ExecuteBusyAsync(Func<Task> action)
    {
        IsBusy = true;
        try
        {
            await action().ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            StatusText = exception.Message;
            _notifications.ShowError(_texts["ErrorTitle"], exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ResetExtractionAsync()
    {
        if (_extraction is not null)
        {
            await _extraction.DisposeAsync().ConfigureAwait(true);
            _extraction = null;
        }

        _preparedExecutable = null;
        OriginalPreview = null;
        AdjustedPreview = null;
        NotifyCommandStates();
    }

    private bool CanGoBack() => !IsBusy && _currentStep > WizardStep.Game;

    private bool CanGoNext() =>
        !IsBusy &&
        _currentStep < WizardStep.ApplyAndVerify &&
        (_currentStep != WizardStep.Game || IsPreparedForSelectedGame());

    private bool CanApply() => !IsBusy && _currentStep == WizardStep.ApplyAndVerify && IsPreparedForSelectedGame();

    private bool IsPreparedForSelectedGame() =>
        SelectedGame?.Validation?.CanGeneratePatch == true &&
        _extraction?.Success == true &&
        string.Equals(_preparedExecutable, SelectedGame.Installation.ExecutablePath, StringComparison.OrdinalIgnoreCase);

    private async Task PrepareChangedSelectionAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            await ResetExtractionAsync().ConfigureAwait(true);
            if (SelectedGame is not null)
            {
                StatusText = SelectedGame.StatusText;
                await PreparePreviewAsync().ConfigureAwait(true);
            }
        }).ConfigureAwait(true);
    }

    private void NotifyCommandStates()
    {
        BackCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();
        ApplyAndLaunchCommand.NotifyCanExecuteChanged();
    }

    private static byte[] Encode(RgbaImage image)
    {
        using var stream = new MemoryStream();
        PngCodec.Write(stream, image);
        return stream.ToArray();
    }

    private void NotifyLocalizedProperties()
    {
        foreach (var property in new[]
        {
            nameof(AppTitle), nameof(Subtitle), nameof(CurrentStepTitle), nameof(CurrentStepDescription),
            nameof(RescanLabel), nameof(BrowseLabel), nameof(BackLabel), nameof(NextLabel), nameof(ApplyLaunchLabel),
            nameof(OriginalLabel), nameof(AdjustedLabel), nameof(ProtanLabel), nameof(DeutanLabel), nameof(TritanLabel),
            nameof(ProtanDescription), nameof(DeutanDescription), nameof(TritanDescription), nameof(StrengthLabel),
            nameof(StrengthLowLabel), nameof(StrengthHighLabel), nameof(ExperimentalNotice), nameof(SelectedVisionTypeLabel),
            nameof(SelectedVisionTypeDescription), nameof(GameSummaryLabel), nameof(VisionSummaryLabel),
            nameof(StrengthSummaryLabel), nameof(ConfigSummaryLabel), nameof(PendingConfigLabel), nameof(DisplayedConfigurationName),
            nameof(VerificationHeadline), nameof(VerificationDetails)
        })
        {
            OnPropertyChanged(property);
        }
    }
}

public enum WizardStep
{
    Game,
    VisionType,
    StrengthPreview,
    ApplyAndVerify
}
