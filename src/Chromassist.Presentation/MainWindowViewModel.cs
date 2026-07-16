using System.Collections.ObjectModel;
using Chromassist.Core.Games.Th18;
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
    private readonly IThcrapSetupLauncher _setupLauncher;
    private readonly IExecutablePicker _executablePicker;
    private readonly IContextImagePicker _contextImagePicker;
    private readonly IUserNotificationService _notifications;
    private readonly TextCatalog _texts;
    private GameItemViewModel? _selectedGame;
    private ContextImageItemViewModel? _selectedContextImage;
    private PresetKind _selectedVisionType = PresetKind.Protan;
    private double _strengthPercent = 50;
    private ColorPreset _currentPreset = PresetCatalog.Create(PresetKind.Protan, 50);
    private WizardStep _currentStep;
    private ExtractionResult? _extraction;
    private string? _preparedExecutable;
    private byte[]? _originalPreview;
    private byte[]? _adjustedPreview;
    private RgbaImage? _previewSource;
    private string _statusText;
    private bool _isBusy;
    private string? _generatedPatchSelectionName;
    private string? _generatedPatchDirectory;
    private bool? _preparationVerified;
    private int _preparedFileCount;
    private int _expectedFileCount;
    private bool _alphaPreserved;
    private bool _transparentPixelsPreserved;
    private bool _neutralPixelsPreserved;
    private bool _repositoryMetadataPresent;
    private int _changedOpaquePixelCount;
    private int _opaquePixelCount;

    public MainWindowViewModel(
        IGameLocator gameLocator,
        IGameValidator gameValidator,
        IResourceExtractor resourceExtractor,
        IPatchBuilder patchBuilder,
        IThcrapSetupLauncher setupLauncher,
        IExecutablePicker executablePicker,
        IContextImagePicker contextImagePicker,
        IUserNotificationService notifications,
        TextCatalog? texts = null)
    {
        _gameLocator = gameLocator;
        _gameValidator = gameValidator;
        _resourceExtractor = resourceExtractor;
        _patchBuilder = patchBuilder;
        _setupLauncher = setupLauncher;
        _executablePicker = executablePicker;
        _contextImagePicker = contextImagePicker;
        _notifications = notifications;
        _texts = texts ?? new TextCatalog();
        _statusText = _texts["Ready"];

        ScanCommand = new AsyncRelayCommand(ScanAsync, () => !IsBusy);
        BrowseCommand = new AsyncRelayCommand(BrowseAsync, () => !IsBusy);
        AddContextImagesCommand = new AsyncRelayCommand(AddContextImagesAsync, () => !IsBusy);
        RemoveContextImageCommand = new RelayCommand(RemoveSelectedContextImage, CanRemoveContextImage);
        UseNeutralContextCommand = new RelayCommand(UseNeutralContext, CanUseNeutralContext);
        BackCommand = new RelayCommand(GoBack, CanGoBack);
        NextCommand = new RelayCommand(GoNext, CanGoNext);
        PrepareAndOpenSetupCommand = new AsyncRelayCommand(PrepareAndOpenSetupAsync, CanApply);
    }

    public ObservableCollection<GameItemViewModel> Games { get; } = [];

    public ObservableCollection<ContextImageItemViewModel> ContextImages { get; } = [];

    public IReadOnlyList<string> Languages { get; } = ["ko", "ja", "en"];

    public AsyncRelayCommand ScanCommand { get; }

    public AsyncRelayCommand BrowseCommand { get; }

    public AsyncRelayCommand AddContextImagesCommand { get; }

    public RelayCommand RemoveContextImageCommand { get; }

    public RelayCommand UseNeutralContextCommand { get; }

    public RelayCommand BackCommand { get; }

    public RelayCommand NextCommand { get; }

    public AsyncRelayCommand PrepareAndOpenSetupCommand { get; }

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
            OnPropertyChanged(nameof(WhyAdjustmentText));
            RefreshPresetAndPreview();
        }
    }

    public ContextImageItemViewModel? SelectedContextImage
    {
        get => _selectedContextImage;
        set
        {
            if (SetProperty(ref _selectedContextImage, value))
            {
                OnPropertyChanged(nameof(ContextModeDescription));
                RemoveContextImageCommand.NotifyCanExecuteChanged();
                UseNeutralContextCommand.NotifyCanExecuteChanged();
                UpdatePreview();
            }
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
            var clamped = Math.Clamp(value, 0, 100);
            if (SetProperty(ref _strengthPercent, clamped))
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
                AddContextImagesCommand.NotifyCanExecuteChanged();
                RemoveContextImageCommand.NotifyCanExecuteChanged();
                UseNeutralContextCommand.NotifyCanExecuteChanged();
                NotifyCommandStates();
            }
        }
    }

    public string? GeneratedPatchSelectionName
    {
        get => _generatedPatchSelectionName;
        private set
        {
            if (SetProperty(ref _generatedPatchSelectionName, value))
            {
                OnPropertyChanged(nameof(DisplayedPatchSelectionName));
            }
        }
    }

    public string? GeneratedPatchDirectory
    {
        get => _generatedPatchDirectory;
        private set => SetProperty(ref _generatedPatchDirectory, value);
    }

    public bool IsGameStep => _currentStep == WizardStep.Game;
    public bool IsVisionStep => _currentStep == WizardStep.VisionType;
    public bool IsStrengthStep => _currentStep == WizardStep.StrengthPreview;
    public bool IsApplyStep => _currentStep == WizardStep.PrepareAndConfigure;
    public bool IsNotApplyStep => _currentStep != WizardStep.PrepareAndConfigure;
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
    public string AddContextImagesLabel => _texts["AddContextImages"];
    public string RemoveContextImageLabel => _texts["RemoveContextImage"];
    public string UseNeutralContextLabel => _texts["UseNeutralContext"];
    public string ContextPrivacyNotice => _texts["ContextPrivacyNotice"];
    public string ContextModeDescription => SelectedContextImage is null
        ? _texts["NeutralPreviewDescription"]
        : string.Format(_texts["StageContextPreviewDescription"], SelectedContextImage.DisplayName);
    public string WhyAdjustmentText => _texts[$"Why{SelectedVisionType}"];
    public string StrengthDisplay => $"{StrengthPercent:0.0}%";
    public string ExperimentalNotice => _texts["Experimental"];
    public string SelectedVisionTypeLabel => _texts[SelectedVisionType.ToString()];
    public string SelectedVisionTypeDescription => _texts[$"{SelectedVisionType}Description"];
    public string GameSummaryLabel => _texts["GameSummary"];
    public string VisionSummaryLabel => _texts["VisionSummary"];
    public string StrengthSummaryLabel => _texts["StrengthSummary"];
    public string ConfigSummaryLabel => _texts["ConfigSummary"];
    public string PendingConfigLabel => _texts["PendingConfig"];
    public string SetupInstructions => _texts["SetupInstructions"];
    public string DisplayedPatchSelectionName => GeneratedPatchSelectionName ?? $"chromassist/th18-{_currentPreset.Id}";
    public string VerificationHeadline => _preparationVerified is null
        ? _texts["VerificationPending"]
        : _preparationVerified.Value ? _texts["VerificationPreparationSuccess"] : _texts["VerificationFailed"];
    public string VerificationDetails => _preparationVerified is null
        ? _texts["VerificationPendingDescription"]
        : string.Format(
            _texts["VerificationPreparationDetails"],
            _preparedFileCount,
            _expectedFileCount,
            _alphaPreserved ? _texts["Yes"] : _texts["No"],
            _transparentPixelsPreserved ? _texts["Yes"] : _texts["No"],
            _repositoryMetadataPresent ? _texts["Yes"] : _texts["No"],
            _neutralPixelsPreserved ? _texts["Yes"] : _texts["No"],
            _changedOpaquePixelCount,
            _opaquePixelCount);

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
        GeneratedPatchSelectionName = null;
        GeneratedPatchDirectory = null;
        SetPreparationVerification(null, 0, _extraction?.Textures.Count ?? 0, false, false, false, false, 0, 0);
        OnPropertyChanged(nameof(DisplayedPatchSelectionName));
        OnPropertyChanged(nameof(SelectedVisionTypeDescription));
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        var previewPath = _extraction?.Textures.FirstOrDefault()?.FilePath;
        if (_previewSource is null && previewPath is not null && File.Exists(previewPath))
        {
            _previewSource = PngCodec.Read(previewPath);
        }

        if (_previewSource is null)
        {
            OriginalPreview = null;
            AdjustedPreview = null;
            return;
        }

        var adjusted = PresetTransformer.Transform(_previewSource, _currentPreset);
        if (SelectedContextImage is null)
        {
            OriginalPreview = Encode(_previewSource);
            AdjustedPreview = Encode(adjusted);
            return;
        }

        var regions = Th18EnemyProjectilePreviewLayout.RepresentativeRegions;
        var originalContext = ContextPreviewComposer.Compose(SelectedContextImage.Image, _previewSource, regions);
        var adjustedContext = ContextPreviewComposer.Compose(SelectedContextImage.Image, adjusted, regions);
        OriginalPreview = Encode(originalContext.Image);
        AdjustedPreview = Encode(adjustedContext.Image);
    }

    private Task AddContextImagesAsync() => ExecuteBusyAsync(() =>
    {
        var selections = _contextImagePicker.PickImages(_texts["ContextPickerTitle"]);
        ContextImageItemViewModel? firstAdded = null;
        foreach (var selection in selections)
        {
            var item = new ContextImageItemViewModel(selection);
            ContextImages.Add(item);
            firstAdded ??= item;
        }

        if (firstAdded is not null)
        {
            SelectedContextImage = firstAdded;
        }

        return Task.CompletedTask;
    });

    private void RemoveSelectedContextImage()
    {
        if (SelectedContextImage is null)
        {
            return;
        }

        var index = ContextImages.IndexOf(SelectedContextImage);
        ContextImages.Remove(SelectedContextImage);
        SelectedContextImage = ContextImages.Count == 0
            ? null
            : ContextImages[Math.Clamp(index, 0, ContextImages.Count - 1)];
    }

    private bool CanRemoveContextImage() => !IsBusy && SelectedContextImage is not null;

    private void UseNeutralContext() => SelectedContextImage = null;

    private bool CanUseNeutralContext() => !IsBusy && SelectedContextImage is not null;

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

    private async Task PrepareAndOpenSetupAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            var validation = SelectedGame?.Validation;
            if (validation is null || _extraction is null)
            {
                return;
            }

            SetPreparationVerification(null, 0, _extraction.Textures.Count, false, false, false, false, 0, 0);
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

            GeneratedPatchSelectionName = $"chromassist/{Path.GetFileName(result.PatchDirectory)}";
            GeneratedPatchDirectory = result.PatchDirectory;
            var repositoryMetadata = Path.Combine(Path.GetDirectoryName(result.PatchDirectory)!, "repo.js");
            var prepared = result.Files.Count == _extraction.Textures.Count &&
                result.Files.All(static file => file.AlphaPreserved && file.TransparentPixelsPreserved && file.NeutralPixelsPreserved) &&
                File.Exists(repositoryMetadata);
            SetPreparationVerification(
                prepared,
                result.Files.Count,
                _extraction.Textures.Count,
                result.Files.All(static file => file.AlphaPreserved),
                result.Files.All(static file => file.TransparentPixelsPreserved),
                result.Files.All(static file => file.NeutralPixelsPreserved),
                File.Exists(repositoryMetadata),
                result.Files.Sum(static file => file.ChangedOpaquePixelCount),
                result.Files.Sum(static file => file.OpaquePixelCount));
            if (!prepared)
            {
                StatusText = _texts["VerificationFailed"];
                _notifications.ShowError(_texts["ErrorTitle"], VerificationDetails);
                return;
            }

            StatusText = _texts["LaunchingSetup"];
            await _setupLauncher.LaunchAsync(validation.Installation.ThcrapDirectory).ConfigureAwait(true);
            StatusText = _texts["SetupOpened"];
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
        _previewSource = null;
        OriginalPreview = null;
        AdjustedPreview = null;
        NotifyCommandStates();
    }

    private bool CanGoBack() => !IsBusy && _currentStep > WizardStep.Game;

    private bool CanGoNext() =>
        !IsBusy &&
        _currentStep < WizardStep.PrepareAndConfigure &&
        (_currentStep != WizardStep.Game || IsPreparedForSelectedGame());

    private bool CanApply() => !IsBusy && _currentStep == WizardStep.PrepareAndConfigure && IsPreparedForSelectedGame();

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
        PrepareAndOpenSetupCommand.NotifyCanExecuteChanged();
    }

    private void SetPreparationVerification(
        bool? success,
        int preparedFileCount,
        int expectedFileCount,
        bool alphaPreserved,
        bool transparentPixelsPreserved,
        bool neutralPixelsPreserved,
        bool repositoryMetadataPresent,
        int changedOpaquePixelCount,
        int opaquePixelCount)
    {
        _preparationVerified = success;
        _preparedFileCount = preparedFileCount;
        _expectedFileCount = expectedFileCount;
        _alphaPreserved = alphaPreserved;
        _transparentPixelsPreserved = transparentPixelsPreserved;
        _neutralPixelsPreserved = neutralPixelsPreserved;
        _repositoryMetadataPresent = repositoryMetadataPresent;
        _changedOpaquePixelCount = changedOpaquePixelCount;
        _opaquePixelCount = opaquePixelCount;
        OnPropertyChanged(nameof(VerificationHeadline));
        OnPropertyChanged(nameof(VerificationDetails));
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
            nameof(StrengthLowLabel), nameof(StrengthHighLabel), nameof(AddContextImagesLabel), nameof(RemoveContextImageLabel),
            nameof(UseNeutralContextLabel),
            nameof(ContextPrivacyNotice), nameof(ContextModeDescription), nameof(WhyAdjustmentText), nameof(ExperimentalNotice), nameof(SelectedVisionTypeLabel),
            nameof(SelectedVisionTypeDescription), nameof(GameSummaryLabel), nameof(VisionSummaryLabel),
            nameof(StrengthSummaryLabel), nameof(ConfigSummaryLabel), nameof(PendingConfigLabel), nameof(DisplayedPatchSelectionName),
            nameof(SetupInstructions), nameof(VerificationHeadline), nameof(VerificationDetails)
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
    PrepareAndConfigure
}
