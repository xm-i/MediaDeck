using System.IO;
using System.Threading.Tasks;

using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.Notifications;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.MediaItemTypes.Base;
using MediaDeck.MediaItemTypes.FolderGroup.Models;

using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.MediaItemTypes.FolderGroup;

[Inject(InjectServiceLifetime.Singleton, typeof(IMediaItemTypeProvider))]
public class FolderGroupMediaItemTypeProvider : BaseMediaItemTypeProvider {
	private readonly IServiceProvider _serviceProvider;

	public FolderGroupMediaItemTypeProvider(ConfigModel config, IServiceProvider serviceProvider) : base(config, MediaType.FolderGroup) {
		this._serviceProvider = serviceProvider;
	}

	public override bool IsTargetPath(string path) {
		return Directory.Exists(path);
	}

	public override MediaItemPathStatus GetPathStatus(string path) {
		var directoryInfo = new DirectoryInfo(path);
		if (!directoryInfo.Exists) {
			return new(false, 0, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue);
		}

		return new(true, 0, directoryInfo.CreationTime, directoryInfo.LastWriteTime, directoryInfo.LastAccessTime);
	}


	/// <summary>
	/// フォルダグループの実行処理。
	/// 設定に応じて外部プログラムを起動するか、MediaDeck内でそのフォルダを開く。
	/// </summary>
	/// <param name="filePath">実行ファイルパス</param>
	/// <param name="scopedServiceProvider">実行するタブのスコープを切ったサービスプロバイダー</param>
	public override Task ExecuteAsync(string filePath, IServiceProvider scopedServiceProvider) {
		var epo = this._config.ExecutionConfig.ExecutionPrograms.FirstOrDefault(x => x.MediaType == this.MediaType) as FolderGroupExecutionProgramObjectModel;

		// 実行方法が Internal の場合
		if (epo?.ExecutionType.Value == ExecutionType.Internal) {
			// 現在のフォルダ条件を削除し、対象のパスを検索トークンとして追加する
			var searchDispatcher = scopedServiceProvider.GetRequiredService<ISearchConditionNotificationDispatcher>();
			searchDispatcher.UpdateRequest.OnNext(conditions => {
				conditions.RemoveRange(conditions.Where(x => x is IFolderSearchCondition));

				// IFolderSearchCondition をサービスプロバイダーから取得して設定する
				var folderCondition = this._serviceProvider.GetRequiredService<IFolderSearchCondition>();
				folderCondition.FolderPath = filePath;
				folderCondition.IncludeSubDirectories = true;

				conditions.Add(folderCondition);
			});
			return Task.CompletedTask;
		}

		// それ以外（External または設定なし）はベースクラスのロジック（外部起動）を使用
		return base.ExecuteAsync(filePath, scopedServiceProvider);
	}

	public override IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems) {
		return MediaItems.Include(mf => mf.FolderGroupMetadata);
	}
}