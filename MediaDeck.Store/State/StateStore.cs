using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using AutoDiAttributes;
using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Objects;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.NotificationDispatcher;
using MediaDeck.Core.Stores.State;
using MediaDeck.Store.SerializerContext;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using R3;

namespace MediaDeck.Store.State;

[Inject(InjectServiceLifetime.Singleton, typeof(IStateStore))]
public class StateStore : DisposableBase, IStateStore {
	private readonly ILogger<StateStore> _logger;
	private readonly AppNotificationDispatcher _notificationDispatcher;
	private readonly IAppPathProvider _pathProvider;
	private readonly IStringProvider? _stringProvider;

	public IServiceProvider ScopedService {
		get;
	}

	public RootStateModel RootState {
		get;
		private set;
	}

	protected virtual string StateFilePath {
		get {
			return this._pathProvider.StateFilePath;
		}
	}

	/// <summary>
	/// <see cref="StateStore"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="service">サービスプロバイダー</param>
	/// <param name="pathProvider">アプリケーションパスプロバイダー</param>
	/// <param name="stringProvider">文字列プロバイダー（省略可能）</param>
	public StateStore(IServiceProvider service, IAppPathProvider pathProvider, IStringProvider? stringProvider = null) {
		this.ScopedService = service;
		this._pathProvider = pathProvider;
		this._stringProvider = stringProvider ?? service.GetService<IStringProvider>();
		this._logger = service.GetRequiredService<ILogger<StateStore>>();
		this._notificationDispatcher = service.GetRequiredService<AppNotificationDispatcher>();
		this.Load();
	}

	public JsonSerializerOptions JsonSerializerOptions {
		get {
			return field ??= new JsonSerializerOptions() {
				WriteIndented = true,
				TypeInfoResolver = StateJsonSerializerContext.Default.WithAddedModifier(global::GenJsonConfig.ForJsonConverterRegistry.ApplyPolymorphism)
			};
		}
	}

	/// <summary>
	///     保存済み設定を読み込みます。
	/// </summary>
	[MemberNotNull(nameof(RootState))]
	public void Load() {
		var scope = this.ScopedService.CreateScope().AddTo(this.CompositeDisposable);
		try {
			if (File.Exists(this.StateFilePath)) {
				try {
					var json = File.ReadAllText(this.StateFilePath);
					this._logger.LogDebug("状態設定JSONを読み込みました。Length: {Length}", json.Length);
					var loaded = JsonSerializer.Deserialize<RootStateModelForJson>(json, this.JsonSerializerOptions);
					if (loaded != null) {
						this.RootState = RootStateModelForJson.CreateModel(loaded, scope.ServiceProvider);
						this._logger.LogInformation("状態設定の読み込みに成功しました");
						return;
					}
				} catch (JsonException je) {
					this._logger.LogError(je, "状態設定ファイルのJSON解析に失敗しました: {FilePath}", this.StateFilePath);
					var message = this._stringProvider?.GetString("StateStore_LoadError_Corrupted") ?? "状態設定ファイルが破損しています。デフォルト設定を使用します。";
					var title = this._stringProvider?.GetString("StateStore_LoadError_Title") ?? "設定読み込みエラー";
					this._notificationDispatcher.Notify.OnNext(
						AppNotification.Error(message, title, 0));
				} catch (IOException ie) {
					this._logger.LogError(ie, "状態設定ファイルの読み込みに失敗しました: {FilePath}", this.StateFilePath);
					var message = this._stringProvider?.GetString("StateStore_LoadError_CannotRead") ?? "状態設定ファイルを読み込めません。デフォルト設定を使用します。";
					var title = this._stringProvider?.GetString("StateStore_LoadError_Title") ?? "設定読み込みエラー";
					this._notificationDispatcher.Notify.OnNext(
						AppNotification.Error(message, title, 0));
				} catch (Exception ex) {
					this._logger.LogError(ex, "状態設定の読み込み処理中に予期しないエラーが発生しました: {FilePath}", this.StateFilePath);
					var message = this._stringProvider?.GetString("StateStore_LoadError_Unexpected") ?? "状態設定の読み込み中にエラーが発生しました。デフォルト設定を使用します。";
					var title = this._stringProvider?.GetString("StateStore_LoadError_Title") ?? "設定読み込みエラー";
					this._notificationDispatcher.Notify.OnNext(
						AppNotification.Error(message, title, 0));
				}
			}
		} catch (Exception ex) {
			this._logger.LogError(ex, "状態設定読み込みのスコープ生成に失敗しました");
		}

		// デフォルト状態を作成
		try {
			var appState = scope.ServiceProvider.GetRequiredService<AppStateModel>();
			this.RootState = new RootStateModel(appState);
			this._logger.LogWarning("デフォルトの状態設定を使用します");
		} catch (Exception ex) {
			this._logger.LogError(ex, "デフォルト状態設定の作成に失敗しました");
			throw;
		}
	}

	/// <summary>
	///     現在の設定をファイルへ保存します。
	/// </summary>
	public void Save() {
		try {
			Directory.CreateDirectory(Path.GetDirectoryName(this.StateFilePath)!);

			var jsonDto = RootStateModelForJson.CreateJson(this.RootState);
			var json = JsonSerializer.Serialize(jsonDto, this.JsonSerializerOptions);
			this._logger.LogDebug("状態設定JSONを生成しました。Length: {Length}", json.Length);
			File.WriteAllText(this.StateFilePath, json);
			this._logger.LogInformation("状態設定を保存しました: {FilePath}", this.StateFilePath);
		} catch (IOException ie) {
			this._logger.LogError(ie, "状態設定ファイルの保存に失敗しました: {FilePath}", this.StateFilePath);
			var message = this._stringProvider?.GetString("StateStore_SaveError_CannotSave") ?? "状態設定を保存できません。";
			var title = this._stringProvider?.GetString("StateStore_SaveError_Title") ?? "保存エラー";
			this._notificationDispatcher.Notify.OnNext(
				AppNotification.Error(message, title));
		} catch (JsonException je) {
			this._logger.LogError(je, "状態設定のシリアライズに失敗しました");
			var message = this._stringProvider?.GetString("StateStore_SaveError_Serialize") ?? "状態設定のシリアライズに失敗しました。";
			var title = this._stringProvider?.GetString("StateStore_SaveError_Title") ?? "保存エラー";
			this._notificationDispatcher.Notify.OnNext(
				AppNotification.Error(message, title));
		} catch (Exception ex) {
			this._logger.LogError(ex, "状態設定の保存中に予期しないエラーが発生しました");
			var message = this._stringProvider?.GetString("StateStore_SaveError_Unexpected") ?? "状態設定の保存中にエラーが発生しました。";
			var title = this._stringProvider?.GetString("StateStore_SaveError_Title") ?? "保存エラー";
			this._notificationDispatcher.Notify.OnNext(
				AppNotification.Error(message, title));
		}
	}
}