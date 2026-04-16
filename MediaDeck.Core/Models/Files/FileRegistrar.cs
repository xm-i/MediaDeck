using System.Collections.Concurrent;

using MediaDeck.Composition.Interfaces.FileTypes;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Core.Models.NotificationDispatcher;

using Microsoft.Extensions.Logging;

namespace MediaDeck.Core.Models.Files;

[Inject(InjectServiceLifetime.Singleton)]
public class FileRegistrar {
	private readonly IFileOperator[] _fileOperators;
	private readonly ConcurrentDictionary<string, FolderModel> _fileToFolderMap = new();
	private readonly ILogger<FileRegistrar> _logger;
	private readonly IFilePathService _filePathService;

	public ObservableQueue<string> RegistrationQueue {
		get;
	} = [];

	public ConfigModel Config {
		get;
	}

	public FileRegistrar(ConfigModel config, ILogger<FileRegistrar> logger, IFilePathService filePathService, IFileTypeService fileTypeService) {
		this.Config = config;
		this._logger = logger;
		this._filePathService = filePathService;
		this._fileOperators = fileTypeService.CreateFileOperators();
		this.RegistrationQueue
			.ObserveAdd()
			.ThrottleFirst(TimeSpan.FromSeconds(0.1))
			.ObserveOnThreadPool()
			.SubscribeAwait(async (x, ct) =>
					await this.RegisterFilesAsync().ConfigureAwait(false),
				AwaitOperation.Sequential,
				false);
	}


	public async Task ScanFolderAsync(FolderModel folder) {
		folder.IsScanning.Value = true;

		var files = await Task.Run(() =>
			Directory.EnumerateFiles(folder.FolderPath, "*", SearchOption.AllDirectories)
				.Where(x => this._filePathService.IsTargetFile(x))
				.ToList());

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
				var type = this._filePathService.GetMediaType(filePath);
				var fileOperator = this._fileOperators.First(x => x.TargetMediaType == type);
				var mf = await fileOperator.RegisterFileAsync(filePath).ConfigureAwait(false);
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