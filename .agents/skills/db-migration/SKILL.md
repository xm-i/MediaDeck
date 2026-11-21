---
name: db-migration
description: >-
  データベーススキーマ変更（テーブル・カラム・インデックス・制約の追加/変更/削除）、
  MediaDeckDbContext や Tables の更新、および DbVersions を用いたマイグレーション追加を行う際に使用する。
---

# データベースマイグレーションの仕組みと実装手順

MediaDeck における SQLite データベースのスキーマ管理およびマイグレーション手順書です。

## 1. 仕組みの概要（アーキテクチャ）

MediaDeck は EF Core のコードファースト自動マイグレーション（`dotnet ef migrations`）ではなく、**`EnsureCreatedAsync()` と `DbVersions` テーブル（`Id = 1` の `Version`）を組み合わせた独自マイグレーション方式**を採用しています。

起動時に [App.xaml.cs](file:///MediaDeck/App.xaml.cs) から [IDatabaseMigrationService.InitializeAsync](file:///MediaDeck.Composition/Interfaces/Services/IDatabaseMigrationService.cs)（実装: [DatabaseMigrationService.cs](file:///MediaDeck.Core/Services/Database/DatabaseMigrationService.cs)）が呼ばれ、以下の流れで DB を初期化・更新します。

1. **新規 DB（または 0 バイト空ファイル）の場合**
   - DB ファイルが存在しない場合は親ディレクトリを作成し、0 バイトファイルを事前作成する（`EnsureCreatedAsync()` 失敗回避のため）。
   - `db.Database.EnsureCreatedAsync()` が `true` を返し、最新の [MediaDeckDbContext.cs](file:///MediaDeck.Composition/Database/MediaDeckDbContext.cs) 定義で全テーブル・全インデックスを一括作成する。
   - トランザクション内で `DbVersions` テーブルへ `Id = 1, Version = DatabaseMigrationRegistry.LatestVersion` を登録する。
2. **既存 DB の場合**
   - `db.Database.EnsureCreatedAsync()` は `false` を返し、既存 DB に対しては何もしない。
   - `DbVersions` テーブル（`v1.0.0` 以降常に存在）の `Id = 1` レコードから現在バージョンを読み取る（レコード欠落時は `InitialVersion = 1` 扱い）。
   - **現在バージョン == `LatestVersion`**: 書き込みなしで即終了する（レコード欠落時のみ登録）。
   - **現在バージョン > `LatestVersion`**: ダウングレード起動保護として警告ログを出力し、バージョンを変更せずマイグレーションをスキップする。
   - **現在バージョン < `LatestVersion`**: `TargetVersion` 昇順で未適用ステップを **1 ステップ = 1 トランザクション** で順次実行し、各ステップ内で DDL/DML 適用と `DbVersions.Version` 更新をまとめてコミットする。途中で例外が発生した場合はそのステップをロールバックし、`ChangeTracker.Clear()` して例外を再スローする。

## 2. 関連ファイル一覧

- **[MediaDeckDbContext.cs](file:///MediaDeck.Composition/Database/MediaDeckDbContext.cs)**: 最新スキーマ定義（エンティティ・リレーション・インデックス・`NOCASE` 照合順序など）
- **[DbVersion.cs](file:///MediaDeck.Composition/Tables/DbVersion.cs)**: バージョン管理テーブルエンティティ
- **[IDatabaseMigration.cs](file:///MediaDeck.Composition/Database/Migrations/IDatabaseMigration.cs)**: マイグレーションステップのインターフェース
- **[DatabaseMigrationRegistry.cs](file:///MediaDeck.Composition/Database/Migrations/DatabaseMigrationRegistry.cs)**: `InitialVersion`, `DbVersionRecordId`, `LatestVersion`, `Migrations` 一覧を管理
- **[Migration002AddMediaItemSystemIndexes.cs](file:///MediaDeck.Composition/Database/Migrations/Migration002AddMediaItemSystemIndexes.cs)**: バージョン 2 のマイグレーション実装例
- **[IDatabaseMigrationService.cs](file:///MediaDeck.Composition/Interfaces/Services/IDatabaseMigrationService.cs)**: サービスインターフェース
- **[DatabaseMigrationService.cs](file:///MediaDeck.Core/Services/Database/DatabaseMigrationService.cs)**: 初期化およびマイグレーション実行本体
- **[DatabaseMigrationServiceTests.cs](file:///MediaDeck.Core.Tests/Services/Database/DatabaseMigrationServiceTests.cs)**: マイグレーション・DDL 一致・`EXPLAIN QUERY PLAN` 検証テスト

## 3. 新しいマイグレーションを追加する手順

テーブル・カラム・インデックス・制約などを変更する際は、必ず以下の手順をすべて実施してください。

1. **最新スキーマ定義を更新する**
   - [MediaDeck.Composition/Tables/](file:///MediaDeck.Composition/Tables/) のエンティティ定義および [MediaDeckDbContext.cs](file:///MediaDeck.Composition/Database/MediaDeckDbContext.cs) の `OnModelCreating` を最新状態に更新し、新規作成 DB が最初から最新スキーマになるようにする。
2. **マイグレーションステップを作成する**
   - `MediaDeck.Composition/Database/Migrations/` 配下に [IDatabaseMigration](file:///MediaDeck.Composition/Database/Migrations/IDatabaseMigration.cs) の実装クラスを作成する。
   - **命名規則**: `Migration{3桁ゼロ埋め番号}{PascalCase内容}`（例: `Migration003AddSomethingColumn`）。`CA1707` 回避のため、クラス名にアンダースコア `_` を含めないこと。
   - **冪等性**: SQL は可能な限り冪等に記述する（例: `CREATE INDEX IF NOT EXISTS`, `CREATE TABLE IF NOT EXISTS`）。
   - **DDL 一致原則**: 新規 DB で EF Core の `EnsureCreatedAsync()` が生成する SQLite DDL（テーブル名・カラム名・インデックス名・引用符・`COLLATE NOCASE` 等）と、マイグレーション SQL で生成される DDL が一致するようにする。
3. **レジストリを更新する**
   - [DatabaseMigrationRegistry.cs](file:///MediaDeck.Composition/Database/Migrations/DatabaseMigrationRegistry.cs) の `LatestVersion` をインクリメントし、`Migrations` 配列へ新しいマイグレーションインスタンスを追加する。
   - ※ `DatabaseMigrationService` のコンストラクタが「重複バージョン」「範囲外バージョン（`<= InitialVersion` または `> LatestVersion`）」「`LatestVersion` に対応するステップ欠落」を検証するため、登録漏れや不整合があると起動時・テスト時に即座に例外が発生する。
4. **ユニットテストを追加・更新する**
   - [DatabaseMigrationServiceTests.cs](file:///MediaDeck.Core.Tests/Services/Database/DatabaseMigrationServiceTests.cs) に以下を追加・更新する。
     - 旧バージョン DB からのマイグレーション実行と既存データ保持の確認。
     - 新規 `EnsureCreatedAsync()` DB とマイグレーション後 DB の `sqlite_master` 上の DDL 一致検証。
     - インデックス追加時は、実際の EF Core LINQ クエリに対して `query.CreateDbCommand()` と `EXPLAIN QUERY PLAN` を実行し、期待通りインデックス（特にカバリングインデックス）が選択されることの確認。
5. **ビルド・テスト・フォーマットを検証する**
   - 変更完了前にソリューション全体のビルド・テスト・フォーマット検証を実行し、すべて合格することを確認する。
   ```bash
   dotnet build MediaDeck.slnx -p:Platform=x64
   dotnet test MediaDeck.Core.Tests/MediaDeck.Core.Tests.csproj
   dotnet format MediaDeck.slnx --verify-no-changes
   ```

## 4. インデックス設計のガイドライン

- **ユーザー設定依存のカラムには安易に張らない**: ユーザーが自由に切り替えられるソート/フィルタ条件用のカラム（例: `Rate`, `UsageCount`, `ModifiedTime` 等）は利用パターンが人によって異なり、特に `UsageCount` のように閲覧ごとに更新されるカラムではインデックス維持コストが増大するため、決め打ちで追加しない。
- **システム共通クエリに絞る**: 追加対象は全ユーザー共通でアプリ内部が必ず実行するクエリ（例: `FolderRepository` の `DirectoryPath` 集計、`FileHashUpdatorService` / `DuplicateFileDetectorService` の `PreHash` 集計）に絞り、実クエリの `EXPLAIN QUERY PLAN` でカバリングインデックス等の効果を確認してから採用する。