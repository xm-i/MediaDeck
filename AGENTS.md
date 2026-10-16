# MediaDeck AI Assistant Rules (AGENTS.md)

このファイルは、AIアシスタント（Agent）が MediaDeck プロジェクトでコードを読み書きする際の**基本ルールとコンテキスト**を定義するものです。AIアシスタントはこのファイルの内容を優先して遵守します。

## 1. プロジェクトの概要と方針
- **プロジェクトの目的**: Windows向けのメディア管理アプリケーション（画像、動画、PDF、アーカイブファイルなどを一元管理）。
- **コミュニケーション**: AIアシスタントとのチャットおよびすべての回答（実装プラン `implementation_plan.md` 等を含む）は**「日本語」**で行うこと。

## 2. 技術スタック
- **言語・フレームワーク**: C#, .NET 10, WinUI 3 (Windows App SDK)
- **アーキテクチャ**: MVVMパターン
- **データベース**: SQLite (Entity Framework Core)
- **主要なライブラリ**: Magick.NET, MetadataExtractor, FFMpegCore, Pdfium.Net.SDK
- **リアクティブ・状態管理**: R3
- **設定ファイル管理**: GenJsonConfig
- **ロギング**: Serilog

## 3. コーディング規約とアーキテクチャルール
### プロジェクト構成とファイル定義
ソリューションは以下のマルチプロジェクト構成（14プロジェクト）で責務が分離されています。コードの配置や参照時は各プロジェクトの責務を厳守してください。

#### アプリケーション本体
- **`MediaDeck/`** (メインアプリケーション / WinUI 3):
  - View (XAML) とそのコードビハインド、UI固有のライフサイクル管理、UIダイアログ/ウィンドウ制御サービス（`WindowManager`, `DialogService` 等）を配置。
- **`MediaDeck.ViewModels/`** (ViewModel層):
  - アプリケーション全体の ViewModel（`MainWindowViewModel`, `TabViewModel`, セレクター各種, ペイン・ダイアログ ViewModel 等）。View と Model の調停を R3 でリアクティブに行う。
- **`MediaDeck.Core/`** (ドメインモデル・ビジネスロジック・サービス):
  - ファイル、フォルダ、タグ、検索条件、ソート条件などのコアモデル、リポジトリ、ドメインサービス（`FileRegistrar`, `FileChangeMonitorService` 等）を集約。
- **`MediaDeck.MediaItemTypes/`** (メディア種別コア拡張):
  - 画像 (`Image`)、動画 (`Video`)、PDF (`Pdf`)、アーカイブ (`Archive`)、フォルダグループ (`FolderGroup`)、不明 (`Unknown`) などのメディア種別ごとのプロバイダー（`IMediaItemTypeProvider`）、オペレーター（`IMediaItemOperator`）、ドメインモデル。
- **`MediaDeck.MediaItemTypes.UI/`** (メディア種別 UI 拡張 / WinUI 3):
  - 各メディア種別に対応する View / ViewModel（Viewer、サムネイルピッカー、一括サムネイル生成設定コントロール等）。
- **`MediaDeck.Composition/`** (共通契約・DI・システム共通設定・DB層):
  - アプリケーション全体の共通インターフェース（`Interfaces/`）、DI登録設定、定数（`Constants/`）、状態モデル・設定モデル（`Stores/State/`, `Stores/Config/`）。
  - データベース層（EF Core の `MediaDeckDbContext`、マイグレーション、エンティティ `Tables/`）もここに集約して管理する。
- **`MediaDeck.Store/`** (永続化ストア):
  - 設定（Config）や状態（State）のファイル永続化処理（`ConfigStore`, `StateStore`）、JSON シリアライザー。
- **`MediaDeck.Common/`** (共通基盤):
  - プロジェクト横断で使用される基底クラス（`DisposableBase` 等）、共通ヘルパー、拡張メソッド。
