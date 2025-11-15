using System.Threading.Tasks;

using MediaDeck.FileTypes.Base.Models.Interfaces;
using MediaDeck.Utils.Notifications;
using MediaDeck.Composition.Stores.Config.Model;

namespace MediaDeck.Models.Files;

[AddSingleton]
public class FileRegistrar {
	private static readonly IFileOperator[] _fileOperators;
	public ObservableQueue<string> RegistrationQueue {
		get;
	} = [];

	public ReactiveProperty<long> QueueCount {
		get;
	} = new();

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

		this.RegistrationQueue.ObserveCountChanged(true).ThrottleLast(TimeSpan.FromSeconds(0.1)).Subscribe(x => {
			this.QueueCount.Value = x;
		});
	}

	public async Task RegisterFilesAsync() {
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
			}
		}
	}
}
