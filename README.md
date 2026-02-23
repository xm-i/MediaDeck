# MediaDeck

Windows向けのメディア管理アプリケーションです。画像、動画、PDF、アーカイブファイルなどを一元管理し、タグ付け、フィルタリング、ソートなどの機能を提供します。

## 機能

### 対応メディアタイプ
- **画像**: JPEG, PNG, GIF, BMP, TIFF, HEIF, PSD, RAW (RAF), ICO, PCX, Netpbm など
- **動画**: FFMpeg対応フォーマット
- **PDF**: PDFドキュメント
- **アーカイブ**: 圧縮ファイル

### 主な機能
- 📁 **フォルダ管理**: 複数フォルダをリポジトリとして登録・管理
- 🏷️ **タグ管理**: ファイルへのタグ付け、タグカテゴリ、エイリアス機能
- 🔍 **高度なフィルタリング**: タグ、ファイルパス、解像度、評価、メディアタイプ、位置情報などで絞り込み
- 📊 **ソート機能**: 様々な条件でのソート
- 🖼️ **複数ビューア**: リスト表示、ラップ表示、詳細表示、マップ表示
- 🗺️ **地図連携**: 位置情報付きメディアのマップ表示
- 🎨 **サムネイル管理**: カスタムサムネイルの設定

## 技術スタック

- **フレームワーク**: .NET 10 / WinUI 3 (Windows App SDK)
- **アーキテクチャ**: MVVM (CommunityToolkit.Mvvm)
- **データベース**: SQLite (Entity Framework Core)
- **画像処理**: Magick.NET, MetadataExtractor
- **動画処理**: FFMpegCore
- **PDF処理**: Pdfium.Net.SDK
- **リアクティブ**: R3, ObservableCollections

## プロジェクト構成

```
MediaDeck/
├── MediaDeck/                  # メインアプリケーション (WinUI 3)
│   ├── FileTypes/              # ファイルタイプ別の実装
│   │   ├── Image/              # 画像ファイル
│   │   ├── Video/              # 動画ファイル
│   │   ├── Pdf/                # PDFファイル
│   │   ├── Archive/            # アーカイブファイル
│   │   └── Unknown/            # 未対応ファイル
│   ├── Models/                 # ビジネスロジック
│   ├── ViewModels/             # ビューモデル
│   ├── Views/                  # UI (XAML)
│   ├── Stores/                 # 状態・設定管理
│   └── Utils/                  # ユーティリティ
├── MediaDeck.Database/         # データベース層 (EF Core)
├── MediaDeck.Composition/      # DI・共通定義
└── lib/                        # サブモジュール
    ├── AutoDiAttributes/       # DI自動登録用Source Generator
    └── R3.JsonConfig/          # R3ベースの設定管理
```

## 必要要件

- Windows 10 (1809) 以降
- .NET 10 ランタイム
- FFMpeg (動画処理用、パス設定が必要)

## ビルド

```powershell
# リポジトリのクローン (サブモジュール含む)
git clone --recursive https://github.com/xm-i/MediaDeck.git

# ビルド
dotnet build
```

## 設定

アプリケーション初回起動時に設定ファイルが作成されます。以下の設定が可能です：

- **スキャン設定**: 対象フォルダ、拡張子フィルタ
- **パス設定**: FFMpegのパス、一時フォルダのパス
- **実行プログラム設定**: 外部アプリケーションとの連携


## 開発メモ

> **開発方針**
> - とりあえず、必要な機能が揃うところまで使い勝手・見た目・バリデーション・エラー処理は無視で作る
> - 画像優先。動画対応はあと