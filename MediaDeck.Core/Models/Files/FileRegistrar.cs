using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using MediaDeck.Common.Base;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.Config.Model.Objects;
using MediaDeck.Core.Models.NotificationDispatcher;

using Microsoft.Extensions.Logging;

namespace MediaDeck.Core.Models.Files;

/// <summary>
/// メディアファイルの登録キューおよびフォルダー走査を管理するサービスです。
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
public class FileRegistrar : ServiceBase {
	private readonly IMediaItemOperator[] _fileOperators;
	private readonly ConcurrentDictionary<string, FolderModel> _fileToFolderMap = new();
	private readonly ILogger<FileRegistrar> _logger;
	private readonly IMediaItemTypeService _mediaItemTypeService;
	private readonly Lock _queueLock = new();

	/// <summary>
	/// ファイル登録キュー
	/// </summary>
	public ObservableQueue<string> RegistrationQueue {
		get;
	} = [];

	/// <summary>
	/// 設定モデル
	/// </summary>
	public ConfigModel Config {
		get;
	}

	/// <summary>
	/// FileRegistrarクラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="config">設定モデル</param>
	/// <param name="logger">ロガー</param>
	/// <param name="mediaItemTypeService">メディアアイテム種別サービス</param>
	public FileRegistrar(ConfigModel config, ILogger<FileRegistrar> logger, IMediaItemTypeService mediaItemTypeService) {
		this.Config = config;
		this._logger = logger;
		this._mediaItemTypeService = mediaItemTypeService;
		this._fileOperators = mediaItemTypeService.CreateMediaItemOperators();
		this.RegistrationQueue
			.ObserveAdd()
			.ThrottleFirst(TimeSpan.FromSeconds(0.1))
			.ObserveOnThreadPool()
			.SubscribeAwait(async (x, ct) =>
					await this.RegisterFilesAsync().ConfigureAwait(false),
				AwaitOperation.Sequential,
				false)
			.AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// ファイルパスを登録キューに追加します。
	/// </summary>
	/// <param name="filePath">追加するファイルパス</param>
	public void Enqueue(string filePath) {
		lock (this._queueLock) {
			this.RegistrationQueue.Enqueue(filePath);
		}
	}

	/// <summary>
	/// 複数のファイルパスを一括で登録キューに追加します。
	/// </summary>
	/// <param name="filePaths">追加するファイルパス一覧</param>
	public void EnqueueRange(IEnumerable<string> filePaths) {
		lock (this._queueLock) {
			this.RegistrationQueue.EnqueueRange(filePaths);
		}
	}

	/// <summary>
	/// 登録キューから次のファイルパスを取り出します。
	/// </summary>
	/// <param name="filePath">取り出されたファイルパス</param>
	/// <returns>取り出しに成功した場合は true、キューが空の場合は false</returns>
	public bool TryDequeue([NotNullWhen(true)] out string? filePath) {
		lock (this._queueLock) {
			return this.RegistrationQueue.TryDequeue(out filePath);
		}
	}

	/// <summary>
	/// 指定されたフォルダーを非同期で走査し、検出されたメディアファイルを登録キューに追加します。
	/// </summary>
	/// <param name="folder">スキャン対象のフォルダーモデル</param>
	public async Task ScanFolderAsync(FolderModel folder) {
		folder.IsScanning.Value = true;
		folder.RemainingCount.Value = 0;
		folder.TotalCount.Value = 0;
		var enqueued = false;

		try {
			var files = await Task.Run(() => {
				var fileEnumOptions = new EnumerationOptions {
					IgnoreInaccessible = true,
					RecurseSubdirectories = true,
				};
				var targets = Directory
					.EnumerateFiles(folder.FolderPath, "*", fileEnumOptions)
					.Where(x => this._mediaItemTypeService.IsTargetPath(x))
					.ToList();

				if (folder.IsGroupingRoot) {
					var dirEnumOptions = new EnumerationOptions {
						IgnoreInaccessible = true,
						RecurseSubdirectories = false,
					};
					targets.AddRange(Directory.EnumerateDirectories(folder.FolderPath, "*", dirEnumOptions));
				}

				return targets
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.ToList();
			}).ConfigureAwait(false);

			folder.RemainingCount.Value = files.Count;
			folder.TotalCount.Value = files.Count;

			if (files.Count == 0) {
				folder.IsScanning.Value = false;
				enqueued = true;
				return;
			}

			foreach (var file in files) {
				this._fileToFolderMap[file] = folder;
			}

			this.EnqueueRange(files);
			enqueued = true;
		} catch (Exception ex) {
			this._logger.LogError(ex, "Failed to scan folder: {FolderPath}", folder.FolderPath);
			throw;
		} finally {
			if (!enqueued) {
				folder.IsScanning.Value = false;
			}
		}
	}

	/// <summary>
	/// 登録キューから順次ファイルを取り出し、メディアアイテムとしての登録処理を実行します。
	/// </summary>
	private async Task RegisterFilesAsync() {
		while (this.TryDequeue(out var filePath)) {
			try {
				var mediaItemFactory = this._mediaItemTypeService.GetMediaItemFactory(filePath);
				if (mediaItemFactory.MediaType == MediaType.Unknown) {
					continue;
				}

				var fileOperator = this._fileOperators.First(x => x.TargetMediaType == mediaItemFactory.MediaType);
				var mf = await fileOperator.RegisterMediaItemAsync(filePath).ConfigureAwait(false);
				if (mf is { } mf2) {
					FileNotifications.FileRegistered.OnNext(mf2);
				}
			} catch (Exception e) {
				this._logger.LogError(e, "Error while registering file: {FilePath}", filePath);
			} finally {
				if (this._fileToFolderMap.TryRemove(filePath, out var folder)) {
					folder.RemainingCount.Value--;
					if (folder.RemainingCount.Value <= 0) {
						folder.IsScanning.Value = false;
					}
				}
			}
		}
	}
}