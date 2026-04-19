using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using AutoDiAttributes;

using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Objects;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Core.Models.NotificationDispatcher;
using MediaDeck.Core.Stores.Config;
using MediaDeck.Stores.SerializerContext;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MediaDeck.Store.Config;

[Inject(InjectServiceLifetime.Singleton, typeof(IConfigStore))]
public class ConfigStore : IConfigStore {
	private readonly ILogger<ConfigStore> _logger;
	private readonly AppNotificationDispatcher _notificationDispatcher;
	private readonly IAppPathProvider _pathProvider;
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

	public ConfigStore(IServiceProvider service, IAppPathProvider pathProvider) {
		this.ScopedService = service;
		this._pathProvider = pathProvider;
		this._logger = service.GetRequiredService<ILogger<ConfigStore>>();
		this._notificationDispatcher = service.GetRequiredService<AppNotificationDispatcher>();
		this.Load();
	}

	/// <summary>
	///     保存済み設定を読み込みます。
	/// </summary>
	[MemberNotNull(nameof(Config))]
	public void Load() {
		var scope = this.ScopedService.CreateScope();
		try {
			if (File.Exists(this.ConfigFilePath)) {
				try {
					var json = File.ReadAllText(this.ConfigFilePath);
					var loaded = JsonSerializer.Deserialize(json, ConfigJsonSerializerContext.Default.ConfigModelForJson);
					if (loaded != null) {
						this.Config = ConfigModelForJson.CreateModel(loaded, scope.ServiceProvider);
						this._logger.LogInformation("アプリケーション設定の読み込みに成功しました");
						return;
					}
				} catch (Exception e) {
					this._logger.LogError(e, "アプリケーション設定ファイルのJSON解析に失敗しました: {FilePath}", this.ConfigFilePath);
					this._notificationDispatcher.Notify.OnNext(
						AppNotification.Error("アプリケーション設定ファイルが破損しています。デフォルト設定を使用します。", "設定読み込みエラー", 0));
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
			var json = JsonSerializer.Serialize(jsonDto, ConfigJsonSerializerContext.Default.ConfigModelForJson);
			File.WriteAllText(this.ConfigFilePath, json);
			this._logger.LogInformation("アプリケーション設定を保存しました: {FilePath}", this.ConfigFilePath);
		} catch (Exception e) {
			this._logger.LogError(e, "アプリケーション設定ファイルの保存に失敗しました: {FilePath}", this.ConfigFilePath);
			this._notificationDispatcher.Notify.OnNext(
				AppNotification.Error("アプリケーション設定を保存できません。", "保存エラー"));
		}
	}
}