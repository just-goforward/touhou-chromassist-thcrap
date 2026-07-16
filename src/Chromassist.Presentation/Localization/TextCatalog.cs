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
                ("Step4Title", "패치 준비 및 thcrap 설정"),
                ("Step4Description", "로컬 패치를 만든 뒤 thcrap 설정 도구에서 선택하고 영구 바로가기를 생성합니다."),
                ("Back", "이전"), ("Next", "다음"), ("Rescan", "다시 찾기"), ("Browse", "실행 파일 직접 지정"),
                ("ApplyLaunch", "패치 준비 후 thcrap 설정 열기"), ("Original", "원본"), ("Adjusted", "변경 후"),
                ("Protan", "적색 계열 색각이상"), ("Deutan", "녹색 계열 색각이상"), ("Tritan", "청황색 계열 색각이상"),
                ("ProtanDescription", "흔히 적색약·적색맹으로 부르는 유형을 포괄하는 실험 설정"),
                ("DeutanDescription", "흔히 녹색약·녹색맹으로 부르는 유형을 포괄하는 실험 설정"),
                ("TritanDescription", "청색과 황색 계열 구분이 어려운 유형을 위한 실험 설정"),
                ("Strength", "보정 강도"), ("StrengthLow", "변화 없음"), ("StrengthHigh", "강한 변화"),
                ("Experimental", "현재 변환량은 사용자 연구로 검증되지 않은 실험값입니다."),
                ("GameSummary", "게임"), ("VisionSummary", "색각 유형"), ("StrengthSummary", "강도"),
                ("ConfigSummary", "thcrap에서 선택할 패치"), ("PendingConfig", "실행 시 로컬 저장소에 등록됩니다"),
                ("SetupInstructions", "thcrap의 ‘All patches’ 탭에서 위 패치를 선택한 뒤 설정 이름과 바로가기 생성을 완료하세요."),
                ("Ready", "게임을 찾고 있습니다…"),
                ("NoGame", "지원되는 TH18 설치본을 찾지 못했습니다. 실행 파일을 직접 지정하세요."),
                ("TexturesReady", "게임과 thcrap 검증 완료 · texture {0}개 준비됨"),
                ("GeneratingPatch", "선택한 강도로 로컬 패치를 생성하고 있습니다…"),
                ("LaunchingSetup", "thcrap 설정 도구를 열고 있습니다…"),
                ("SetupOpened", "thcrap에서 TH Chromassist (local) 패치를 선택하고 바로가기를 생성하세요."),
                ("VerificationPending", "실행 시 패치 준비 상태를 자동 검사합니다"),
                ("VerificationPendingDescription", "생성 파일 수, 알파 채널, 투명 픽셀과 로컬 저장소 메타데이터를 검사합니다."),
                ("VerificationPreparationSuccess", "로컬 패치 준비 검증 완료"),
                ("VerificationFailed", "로컬 패치 준비 검증 실패"),
                ("VerificationPreparationDetails", "파일: {0}/{1} · 알파 보존: {2} · 투명 픽셀 보존: {3} · repo.js: {4}"),
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
                ("Step4Title", "パッチ準備とthcrap設定"),
                ("Step4Description", "ローカルパッチを作成し、thcrap設定ツールで選択して常用ショートカットを作ります。"),
                ("Back", "戻る"), ("Next", "次へ"), ("Rescan", "再検索"), ("Browse", "実行ファイルを指定"),
                ("ApplyLaunch", "パッチを準備してthcrap設定を開く"), ("Original", "元画像"), ("Adjusted", "変更後"),
                ("Protan", "1型色覚（赤系）"), ("Deutan", "2型色覚（緑系）"), ("Tritan", "3型色覚（青黄系）"),
                ("ProtanDescription", "1型2色覚と1型3色覚を区別せず扱う実験設定"),
                ("DeutanDescription", "2型2色覚と2型3色覚を区別せず扱う実験設定"),
                ("TritanDescription", "青・黄系の識別が難しい3型色覚向けの実験設定"),
                ("Strength", "補正強度"), ("StrengthLow", "変化なし"), ("StrengthHigh", "強い変化"),
                ("Experimental", "現在の変換量はユーザー調査で検証されていない実験値です。"),
                ("GameSummary", "ゲーム"), ("VisionSummary", "色覚タイプ"), ("StrengthSummary", "強度"),
                ("ConfigSummary", "thcrapで選択するパッチ"), ("PendingConfig", "実行時にローカルリポジトリへ登録されます"),
                ("SetupInstructions", "thcrapの「All patches」タブで上記パッチを選択し、設定名とショートカットの作成を完了してください。"),
                ("Ready", "ゲームを検索しています…"),
                ("NoGame", "対応するTH18を検出できませんでした。実行ファイルを指定してください。"),
                ("TexturesReady", "ゲームとthcrapの確認完了 · texture {0}個を準備しました"),
                ("GeneratingPatch", "選択した強度でローカルパッチを生成しています…"),
                ("LaunchingSetup", "thcrap設定ツールを開いています…"),
                ("SetupOpened", "thcrapでTH Chromassist (local)パッチを選択し、ショートカットを作成してください。"),
                ("VerificationPending", "実行時にパッチ準備状態を自動確認します"),
                ("VerificationPendingDescription", "生成ファイル数、アルファ、透明ピクセル、ローカルリポジトリ情報を確認します。"),
                ("VerificationPreparationSuccess", "ローカルパッチの準備確認が完了しました"),
                ("VerificationFailed", "ローカルパッチの準備確認に失敗しました"),
                ("VerificationPreparationDetails", "ファイル: {0}/{1} · アルファ保持: {2} · 透明ピクセル保持: {3} · repo.js: {4}"),
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
                ("Step4Title", "Prepare patch and configure thcrap"),
                ("Step4Description", "Create the local patch, select it in thcrap setup, and create a reusable shortcut."),
                ("Back", "Back"), ("Next", "Next"), ("Rescan", "Scan again"), ("Browse", "Choose executable"),
                ("ApplyLaunch", "Prepare patch and open thcrap setup"), ("Original", "Original"), ("Adjusted", "Adjusted"),
                ("Protan", "Protan red–green CVD"), ("Deutan", "Deutan red–green CVD"), ("Tritan", "Tritan blue–yellow CVD"),
                ("ProtanDescription", "Experimental setting covering protanomaly and protanopia without diagnosing either"),
                ("DeutanDescription", "Experimental setting covering deuteranomaly and deuteranopia without diagnosing either"),
                ("TritanDescription", "Experimental setting covering tritanomaly and tritanopia without diagnosing either"),
                ("Strength", "Correction strength"), ("StrengthLow", "No change"), ("StrengthHigh", "Strong change"),
                ("Experimental", "The current transformation strengths are experimental and have not been validated in user studies."),
                ("GameSummary", "Game"), ("VisionSummary", "Colour-vision type"), ("StrengthSummary", "Strength"),
                ("ConfigSummary", "Patch to select in thcrap"), ("PendingConfig", "Registered in the local repository when run"),
                ("SetupInstructions", "In thcrap, open All patches, select the patch shown above, then finish naming the configuration and creating its shortcut."),
                ("Ready", "Looking for the game…"),
                ("NoGame", "No supported TH18 installation was found. Choose the executable manually."),
                ("TexturesReady", "Game and thcrap verified · {0} textures prepared"),
                ("GeneratingPatch", "Generating a local patch at the selected strength…"),
                ("LaunchingSetup", "Opening the thcrap setup tool…"),
                ("SetupOpened", "Select the TH Chromassist (local) patch in thcrap and create a shortcut."),
                ("VerificationPending", "Patch preparation will be checked automatically"),
                ("VerificationPendingDescription", "Checks the file count, alpha channel, transparent pixels, and local repository metadata."),
                ("VerificationPreparationSuccess", "Local patch preparation verified"),
                ("VerificationFailed", "Local patch preparation verification failed"),
                ("VerificationPreparationDetails", "Files: {0}/{1} · alpha preserved: {2} · transparent pixels preserved: {3} · repo.js: {4}"),
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
