# SonarQube 連携設定ガイド

MediaDeck における SonarQube (`https://sonarqube.home.internal`) の運用・セットアップ手順です。

## 構成ファイル一覧

| ファイル | 用途 |
| --- | --- |
| `Jenkinsfile` | Jenkins からビルド・単体テスト・SonarQube 解析を実行するための Declarative Pipeline |
| `SonarQube.Analysis.xml` | `dotnet-sonarscanner` の共通設定（サーバー URL、除外パス、TRX/OpenCover レポートパス） |
| `.config/dotnet-tools.json` | ローカルツールとして `dotnet-sonarscanner` のバージョンを固定 |
| `Directory.Build.targets` | 外部サブモジュール（`lib/AutoDiAttributes`, `lib/GenJsonConfig`）を MSBuild レベルで解析除外 (`SonarQubeExclude`) |
| `.sonarlint/MediaDeck.json` | Visual Studio（SonarQube for IDE）向け共有 Connected Mode 設定 |
| `.sonarlint/connectedMode.json` | VS Code（SonarQube for IDE）向け共有 Connected Mode 設定 |
| `.vscode/extensions.json` | VS Code 推奨拡張機能（`sonarsource.sonarlint-vscode`）の定義 |

- プロジェクトキー: `xm-i_MediaDeck_276a5153-1289-4cf8-88c6-e97e51bfce54`
- 除外対象: 外部サブモジュール（`lib/AutoDiAttributes/**`, `lib/GenJsonConfig/**`）、自動生成ファイル（`*.g.cs`, `*.g.i.cs`）、EF Core マイグレーション。自作アナライザー（`lib/MediaDeck.Analyzers`）および WinUI 3 UI プロジェクト（`MediaDeck/**`, `MediaDeck.MediaItemTypes.UI/**`）はコード解析対象に含め、単体テスト計測対象外のためカバレッジ計測分母からは除外しています。

---

## 1. Jenkins 側のセットアップ

### 構成（Linux Jenkins コントローラ + Windows エージェント）
`MediaDeck` は WinUI 3（`net11.0-windows10.0.26100.0`）を含むため、ビルド・解析処理は Windows 環境で実行する必要があります。  
Jenkins サーバー（Ubuntu）とは別に、**開発用 Windows PC を Jenkins の Inbound（WebSocket）エージェントとして接続する**ことで、追加の Windows ライセンスや VM リソースを消費せずにフル解析を実行できます。

#### A. Jenkins コントローラ（Ubuntu）側でのノード登録
1. **Manage Jenkins > Nodes > New Node** を開きます（ノード名例: `windows-dev`、Type: `Permanent Agent`）。
2. 以下のように設定します。
   - **Remote root directory**: `C:\jenkins-agent`（普段の開発用リポジトリとは別の作業ディレクトリを指定）
   - **Labels**: `windows`（[Jenkinsfile](../Jenkinsfile) の `agent { label 'windows' }` と一致させる）
   - **Usage**: `Only build jobs with label expressions matching this node` を選択（Linux 用ジョブが誤って Windows PC に流れないようにするため）
   - **Launch method**: `Launch agent by connecting it to the controller` を選択し、**Use WebSocket** を有効化（HTTP/HTTPS ポート経由で接続でき、追加の JNLP ポート開放が不要）
   - **Availability（可用性）**: デフォルトの `Keep this agent online as much as possible` のままにします
3. 保存後、ノード画面に表示される `agent.jar` のダウンロード URL と接続コマンド（`-secret ...`）を控えます。

#### B. Windows 開発機側でのエージェント起動準備
1. **Java（JDK 25 または 21）** をインストールします（例: `winget install EclipseAdoptium.Temurin.25.JDK` / `EclipseAdoptium.Temurin.21.JDK`）。
   - ※ Java 25 は Jenkins LTS `2.541.1` 以降（Weekly `2.534` 以降）でサポートされています。それより古い Jenkins をお使いの場合は JDK 21 を使用してください。
2. `C:\jenkins-agent` フォルダを作成し、手順 A で控えた `agent.jar` を配置します。
3. 控えた接続コマンド（例: `java -jar agent.jar -url https://<JenkinsのURL>/ -secret <シークレット> -name "windows-dev" -webSocket -workDir "C:\jenkins-agent"`）を実行します。
   - ※ 内部 CA 証明書や NuGet キャッシュを普段の開発環境と同じに保つため、Windows サービス（`LocalSystem`）ではなく**現在のログオンユーザー権限**（ログイン時タスクスケジューラやスタートアップ登録等）で起動する構成がおすすめです。
   - 前提ツール（.NET 11 SDK / Windows SDK `10.0.26100.0` / Git）がシステム PATH で参照できることを確認してください。

