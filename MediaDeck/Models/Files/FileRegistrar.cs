using System.Threading.Tasks;

using CommunityToolkit.Mvvm.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;

using MediaDeck.FileTypes.Base.Models.Interfaces;
using MediaDeck.Models.Preferences;
using MediaDeck.Utils.Notifications;

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

	public Config Config {
		get;
	}

	static FileRegistrar() {
		_fileOperators = FileTypeUtility.CreateFileOperators();
	}

	public FileRegistrar(Config config) {
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