- **`MediaDeck.Core.Tests/`** (ユニットテスト):
  - `MediaDeck.Core` を対象とした xUnit 単体テスト。

#### 自社製基盤ライブラリ・Source Generator (`lib/` 配下)
- **`lib/AutoDiAttributes/`**: `[Inject]` 属性による DI（依存性の注入）登録自動化 Source Generator。
- **`lib/GenJsonConfig/`**: `ReactiveProperty` や `ObservableList` に対応した JSON DTO / シリアライザー自動生成 Source Generator。
- **`lib/MediaDeck.Analyzers/`**: R3 の `AddTo(this.CompositeDisposable)` 漏れを静的解析で検出するカスタム Roslyn アナライザー（`RequireAddToAnalyzer` など）。

### 実装上のルール
- **コメント規約**: コメントは必ず「日本語」で記述すること。また、新規作成・編集したクラス、メソッド、プロパティには必ずXMLドキュメンテーションコメント（`/// <summary>...`）を付与すること。
- **MVVMパターンの遵守**: View (XAML) と ViewModel の分離を徹底する。コードビハインド（.xaml.cs）への記述は最小限にし、純粋なUI制御（アニメーションなど）のみに留める。
- **R3の使用 (非常に重要)**: 非同期処理、データバインディング、コマンドの実装には `R3` （Rxの代替）を活用する。`[ObservableProperty]` や `[RelayCommand]` といった CommunityToolkit のジェネレーターは使用せず、`ReactiveProperty` や `ReactiveCommand` 等でリアクティブに設計する。また、購読の解除漏れを防ぐため、`AddTo(this.CompositeDisposable)` を徹底すること。これにより、親オブジェクトの `Dispose` 時に全ての購読も破棄されるように設計する。
- **非同期購読には `SubscribeAwait` を使うこと**: `Subscribe(async _ => ...)` のような `async void` 相当の書き方は禁止。非同期処理が伴う購読は必ず `SubscribeAwait` を使用し、`AwaitOperation` を明示的に指定すること。
- **Disposeルール (重要)**:
  - すべてのViewModel、Model、Serviceは `DisposableBase` を継承する基底クラス（`ViewModelBase` / `ModelBase` / `ServiceBase`）を継承すること。
  - `IDisposable` を返す全てのメソッド呼び出し（`Subscribe`, `SubscribeAwait`, `new ReactiveProperty<T>()` 等）は、必ず `.AddTo(this.CompositeDisposable)` すること。
  - `Dispose(bool)` のオーバーライドで独自リソースを解放する場合は、必ず `base.Dispose(disposing)` を呼ぶこと。
- **設定管理**: アプリケーション設定は `GenJsonConfig` を利用して管理・永続化を行う。
- **TabState拡張時の必須更新 (重要)**: `TabStateModel` / `SearchStateModel` / `ViewerStateModel` に新しい状態プロパティを追加した場合、必ず次の2箇所も同時に更新すること。
  - `TabStateModel.ApplyDefaultState`: `AppState.DefaultTabState` から新規 `tabState` へ初期値をコピーする処理を追加する（※新規タブ作成時に `MainWindowViewModel.AddTab` や `WindowManager` から本メソッドが呼ばれます）。
  - `TabViewModel.SubscribeDefaultTabStateSync`: タブ側変更を `AppState.DefaultTabState` へ反映する購読処理を追加する。
