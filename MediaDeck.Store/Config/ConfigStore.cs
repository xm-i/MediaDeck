using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using AutoDiAttributes;

using MediaDeck.Composition.Constants;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Core.Stores.Config;
using MediaDeck.Stores.SerializerContext;

using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.Store.Config;

[Inject(InjectServiceLifetime.Singleton, typeof(IConfigStore))]
public class ConfigStore : IConfigStore {
	public IServiceProvider ScopedService {
		get;
	}

	public ConfigModel Config {
		get;
		private set;
	}

	// Protected property to allow overriding the file path during testing.
	protected virtual string ConfigFilePath {
		get {
			return FilePathConstants.ConfigFilePath;
		}
	}

	public ConfigStore(IServiceProvider service) {
		this.ScopedService = service;
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
				var json = File.ReadAllText(this.ConfigFilePath);
				var loaded = JsonSerializer.Deserialize(json, ConfigJsonSerializerContext.Default.ConfigModelForJson);
				if (loaded != null) {
					this.Config = ConfigModelForJson.CreateModel(loaded, scope.ServiceProvider);
					return;
				}
			}
		} catch (Exception) {
			// TODO: 失敗通知
		}
		this.Config = scope.ServiceProvider.GetRequiredService<ConfigModel>();
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
		} catch (Exception) {
			// TODO: 失敗通知
		}
	}
}