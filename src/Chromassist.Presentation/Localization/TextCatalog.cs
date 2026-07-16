using System.Globalization;

namespace Chromassist.Presentation.Localization;

public sealed class TextCatalog
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Values =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["ko"] = Create(
                ("AppTitle", "TH Chromassist"),
                ("Subtitle", "동방홍룡동 탄막 색상 접근성 설정 마법사"),
                ("Step1Title", "게임 설치 확인"),
                ("Step1Description", "TH18과 thcrap 한국어 패치의 위치와 버전을 확인합니다."),
                ("Step2Title", "색각 유형 선택"),
                ("Step2Description", "가장 가까운 색각 특성을 하나 선택하세요. 이는 의학적 진단이 아닙니다."),
                ("Step3Title", "보정 강도와 미리보기"),
                ("Step3Description", "슬라이더를 움직이면 로컬 탄막 미리보기가 즉시 변경됩니다."),
                ("Step4Title", "적용 및 실행 확인"),
                ("Step4Description", "선택 내용을 확인하고 thcrap으로 게임을 실행합니다."),
                ("Back", "이전"), ("Next", "다음"), ("Rescan", "다시 찾기"), ("Browse", "실행 파일 직접 지정"),
                ("ApplyLaunch", "패치 적용 후 게임 실행"), ("Original", "원본"), ("Adjusted", "변경 후"),
                ("Protan", "Protan 계열"), ("Deutan", "Deutan 계열"), ("Tritan", "Tritan 계열"),
                ("ProtanDescription", "적색 계열 구분에 어려움이 있는 경우를 위한 실험 설정"),
                ("DeutanDescription", "녹색 계열 구분에 어려움이 있는 경우를 위한 실험 설정"),
                ("TritanDescription", "청색·황색 계열 구분에 어려움이 있는 경우를 위한 실험 설정"),
                ("Strength", "보정 강도"), ("StrengthLow", "변화 없음"), ("StrengthHigh", "강한 변화"),
                ("Experimental", "현재 변환량은 사용자 연구로 검증되지 않은 실험값입니다."),
                ("GameSummary", "게임"), ("VisionSummary", "색각 유형"), ("StrengthSummary", "강도"),
                ("ConfigSummary", "적용할 thcrap 설정"), ("PendingConfig", "실행 시 생성됩니다"),
                ("Ready", "게임을 찾고 있습니다…"),
                ("NoGame", "지원되는 TH18 설치본을 찾지 못했습니다. 실행 파일을 직접 지정하세요."),
                ("TexturesReady", "게임과 thcrap 검증 완료 · texture {0}개 준비됨"),
                ("GeneratingPatch", "선택한 강도로 로컬 패치를 생성하고 있습니다…"),
                ("Launching", "생성된 설정으로 thcrap loader를 실행하고 있습니다…"),
                ("Verifying", "thcrap 로그에서 패치 적용 여부를 자동 확인하고 있습니다…"),
                ("VerificationPending", "아직 실행하지 않았습니다"),
                ("VerificationPendingDescription", "게임 실행 후 run configuration과 6개 texture의 로드 여부를 표시합니다."),
                ("VerificationStackSuccess", "Chromassist 패치 선택 및 활성화 확인"),
                ("VerificationRuntimeSuccess", "Chromassist texture 6/6 적용 확인"),
                ("VerificationFailed", "패치 적용을 확인하지 못했습니다"),
                ("VerificationFiles", "run configuration: {2} · patch stack: {3} · texture 로드: {0}/{1}"),
                ("Yes", "예"), ("No", "아니요"),
                ("ErrorTitle", "TH Chromassist 오류"), ("InfoTitle", "TH Chromassist")),
            ["ja"] = Create(
                ("AppTitle", "TH Chromassist"),
                ("Subtitle", "東方虹龍洞 弾幕色アクセシビリティ設定ウィザード"),
                ("Step1Title", "ゲームの確認"),
                ("Step1Description", "TH18とthcrap日本語／翻訳パッチ環境の場所とバージョンを確認します。"),
                ("Step2Title", "色覚タイプの選択"),
                ("Step2Description", "最も近い色覚特性を一つ選択してください。医学的診断ではありません。"),
                ("Step3Title", "補正強度とプレビュー"),
                ("Step3Description", "スライダーを動かすとローカル弾幕プレビューが即時に変わります。"),
                ("Step4Title", "適用と起動確認"),
                ("Step4Description", "選択内容を確認し、thcrapでゲームを起動します。"),
                ("Back", "戻る"), ("Next", "次へ"), ("Rescan", "再検索"), ("Browse", "実行ファイルを指定"),
                ("ApplyLaunch", "パッチを適用してゲームを起動"), ("Original", "元画像"), ("Adjusted", "変更後"),
                ("Protan", "Protan系"), ("Deutan", "Deutan系"), ("Tritan", "Tritan系"),
                ("ProtanDescription", "赤系の識別が難しい場合の実験設定"),
                ("DeutanDescription", "緑系の識別が難しい場合の実験設定"),
                ("TritanDescription", "青・黄系の識別が難しい場合の実験設定"),
                ("Strength", "補正強度"), ("StrengthLow", "変化なし"), ("StrengthHigh", "強い変化"),
                ("Experimental", "現在の変換量はユーザー調査で検証されていない実験値です。"),
                ("GameSummary", "ゲーム"), ("VisionSummary", "色覚タイプ"), ("StrengthSummary", "強度"),
                ("ConfigSummary", "適用するthcrap設定"), ("PendingConfig", "起動時に生成されます"),
                ("Ready", "ゲームを検索しています…"),
                ("NoGame", "対応するTH18を検出できませんでした。実行ファイルを指定してください。"),
                ("TexturesReady", "ゲームとthcrapの確認完了 · texture {0}個を準備しました"),
                ("GeneratingPatch", "選択した強度でローカルパッチを生成しています…"),
                ("Launching", "生成した設定でthcrap loaderを起動しています…"),
                ("Verifying", "thcrapログでパッチ適用を自動確認しています…"),
                ("VerificationPending", "まだ起動していません"),
                ("VerificationPendingDescription", "起動後にrun configurationと6個のtextureのロード状況を表示します。"),
                ("VerificationStackSuccess", "Chromassistパッチの選択と有効化を確認しました"),
                ("VerificationRuntimeSuccess", "Chromassist texture 6/6の適用を確認しました"),
                ("VerificationFailed", "パッチの適用を確認できませんでした"),
                ("VerificationFiles", "run configuration: {2} · patch stack: {3} · textureロード: {0}/{1}"),
                ("Yes", "はい"), ("No", "いいえ"),
                ("ErrorTitle", "TH Chromassist エラー"), ("InfoTitle", "TH Chromassist")),
            ["en"] = Create(
                ("AppTitle", "TH Chromassist"),
                ("Subtitle", "Touhou 18 bullet-colour accessibility setup wizard"),
                ("Step1Title", "Verify the game"),
                ("Step1Description", "Check the location and versions of TH18 and its thcrap translation setup."),
                ("Step2Title", "Choose a colour-vision type"),
                ("Step2Description", "Choose the closest colour-vision characteristic. This is not a medical diagnosis."),
                ("Step3Title", "Strength and preview"),
                ("Step3Description", "Moving the slider updates the local bullet preview immediately."),
                ("Step4Title", "Apply and verify"),
                ("Step4Description", "Review your choices and launch the game through thcrap."),
                ("Back", "Back"), ("Next", "Next"), ("Rescan", "Scan again"), ("Browse", "Choose executable"),
                ("ApplyLaunch", "Apply patch and launch game"), ("Original", "Original"), ("Adjusted", "Adjusted"),
                ("Protan", "Protan family"), ("Deutan", "Deutan family"), ("Tritan", "Tritan family"),
                ("ProtanDescription", "Experimental setting for difficulty distinguishing red-family colours"),
                ("DeutanDescription", "Experimental setting for difficulty distinguishing green-family colours"),
                ("TritanDescription", "Experimental setting for difficulty distinguishing blue-yellow colours"),
                ("Strength", "Correction strength"), ("StrengthLow", "No change"), ("StrengthHigh", "Strong change"),
                ("Experimental", "The current transformation strengths are experimental and have not been validated in user studies."),
                ("GameSummary", "Game"), ("VisionSummary", "Colour-vision type"), ("StrengthSummary", "Strength"),
                ("ConfigSummary", "thcrap configuration to apply"), ("PendingConfig", "Created when launched"),
                ("Ready", "Looking for the game…"),
                ("NoGame", "No supported TH18 installation was found. Choose the executable manually."),
                ("TexturesReady", "Game and thcrap verified · {0} textures prepared"),
                ("GeneratingPatch", "Generating a local patch at the selected strength…"),
                ("Launching", "Launching thcrap loader with the generated configuration…"),
                ("Verifying", "Automatically checking the thcrap log for patch application…"),
                ("VerificationPending", "Not launched yet"),
                ("VerificationPendingDescription", "After launch, this shows whether the run configuration and six textures were loaded."),
                ("VerificationStackSuccess", "Chromassist patch selection and activation confirmed"),
                ("VerificationRuntimeSuccess", "Chromassist texture application confirmed: 6/6"),
                ("VerificationFailed", "Could not confirm patch application"),
                ("VerificationFiles", "Run configuration: {2} · patch stack: {3} · textures loaded: {0}/{1}"),
                ("Yes", "Yes"), ("No", "No"),
                ("ErrorTitle", "TH Chromassist error"), ("InfoTitle", "TH Chromassist"))
        };

    private string _language = NormalizeLanguage(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

    public string Language
    {
        get => _language;
        set => _language = NormalizeLanguage(value);
    }

    public string this[string key] => Values[_language].TryGetValue(key, out var value) ? value : key;

    public static IReadOnlyCollection<string> GetKeys(string language) =>
        Values[NormalizeLanguage(language)].Keys.ToArray();

    private static IReadOnlyDictionary<string, string> Create(params (string Key, string Value)[] entries) =>
        entries.ToDictionary(static entry => entry.Key, static entry => entry.Value, StringComparer.OrdinalIgnoreCase);

    private static string NormalizeLanguage(string language) => Values.ContainsKey(language) ? language : "en";
}