- **ロギング**: プロジェクト全体のログ出力には `Serilog` を使用する。
- **DIとサービス管理**: `AutoDiAttributes` などを活用してDIの自動登録を行い、コンポーネント間は疎結合に保つ。
- **R3/ObservableCollectionsのバインディング (非常に重要)**: `ObservableList<T>` や `ObservableDictionary<K, V>` はそのままでは WinUI 3 の UI にバインドできません。ViewModel では `.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current)` を使用して、UI スレッドへの同期を伴う `INotifyCollectionChangedSynchronizedViewList<T>` 等に変換して公開してください。
- **コーディングスタイル**: プロジェクトルートの `.editorconfig` に定義されているルールを遵守すること。コードを記述・修正する際は必ずこれらのルールに従うこと。
- **x:Bind を優先すること**: XAML のデータバインディングでは `{Binding}` ではなく `{x:Bind}` を使用すること。型安全性とパフォーマンスの観点から `{x:Bind}` を優先し、`{Binding}` への安易な変更は禁止する。
- **国際化 (i18n) の遵守 (重要)**:
  - UI上の文字列（ラベル、ツールチップ、プレースホルダー等）は、直接ハードコードせず、必ず `Strings/ja/Resources.resw` および `Strings/en/Resources.resw` に定義すること。
  - XAML要素のローカライズには `x:Uid` を使用すること。リソースキーは `Uid名.プロパティ名` （例: `MyButton.Content`, `MyControl.ToolTipService.ToolTip`）の形式で定義する。
  - **Windowクラスの注意点**: WinUI 3の `Window` 要素は `x:Uid` による `Title` プロパティの設定をネイティブにサポートしていない（ビルドエラーの原因となる）。ウィンドウタイトルをローカライズする場合は、コードビハインドで `IStringProvider` を使用して `this.Title` に直接代入すること。
  - ViewModelやビジネスロジック内で文字列が必要な場合は、`IStringProvider` をコンストラクタで注入して使用すること。
  - リソースキーは、機能や領域が判別しやすいプレフィックスを付けること（例: `TagManager_`, `Config_`, `StatusBar_`）。
- **作業完了前の検証 (重要)**: 作業を終了する前に、必ず `dotnet build`、`dotnet test`、および `dotnet format` を実行し、ビルドエラー、テスト失敗、フォーマット違反がすべて解消されていることを確認すること。

## 4. CHANGELOG の管理ルール
- **対象読者**: CHANGELOG.md はアプリの**一般ユーザー向け**に書くこと。
- **記載する基準**: 「このアプリを使うユーザーの操作・体験に直接影響する変更か？」を判断軸にする。該当するものだけ記載する。
- **記載しない基準**: ユーザーから見えない変更（コード品質、開発効率、内部構造の改善など）は記載しない。
- **文体**: 技術用語・クラス名・メソッド名は使わず、「〜できるようになりました」「〜する問題を修正しました」のように平易な日本語で書く。
- **生成方法**: バージョンアップ時は `git log <前回タグ>..HEAD --oneline --no-merges` でコミットログを取得し、上記の基準で取捨選択・意訳する。

## 5. バージョンアップ手順
新バージョンのリリース準備を行う際は、以下の手順をすべて実施すること。

1. **前回タグを確認する**
   ```
   git tag --sort=-version:refname
   ```

2. **コミットログを取得する**
   ```
   git log <前回タグ>..HEAD --oneline --no-merges
   ```

3. **バージョン番号を更新する**（更新対象ファイルは以下の2箇所）
   - `Directory.Build.props` — `<Version>`, `<AssemblyVersion>`, `<FileVersion>` を新バージョンに変更する
   - `MediaDeck/Package.appxmanifest` — `<Identity Version="...">` を新バージョン（4桁形式）に変更する
   - `MediaDeck/Package.Dev.appxmanifest` — `<Identity Version="...">` を新バージョン（4桁形式）に変更する（Debugビルドはこちらが使われるため忘れずに更新する）

4. **CHANGELOG.md を更新する**
   - 手順2のコミットログをセクション4のルールに従って整理し、新バージョンのエントリを先頭に追加する

5. **ビルドして確認する**
もし「定型的なビルド手順」「特定のテストの実行手順」「新規機能追加時の雛形作成手順」などをAIに自動で行わせたい場合は、プロジェクトルートに `.agents/workflows/` というディレクトリを作成し、その中に手順を書いたマークダウンファイル（例：`build_step.md`）を配置することができます。