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
- **設定ファイル管理**: R3.JsonConfig
- **ロギング**: Serilog

## 3. コーディング規約とアーキテクチャルール
### プロジェクト構成とファイル定義
- `MediaDeck/` (メインアプリ): Views (XAML), ViewModels, Models (ビジネスロジック) が含まれる。
  - **`MediaItemTypes/`**: 画像、動画、PDF、アーカイブなど、メディアタイプ（ファイルタイプ）ごとの個別処理や詳細な実装コードはこのディレクトリ以下に配置する。
- `MediaDeck.Database/`: データベース層 (EF Core)。DBコンテキストやエンティティの定義・アクセスはここに集約する。
- `MediaDeck.Composition/`: DI（依存性の注入）やシステム全体の共通設定。

### 実装上のルール
- **コメント規約**: コメントは必ず「日本語」で記述すること。また、新規作成・編集したクラス、メソッド、プロパティには必ずXMLドキュメンテーションコメント（`/// <summary>...`）を付与すること。
- **MVVMパターンの遵守**: View (XAML) と ViewModel の分離を徹底する。コードビハインド（.xaml.cs）への記述は最小限にし、純粋なUI制御（アニメーションなど）のみに留める。
- **R3の使用 (非常に重要)**: 非同期処理、データバインディング、コマンドの実装には `R3` （Rxの代替）を活用する。`[ObservableProperty]` や `[RelayCommand]` といった CommunityToolkit のジェネレーターは使用せず、`ReactiveProperty` や `ReactiveCommand` 等でリアクティブに設計する。また、購読の解除漏れを防ぐため、`AddTo(this.CompositeDisposable)` を徹底すること。これにより、親オブジェクトの `Dispose` 時に全ての購読も破棄されるように設計する。
- **非同期購読には `SubscribeAwait` を使うこと**: `Subscribe(async _ => ...)` のような `async void` 相当の書き方は禁止。非同期処理が伴う購読は必ず `SubscribeAwait` を使用し、`AwaitOperation` を明示的に指定すること。
- **Disposeルール (重要)**:
  - すべてのViewModel、Model、Serviceは `DisposableBase` を継承する基底クラス（`ViewModelBase` / `ModelBase` / `ServiceBase`）を継承すること。
  - `IDisposable` を返す全てのメソッド呼び出し（`Subscribe`, `SubscribeAwait`, `new ReactiveProperty<T>()` 等）は、必ず `.AddTo(this.CompositeDisposable)` すること。
  - `Dispose(bool)` のオーバーライドで独自リソースを解放する場合は、必ず `base.Dispose(disposing)` を呼ぶこと。
  - Viewのコードビハインドでは `Unloaded` イベント等で `(this.DataContext as IDisposable)?.Dispose()` を呼び、ViewModelのDisposeを確実にトリガーすること。
- **設定管理**: アプリケーション設定は `R3.JsonConfig` を利用して管理・永続化を行う。
- **TabState拡張時の必須更新 (重要)**: `TabStateModel` / `SearchStateModel` / `ViewerStateModel` に新しい状態プロパティを追加した場合、必ず次の2箇所も同時に更新すること。
  - `MainWindowViewModel.AddTab`: `AppState.DefaultTabState` から新規 `tabState` へ初期値をコピーする処理を追加する。
  - `TabContext.SubscribeDefaultTabStateSync`: タブ側変更を `AppState.DefaultTabState` へ反映する購読処理を追加する。
- **ロギング**: プロジェクト全体のログ出力には `Serilog` を使用する。
- **DIとサービス管理**: `AutoDiAttributes` などを活用してDIの自動登録を行い、コンポーネント間は疎結合に保つ。
- **R3/ObservableCollectionsのバインディング (非常に重要)**: `ObservableList<T>` や `ObservableDictionary<K, V>` はそのままでは WinUI 3 の UI にバインドできません。ViewModel では `.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current)` を使用して、UI スレッドへの同期を伴う `INotifyCollectionChangedSynchronizedViewList<T>` 等に変換して公開してください。
- **コーディングスタイル**: プロジェクトルートの `.editorconfig` に定義されているルールを遵守すること。コードを記述・修正する際は必ずこれらのルールに従うこと。
- **x:Bind を優先すること**: XAML のデータバインディングでは `{Binding}` ではなく `{x:Bind}` を使用すること。型安全性とパフォーマンスの観点から `{x:Bind}` を優先し、`{Binding}` への安易な変更は禁止する。
- **作業完了前の検証 (重要)**: 作業を終了する前に、必ず `dotnet build`、`dotnet test`、および `dotnet format` を実行し、ビルドエラー、テスト失敗、フォーマット違反がすべて解消されていることを確認すること。

## 4. 拡張自動化ワークフローについて
もし「定型的なビルド手順」「特定のテストの実行手順」「新規機能追加時の雛形作成手順」などをAIに自動で行わせたい場合は、プロジェクトルートに `.agents/workflows/` というディレクトリを作成し、その中に手順を書いたマークダウンファイル（例：`build_step.md`）を配置することができます。