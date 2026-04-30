# MediaDeck

Windows向けの高機能メディア管理アプリケーションです。画像、動画、PDF、アーカイブファイルなどを一元管理し、高度な検索、タグ付け、メタデータ管理機能を提供します。

## 主な機能

- **リポジトリ管理**: 複数のローカルフォルダをリポジトリとして登録し、一括して管理できます。
- **高度な検索とフィルタリング**:
  - **リアクティブ検索**: R3ベースの高速エンジンを搭載。数万件のライブラリでも遅延なくフィルタリングが可能です。
  - **プロパティ検索**: `prop.` プレフィックスを用いた詳細なプロパティ比較（解像度、ファイルサイズ、日付、評価等）に対応。候補選択時に専用のダイアログで比較演算子と値を設定できます。
  - **多様な条件**: タグ、ファイルパス、解像度、評価、メディアタイプ、位置情報など、豊富な条件を組み合わせ可能。
- **大規模データの高速表示**: `IAsyncEnumerable` を活用したストリーミング読み込みにより、UIをブロックすることなくバックグラウンドで逐次表示を行います。
- **メタデータ・タグ管理**: Magick.NET や FFMpegCore を利用したメタデータの自動抽出、タグカテゴリ、エイリアス機能による高度な整理が可能です。
- **多彩なビューア**: リスト表示、ラップ（グリッド）表示、詳細表示、マップ表示など、用途に合わせた表示モードを切り替えられます。
- **地図連携**: 位置情報を持つメディアを地図上にマッピングして表示できます。
- **安定したリソース管理**: 厳格なDisposeパターンの遵守により、メモリリークを抑えた安定したパフォーマンスを維持します。

## 対応メディアタイプ

- **画像**: JPEG, PNG, GIF, BMP, TIFF, HEIF, PSD, RAW (RAF), ICO, PCX, Netpbm 等 (Magick.NETによる広範なサポート)
- **動画**: FFMpegが対応する各種フォーマット
- **PDF**: PDFドキュメント
- **アーカイブ**: 各種圧縮ファイル（ZIP, 7z, RAR等）

## 技術スタック

- **フレームワーク**: .NET 10 / WinUI 3 (Windows App SDK)
- **アーキテクチャ**: MVVM パターン
- **リアクティブ・状態管理**: [R3](https://github.com/Cysharp/R3) (Next-generation Reactive Extensions)
- **データベース**: SQLite (Entity Framework Core)
- **画像処理**: Magick.NET, MetadataExtractor
- **動画処理**: FFMpegCore
- **PDF処理**: Pdfium.Net.SDK
- **ロギング**: Serilog
- **設定ファイル管理**: R3.JsonConfig
- **DI・コード生成**: AutoDiAttributes (Source Generatorを活用した自動DI登録)

## プロジェクト構成

```
MediaDeck/
├── MediaDeck/                  # メインアプリケーション (WinUI 3 / Views / Styles)
├── MediaDeck.ViewModels/       # ViewModels (R3ベースのReactiveProperty/Command)
├── MediaDeck.Core/             # ビジネスロジック・検索エンジン・モデル
├── MediaDeck.MediaItemTypes/   # メディアタイプの共通定義とロジック実装
├── MediaDeck.MediaItemTypes.UI/# メディアタイプ別のUIコンポーネント
├── MediaDeck.Store/            # 状態管理・設定永続化 (R3.JsonConfig)
├── MediaDeck.Composition/      # システム構成（DI、データベース層/EF Core）
├── MediaDeck.Common/           # 共通ユーティリティ・基底クラス類
└── lib/                        # 外部ライブラリ・サブモジュール
```

## 必要要件

- Windows 10 (1809) 以降
- .NET 10 ランタイム
- FFMpeg (動画処理機能を利用する場合、パス設定が必要です)

## ビルド手順

```powershell
# リポジトリのクローン (サブモジュールを含む)
git clone --recursive https://github.com/xm-i/MediaDeck.git

# ソリューションのビルド
dotnet build -r win-x64
```

## 設定

初回起動時に設定ファイルが自動生成されます。アプリ内の設定画面、または設定ファイルを直接編集することで以下の調整が可能です。

- **スキャン設定**: 対象リポジトリのパス、スキャン対象の拡張子。
- **読み込み設定**: 一度に読み込むバッチサイズや、ストリーミングの閾値。
- **パス設定**: FFMpegの実行ファイルパス、一時ディレクトリの場所。
- **外部アプリ連携**: 特定の拡張子を外部プログラムで開くための設定。
