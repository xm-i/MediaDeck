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
  - **`FileTypes/`**: 画像、動画、PDF、アーカイブなど、メディアタイプ（ファイルタイプ）ごとの個別処理や詳細な実装コードはこのディレクトリ以下に配置する。
- `MediaDeck.Database/`: データベース層 (EF Core)。DBコンテキストやエンティティの定義・アクセスはここに集約する。
- `MediaDeck.Composition/`: DI（依存性の注入）やシステム全体の共通設定。

### 実装上のルール
- **コメント規約**: コメントは必ず「日本語」で記述すること。また、新規作成・編集したクラス、メソッド、プロパティには必ずXMLドキュメンテーションコメント（`/// <summary>...`）を付与すること。
- **MVVMパターンの遵守**: View (XAML) と ViewModel の分離を徹底する。コードビハインド（.xaml.cs）への記述は最小限にし、純粋なUI制御（アニメーションなど）のみに留める。
- **R3の使用 (非常に重要)**: 非同期処理、データバインディング、コマンドの実装には `R3` （Rxの代替）を活用する。`[ObservableProperty]` や `[RelayCommand]` といった CommunityToolkit のジェネレーターは使用せず、`ReactiveProperty` や `ReactiveCommand` 等でリアクティブに設計する。また、購読の解除漏れを防ぐため、`AddTo(this.CompositeDisposable)` を徹底すること。これにより、親オブジェクトの `Dispose` 時に全ての購読も破棄されるように設計する。
- **設定管理**: アプリケーション設定は `R3.JsonConfig` を利用して管理・永続化を行う。
- **ロギング**: プロジェクト全体のログ出力には `Serilog` を使用する。
- **DIとサービス管理**: `AutoDiAttributes` などを活用してDIの自動登録を行い、コンポーネント間は疎結合に保つ。
- **R3/ObservableCollectionsのバインディング (非常に重要)**: `ObservableList<T>` や `ObservableDictionary<K, V>` はそのままでは WinUI 3 の UI にバインドできません。ViewModel では `.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current)` を使用して、UI スレッドへの同期を伴う `INotifyCollectionChangedSynchronizedViewList<T>` 等に変換して公開してください。

## 4. 拡張自動化ワークフローについて
もし「定型的なビルド手順」「特定のテストの実行手順」「新規機能追加時の雛形作成手順」などをAIに自動で行わせたい場合は、プロジェクトルートに `.agents/workflows/` というディレクトリを作成し、その中に手順を書いたマークダウンファイル（例：`build_step.md`）を配置することができます。

---

> 💡 **ユーザーさまへ**
> このファイルはAIの振る舞いを決定づける重要なファイルです。「ここでは必ずこのクラスを継承する」「こういう書き方はNG」など、プロジェクト固有のルールが増えた場合は、このファイルにどんどん追記してください。追記するほどAIの提案精度が向上します。
