using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.IO;
using System;
using MediaDeck.Composition.Constants;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Stores.SerializerContext;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.Core.Stores.State;

[Inject(InjectServiceLifetime.Singleton)]
public class StateStore {
	public IServiceProvider ScopedService {
		get;
	}

	public StateModel State {
		get;
		private set;
	}

    // Protected property to allow overriding the file path during testing.
    protected virtual string StateFilePath => FilePathConstants.StateFilePath;

	public StateStore(IServiceProvider service) {
		this.ScopedService = service;
		this.Load();
	}

	/// <summary>
	///     保存済み設定を読み込みます。
	/// </summary>
	[MemberNotNull(nameof(State))]
	public void Load() {
		var scope = this.ScopedService.CreateScope();
		try {
			if (File.Exists(this.StateFilePath)) {
				var json = File.ReadAllText(this.StateFilePath);
				var loaded = JsonSerializer.Deserialize(json, StateJsonSerializerContext.Default.StateModelForJson);
				if (loaded != null) {
					this.State = StateModelForJson.CreateModel(loaded, scope.ServiceProvider);
					return;
				}
			}
		} catch (Exception) {
			// TODO: 失敗通知
		}
		this.State = scope.ServiceProvider.GetRequiredService<StateModel>();
	}

	/// <summary>
	///     現在の設定をファイルへ保存します。
	/// </summary>
	public void Save() {
		try {
			Directory.CreateDirectory(Path.GetDirectoryName(this.StateFilePath)!);

			var jsonDto = StateModelForJson.CreateJson(this.State);
			var json = JsonSerializer.Serialize(jsonDto, StateJsonSerializerContext.Default.StateModelForJson);
			File.WriteAllText(this.StateFilePath, json);
		} catch (Exception) {
			// TODO: 失敗通知
		}
	}
}
