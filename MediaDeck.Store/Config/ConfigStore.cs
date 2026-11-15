using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using AutoDiAttributes;
using GenJsonConfig;
using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Objects;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Core.Models.NotificationDispatcher;
using MediaDeck.Core.Stores.Config;
using MediaDeck.Store.SerializerContext;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using R3;

namespace MediaDeck.Store.Config;

[Inject(InjectServiceLifetime.Singleton, typeof(IConfigStore))]
public class ConfigStore : DisposableBase, IConfigStore {
	private readonly ILogger<ConfigStore> _logger;
	private readonly AppNotificationDispatcher _notificationDispatcher;
	private readonly IAppPathProvider _pathProvider;
	private readonly IStringProvider? _stringProvider;
	public IServiceProvider ScopedService {
		get;
	}

	public ConfigModel Config {
		get;
		private set;
	}

	protected virtual string ConfigFilePath {
		get {
			return this._pathProvider.ConfigFilePath;
		}
	}

	/// <summary>
	/// <see cref="ConfigStore"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="service">サービスプロバイダー</param>
	/// <param name="pathProvider">アプリケーションパスプロバイダー</param>
	/// <param name="stringProvider">文字列プロバイダー（省略可能）</param>
	public ConfigStore(IServiceProvider service, IAppPathProvider pathProvider, IStringProvider? stringProvider = null) {
		this.ScopedService = service;
		this._pathProvider = pathProvider;
		this._stringProvider = stringProvider ?? service.GetService<IStringProvider>();
		this._logger = service.GetRequiredService<ILogger<ConfigStore>>();
		this._notificationDispatcher = service.GetRequiredService<AppNotificationDispatcher>();
		this.Load();
	}

	public JsonSerializerOptions JsonSerializerOptions {
		get {
			return field ??= new JsonSerializerOptions() {
				WriteIndented = true,
				TypeInfoResolver = ConfigJsonSerializerContext.Default.WithAddedModifier(ForJsonConverterRegistry.ApplyPolymorphism)
			};
		}
	}

	/// <summary>
	///     保存済み設定を読み込みます。
	/// </summary>
	[MemberNotNull(nameof(Config))]
	public void Load() {
		if (this.Config != null) {
			this._logger.LogWarning("アプリケーション設定は既に読み込まれています。インスタンスの再生成はスキップされました。");
			return;
		}

		var scope = this.ScopedService.CreateScope().AddTo(this.CompositeDisposable);
		try {
			if (File.Exists(this.ConfigFilePath)) {
				try {
					var json = File.ReadAllText(this.ConfigFilePath);
					var loaded = JsonSerializer.Deserialize<ConfigModelForJson>(json, this.JsonSerializerOptions);
					if (loaded != null) {
						this.Config = ConfigModelForJson.CreateModel(loaded, scope.ServiceProvider);
						this._logger.LogInformation("アプリケーション設定の読み込みに成功しました");
						return;
					}
				} catch (Exception e) {
					this._logger.LogError(e, "アプリケーション設定ファイルのJSON解析に失敗しました: {FilePath}", this.ConfigFilePath);
					var message = this._stringProvider?.GetString("ConfigStore_LoadError_Corrupted") ?? "アプリケーション設定ファイルが破損しています。デフォルト設定を使用します。";
					var title = this._stringProvider?.GetString("ConfigStore_LoadError_Title") ?? "設定読み込みエラー";
					this._notificationDispatcher.Notify.OnNext(
						AppNotification.Error(message, title, 0));
				}
			}
		} catch (Exception ex) {
			this._logger.LogError(ex, "アプリケーション設定読み込みのスコープ生成に失敗しました");
		}

		// デフォルト設定を作成
		this.Config = scope.ServiceProvider.GetRequiredService<ConfigModel>();
		this._logger.LogWarning("デフォルトのアプリケーション設定を使用します");
	}

	/// <summary>
	///     現在の設定をファイルへ保存します。
	/// </summary>
	public void Save() {
		try {
			Directory.CreateDirectory(Path.GetDirectoryName(this.ConfigFilePath)!);

			var jsonDto = ConfigModelForJson.CreateJson(this.Config);
			var json = JsonSerializer.Serialize(jsonDto, this.JsonSerializerOptions);
			File.WriteAllText(this.ConfigFilePath, json);
			this._logger.LogInformation("アプリケーション設定を保存しました: {FilePath}", this.ConfigFilePath);
		} catch (Exception e) {
			this._logger.LogError(e, "アプリケーション設定ファイルの保存に失敗しました: {FilePath}", this.ConfigFilePath);
			var message = this._stringProvider?.GetString("ConfigStore_SaveError_Message") ?? "アプリケーション設定を保存できません。";
			var title = this._stringProvider?.GetString("ConfigStore_SaveError_Title") ?? "保存エラー";
			this._notificationDispatcher.Notify.OnNext(
				AppNotification.Error(message, title));
		}
	}
}