using System.Collections.Concurrent;

using MediaDeck.Common.Base;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Core.Models.NotificationDispatcher;

using Microsoft.Extensions.Logging;

namespace MediaDeck.Core.Models.Files;

[Inject(InjectServiceLifetime.Singleton)]
public class FileRegistrar : ServiceBase {
	private readonly IMediaItemOperator[] _fileOperators;
	private readonly ConcurrentDictionary<string, FolderModel> _fileToFolderMap = new();
	private readonly ILogger<FileRegistrar> _logger;
	private readonly IMediaItemTypeService _mediaItemTypeService;

	public ObservableQueue<string> RegistrationQueue {
		get;
	} = [];

	public ConfigModel Config {
		get;
	}

	public FileRegistrar(ConfigModel config, ILogger<FileRegistrar> logger, IMediaItemTypeService MediaItemTypeService) {
		this.Config = config;
		this._logger = logger;
		this._mediaItemTypeService = MediaItemTypeService;
		this._fileOperators = MediaItemTypeService.CreateMediaItemOperators();
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


	public async Task ScanFolderAsync(FolderModel folder) {
		folder.IsScanning.Value = true;

		var files = await Task.Run(() => {
			var targets = Directory
				.EnumerateFiles(folder.FolderPath, "*", SearchOption.AllDirectories)
				.Where(x => this._mediaItemTypeService.IsTargetPath(x))
				.ToList();

			if (folder.IsGroupingRoot) {
				targets.AddRange(Directory.EnumerateDirectories(folder.FolderPath, "*", SearchOption.TopDirectoryOnly));
			}

			return targets
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
		});

		folder.TotalCount.Value = files.Count;
		folder.RemainingCount.Value = files.Count;

		if (files.Count == 0) {
			folder.IsScanning.Value = false;
			return;
		}

		foreach (var file in files) {
			this._fileToFolderMap[file] = folder;
		}

		this.RegistrationQueue.EnqueueRange(files);
	}

	private async Task RegisterFilesAsync() {
		while (this.RegistrationQueue.TryDequeue(out var filePath)) {
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