### SonarQube 連携設定（手順 A: プラグインを使う場合・推奨）
1. Jenkins に **SonarQube Scanner** プラグインをインストールします。
2. **Manage Jenkins > Credentials** で `Secret text` クレデンシャルを作成し、SonarQube で発行したプロジェクトトークン（またはユーザートークン）を登録します。
3. **Manage Jenkins > System > SonarQube servers** で以下を追加します。
   - **Name**: `SonarQube`（`Jenkinsfile` の `withSonarQubeEnv('SonarQube')` と一致させる）
   - **Server URL**: `https://sonarqube.home.internal`
   - **Server authentication token**: 手順 2 で作成したクレデンシャルを選択
4. Pipeline ジョブ（または Multibranch Pipeline）で本リポジトリの `Jenkinsfile` を指定します。

> **Quality Gate でビルド結果を判定したい場合**:
> SonarQube 側の **Project Settings > Webhooks** に `https://<JenkinsのURL>/sonarqube-webhook/` を登録したうえで、`Jenkinsfile` 内の `stage('Quality Gate')` のコメントアウトを解除してください。

### 手順 B: SonarQube プラグインを入れずに Credentials のみで実行する場合
`Jenkinsfile` 内の `withSonarQubeEnv('SonarQube') { ... }` を、コメント記載の `withCredentials([string(credentialsId: 'sonarqube-token', variable: 'SONAR_AUTH_TOKEN')]) { ... }` に差し替えてください。

### 内部 HTTPS 証明書（`PKIX path building failed` 対策）
`https://sonarqube.home.internal` がホーム CA 等の内部証明書を使用している場合、スキャナーの Java 実行環境が証明書を信頼できないと `end` ステップで SSL エラーになることがあります。

- **方法 1（推奨）**: Jenkins エージェントの Windows「信頼されたルート証明機関」ストアにルート CA 証明書をインストールし、必要に応じて `Jenkinsfile` の `environment` にある `SONAR_SCANNER_OPTS` 行（`-Djavax.net.ssl.trustStoreType=WINDOWS-ROOT -Djavax.net.ssl.trustStore=NONE`）を有効化してください。
- **方法 2**: PKCS#12 形式のトラストストア（`.p12`）を用意し、`dotnet-sonarscanner begin` に `/d:sonar.scanner.truststorePath=...` と `/d:sonar.scanner.truststorePassword=...` を追加してください。

---

## 2. IDE（Visual Studio / VS Code）での Connected Mode 設定

トークンなどの秘密情報はリポジトリに含めていないため、初回のみ各開発環境の IDE 上で自分の SonarQube トークンを登録してください。

### Visual Studio
1. 拡張機能 **SonarQube for Visual Studio** をインストールします。
2. `MediaDeck.slnx` を開くと、`.sonarlint/MediaDeck.json` が自動検出され、画面上部に共有バインドの案内バーが表示されます。
3. 案内に従ってバインドを承認し、SonarQube（`https://sonarqube.home.internal`）で発行した自分のユーザートークンを入力します。

### VS Code
1. 推奨拡張機能 **SonarQube for IDE**（`sonarsource.sonarlint-vscode`）をインストールします。
2. リポジトリを開くと `.sonarlint/connectedMode.json` が自動検出され、設定を適用する通知が表示されます。
3. **Use Configuration** を選択し、SonarQube で発行した自分のユーザートークンを入力します。

---

## 3. ローカルからの手動スキャン実行例（動作確認用）

PowerShell から手動で解析・送信を行う場合のコマンド例です。

```powershell
$env:SONAR_TOKEN = "<あなたのSonarQubeトークン>"
$settingsPath = (Resolve-Path "SonarQube.Analysis.xml").Path

dotnet tool restore
dotnet restore MediaDeck.slnx -p:Platform=x64

dotnet tool run dotnet-sonarscanner begin `
  /k:"xm-i_MediaDeck_276a5153-1289-4cf8-88c6-e97e51bfce54" `
  /n:"MediaDeck" `
  /s:"$settingsPath" `
  /d:sonar.token="$env:SONAR_TOKEN"

dotnet build MediaDeck.slnx -c Release -p:Platform=x64 --no-restore

dotnet test MediaDeck.Core.Tests/MediaDeck.Core.Tests.csproj `
  -c Release `
  --no-build `
  --logger "trx;LogFileName=test-results.trx" `
  --collect "XPlat Code Coverage;Format=opencover" `
  --results-directory "./TestResults"

dotnet tool run dotnet-sonarscanner end /d:sonar.token="$env:SONAR_TOKEN"
```
