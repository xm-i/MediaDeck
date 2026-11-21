// MediaDeck SonarQube 解析・テスト実行用 Jenkins Declarative Pipeline
// 実行要件:
// - Windows ビルドエージェント（.NET 11 SDK / Windows SDK 10.0.26100.0 が必要）
// - Jenkins の「SonarQube Scanner」プラグインでサーバー名 "SonarQube" (URL: https://sonarqube.home.internal) を登録済みであること
//   ※ withSonarQubeEnv を使わず Secret text クレデンシャルのみで実行する場合は、ステージ内のコメント例に切り替えてください
// - GitHub Webhook による自動ビルドを行う場合は、Jenkins に「GitHub」プラグインがインストールされていること

pipeline {
  // Windows エージェント（開発機等）に "windows" ラベルを付与して実行
  agent {
    label 'windows'
  }

  options {
    // 多重実行によるビルド成果物・.sonarqube 作業フォルダの競合を防止
    disableConcurrentBuilds()
  }

  triggers {
    // GitHub Webhook（/github-webhook/）からの push 通知で自動ビルドを実行
    githubPush()
  }

  environment {
    SONAR_PROJECT_KEY = 'xm-i_MediaDeck_276a5153-1289-4cf8-88c6-e97e51bfce54'
    SONAR_PROJECT_NAME = 'MediaDeck'
    // Java 側で内部証明書エラー (PKIX path building failed) が発生する場合は以下または truststore を設定してください（詳細は docs/sonarqube.md 参照）
    // SONAR_SCANNER_OPTS = '-Djavax.net.ssl.trustStoreType=WINDOWS-ROOT -Djavax.net.ssl.trustStore=NONE'
  }

  stages {
    stage('Checkout') {
      steps {
        checkout scm
        script {
          if (isUnix()) {
            error('MediaDeck は WinUI 3 を含むため、ソリューション全体のビルド・SonarQube 解析には Windows エージェントが必要です。')
          }
        }
        powershell '''
          $ErrorActionPreference = 'Stop'
          [Console]::OutputEncoding = [System.Text.Encoding]::UTF8

          # サブモジュール（AutoDiAttributes / GenJsonConfig）を確実に取得
          git submodule sync --recursive
          if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
          git submodule update --init --recursive
          if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        '''
      }
    }

    stage('SonarQube Analysis & Test') {
      steps {
        // Jenkins「システム設定 > SonarQube servers」の Name と一致させる
        withSonarQubeEnv('SonarQube') {
          // ※ SonarQube プラグインの withSonarQubeEnv を使わない場合の代替例:
          // withCredentials([string(credentialsId: 'sonarqube-token', variable: 'SONAR_AUTH_TOKEN')]) {
          powershell '''
            $ErrorActionPreference = 'Stop'
            [Console]::OutputEncoding = [System.Text.Encoding]::UTF8

            function Invoke-Checked {
              param([Parameter(Mandatory = $true)][scriptblock]$Command)
              & $Command
              if ($LASTEXITCODE -ne 0) {
                exit $LASTEXITCODE
              }
            }

            # 前回実行時の中間成果物をクリーンアップ
            if (Test-Path '.sonarqube') { Remove-Item -Path '.sonarqube' -Recurse -Force }
            if (Test-Path 'TestResults') { Remove-Item -Path 'TestResults' -Recurse -Force }

            # Directory.Build.props からアプリバージョンを取得
            [xml]$buildProps = Get-Content 'Directory.Build.props'
            $projectVersion = ($buildProps.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1)

            $analysisSettingsPath = (Resolve-Path 'SonarQube.Analysis.xml').Path
            $sonarHostUrl = if ($env:SONAR_HOST_URL) { $env:SONAR_HOST_URL.TrimEnd('/') } else { 'https://sonarqube.home.internal' }
            $sonarToken = if ($env:SONAR_AUTH_TOKEN) { $env:SONAR_AUTH_TOKEN } else { $env:SONAR_TOKEN }

            if ([string]::IsNullOrWhiteSpace($sonarToken)) {
              Write-Error 'SONAR_AUTH_TOKEN (または SONAR_TOKEN) が設定されていません。Jenkins の SonarQube 設定または Credentials を確認してください。'
              exit 1
            }

            # ローカルツール（dotnet-sonarscanner）と NuGet パッケージを復元
            Invoke-Checked { dotnet tool restore }
            Invoke-Checked { dotnet restore MediaDeck.slnx -p:Platform=x64 }

            # SonarScanner 開始（Community Build のため sonar.branch.name は指定しない）
            # sonar.qualitygate.wait=true により、end 実行時に Quality Gate 評価完了まで待機し Failed ならビルドを失敗させる
            $beginArgs = @(
              'tool', 'run', 'dotnet-sonarscanner', 'begin',
              "/k:$($env:SONAR_PROJECT_KEY)",
              "/n:$($env:SONAR_PROJECT_NAME)",
              "/s:$analysisSettingsPath",
              "/d:sonar.host.url=$sonarHostUrl",
              "/d:sonar.token=$sonarToken",
              '/d:sonar.qualitygate.wait=true',
              '/d:sonar.qualitygate.timeout=300'
            )
            if (-not [string]::IsNullOrWhiteSpace($projectVersion)) {
              $beginArgs += "/v:$projectVersion"
            }

            Invoke-Checked { & dotnet @beginArgs }

            # ソリューション全体のビルド（Release / x64）
            Invoke-Checked { dotnet build MediaDeck.slnx -c Release -p:Platform=x64 --no-restore }

            # 単体テスト実行および TRX・OpenCover カバレッジ計測
            Invoke-Checked {
              dotnet test MediaDeck.Core.Tests/MediaDeck.Core.Tests.csproj `
                -c Release `
                --no-build `
                --logger 'trx;LogFileName=test-results.trx' `
                --collect 'XPlat Code Coverage;Format=opencover' `
                --results-directory './TestResults'
            }

            # 解析結果を SonarQube サーバーへ送信し、Quality Gate 評価完了まで待機（Failed 時は非ゼロ終了）
            Invoke-Checked { dotnet tool run dotnet-sonarscanner end "/d:sonar.token=$sonarToken" }
          '''
          // }
        }
      }
    }

    // 現在は sonar.qualitygate.wait=true により上記ステージ内で Quality Gate 評価まで待機して成否を判定しています
    // Webhook 方式で Quality Gate を独立ステージに分離したい場合は、上記 begin 引数の
    // /d:sonar.qualitygate.wait=true と /d:sonar.qualitygate.timeout=300 を外し、
    // SonarQube 側で Jenkins 向け Webhook を設定したうえで以下のコメントを解除してください
    // stage('Quality Gate') {
    //   steps {
    //     timeout(time: 5, unit: 'MINUTES') {
    //       waitForQualityGate abortPipeline: true
    //     }
    //   }
    // }
  }

  post {
    always {
      archiveArtifacts artifacts: 'TestResults/**', allowEmptyArchive: true
      script {
        if (!isUnix()) {
          powershell '''
            if (Test-Path '.sonarqube') { Remove-Item -Path '.sonarqube' -Recurse -Force -ErrorAction SilentlyContinue }
          '''
        }
      }
    }
  }
}
