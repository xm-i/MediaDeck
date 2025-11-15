using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;

using MediaDeck.Composition.Constants;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Stores.SerializerContext;

using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.Stores.Config;

[AddSingleton]
public class ConfigStore {
	public IServiceProvider ScopedService {
		get;
	}

	public ConfigModel ConfigModel {
		get;
		private set;
	}

	public ConfigStore(IServiceProvider service) {
		this.ScopedService = service;
		this.Load();
	}

	/// <summary>
	///     保存済み設定を読み込みます。
	/// </summary>
	[MemberNotNull(nameof(ConfigModel))]
	public void Load() {
		var scope = this.ScopedService.CreateScope();
		try {
			if (File.Exists(FilePathConstants.ConfigFilePath)) {
				var json = File.ReadAllText(FilePathConstants.ConfigFilePath);
				var loaded = JsonSerializer.Deserialize(json, ConfigJsonSerializerContext.Default.ConfigModelForJson);
				if (loaded != null) {
					this.ConfigModel = ConfigModelForJson.CreateModel(loaded, scope.ServiceProvider);
					return;
				}
			}
		} catch (Exception) {
			// TODO: 失敗通知
		}
		this.ConfigModel = scope.ServiceProvider.GetRequiredService<ConfigModel>();
	}

	/// <summary>
	///     現在の設定をファイルへ保存します。
	/// </summary>
	public void Save() {
		try {
			Directory.CreateDirectory(Path.GetDirectoryName(FilePathConstants.ConfigFilePath)!);

			var jsonDto = ConfigModelForJson.CreateJson(this.ConfigModel);
			var json = JsonSerializer.Serialize(jsonDto, ConfigJsonSerializerContext.Default.ConfigModelForJson);
			File.WriteAllText(FilePathConstants.ConfigFilePath, json);
		} catch (Exception) {
			// TODO: 失敗通知
		}
	}
}

