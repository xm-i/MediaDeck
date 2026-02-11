using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using MediaDeck.FileTypes.Base.Models.Interfaces;
using MediaDeck.Utils.Notifications;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.State.Model.Objects;

namespace MediaDeck.Models.Files;

[Inject(InjectServiceLifetime.Singleton)]
public class FileRegistrar {
	private static readonly IFileOperator[] _fileOperators;
	private readonly ConcurrentDictionary<string, FolderModel> _fileToFolderMap = new();

	public ObservableQueue<string> RegistrationQueue {
		get;
	} = [];

	public ConfigModel Config {
		get;
	}

	static FileRegistrar() {
		_fileOperators = FileTypeUtility.CreateFileOperators();
	}

	public FileRegistrar(ConfigModel config) {
		this.Config = config;
		this.RegistrationQueue
			.ObserveAdd()
			.ThrottleFirst(TimeSpan.FromSeconds(0.1))
			.Synchronize()
			.Subscribe(async _ => {
				await this.RegisterFilesAsync();
			});
	}

	public async Task ScanFolderAsync(FolderModel folder) {
		folder.IsScanning.Value = true;

		var files = await Task.Run(() =>
			Directory.EnumerateFiles(folder.FolderPath, "*", SearchOption.AllDirectories)
				.Where(x => x.IsTargetFile())
				.ToList()
		);

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
				var type = filePath.GetMediaType();
				var fileOperator = _fileOperators.First(x => x.TargetMediaType == type);
				var mf = await fileOperator.RegisterFileAsync(filePath);
				if (mf is { } mf2) {
					FileNotifications.FileRegistered.OnNext(mf2);
				}
			} catch (Exception e) {
				Console.WriteLine(e);
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
