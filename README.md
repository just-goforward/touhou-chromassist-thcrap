# touhou-chromassist-thcrap

Windows용 Touhou 색각 접근성 로컬 패치 생성기입니다. 현재 구현 대상은 Steam판 TH18 동방홍룡동 v1.00a입니다.

이 애플리케이션은 지원되는 게임 설치본과 기존 thcrap 구성을 확인하고, 사용자가 선택한 실험적 색각 preset을 로컬에서 적용한 PNG를 생성한 뒤 별도의 thcrap local patch로 설치합니다. 원본 `th18.exe`, `th18.dat`와 기존 번역 patch는 수정하지 않습니다.

## 현재 상태

- C# / .NET 10 / WPF portable desktop app
- Steam과 기존 thcrap 설정을 이용한 TH18 경로 탐지
- EXE/DAT SHA-256 기반 exact asset-set 검증
- THTK `thdat`·`thanm` sidecar 실행
- RGBA8 PNG decoder/encoder와 alpha/geometry 불변 검사
- Original, Protan, Deutan, Tritan 실험 preset
- 별도 `thcrap/repos/local/...` patch와 run configuration 생성
- 합성 fixture 기반 단위 테스트

Preset은 의학적 진단이나 개인 지각의 정확한 재현이 아닙니다. 현재 preset은 구현 검증을 위한 `experimental` 상태이며, 색각이상 당사자 사용자 연구 전에는 효과나 공정성 동등성을 보장하지 않습니다.

## 사용 흐름

1. portable ZIP을 원하는 폴더에 풀고 `Chromassist.App.exe`를 실행합니다.
2. 앱이 Steam의 TH18 설치본과 그 안의 thcrap 한국어 패치를 자동으로 찾고 해시를 확인할 때까지 기다립니다. 찾지 못하면 `th18.exe`를 직접 지정합니다.
3. Protan, Deutan, Tritan 프리셋 중 하나를 고릅니다. 프리셋 변경은 미리보기만 바꾸며 아직 파일을 생성하지 않습니다.
4. `적용하고 게임 실행`을 누릅니다. 앱은 로컬 임시 폴더에서 사용자 소유 에셋을 추출하고, 별도 thcrap 패치와 실행 설정을 생성한 뒤 thcrap으로 TH18을 실행합니다.

현재는 정확히 확인된 TH18 v1.00a Steam 설치본 한 종류만 지원합니다. 해시가 다른 정상 설치본도 안전을 위해 거부될 수 있습니다. 앱이 생성한 이미지는 개인 로컬 사용 범위를 벗어나 공유하지 마십시오.

## 개발

요구 사항:

- Windows 10/11 x64
- .NET 10 SDK
- Git

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
dotnet publish src/Chromassist.App/Chromassist.App.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -o publish/win-x64
```

THTK 실행 파일은 라이선스와 hash를 확인한 뒤 배포 패키지의 `tools/thtk/` 아래에 둡니다. 저장소에는 실행 파일을 커밋하지 않습니다.

검증된 portable 패키지는 다음 명령으로 만듭니다.

```powershell
.\scripts\package-portable.ps1
```

## 저작권과 비제휴 고지

이 프로젝트는 上海アリス幻樂団, ZUN, Touhou Project, Steam 또는 thpatch의 공식 프로젝트가 아닙니다. Touhou Project와 각 원작 게임의 권리는 해당 권리자에게 있습니다.

저장소와 공개 릴리스에는 Touhou 원본 이미지, 음악, 실행 파일, 추출 데이터 또는 이를 변환한 에셋을 포함하지 않습니다. 선택적 patch 생성은 사용자가 직접 보유한 로컬 설치본을 같은 컴퓨터 안에서만 처리합니다.

## License

프로젝트 코드는 [MIT](LICENSE)로 배포합니다. THTK와 thcrap은 각 프로젝트의 별도 라이선스를 따릅니다.
