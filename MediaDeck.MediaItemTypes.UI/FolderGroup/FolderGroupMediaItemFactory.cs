using System.IO;
using System.Threading.Tasks;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.Composition.Interfaces.Notifications;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.MediaItemTypes.FolderGroup.Models;
using MediaDeck.MediaItemTypes.FolderGroup.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base;
using MediaDeck.MediaItemTypes.UI.FolderGroup.Views;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.MediaItemTypes.UI.FolderGroup;

[Inject(InjectServiceLifetime.Singleton, typeof(IMediaItemFactory))]
public class FolderGroupMediaItemFactory : BaseMediaItemFactory<FolderGroupMediaItemOperator, FolderGroupMediaItemModel, FolderGroupExecutionProgramObjectModel, FolderGroupMediaItemViewModel, FolderGroupExecutionProgramConfigViewModel, FolderGroupDetailViewerPreviewControlView, FolderGroupThumbnailPickerViewModel, FolderGroupThumbnailPickerView, FolderGroupExecutionConfigView> {
	private FolderGroupDetailViewerPreviewControlView? _detailViewerPreviewControlView;
	private readonly FolderGroupMediaItemOperator _fileOperator;
	private readonly IServiceProvider _serviceProvider;

	public FolderGroupMediaItemFactory(
		FolderGroupMediaItemOperator fileOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IServiceProvider serviceProvider)
		: base(config, tagsManager, MediaType.FolderGroup) {
		this._fileOperator = fileOperator;
		this._serviceProvider = serviceProvider;
	}

	public override FolderGroupMediaItemOperator CreateMediaItemOperator() {
		return this._fileOperator;
	}

	public override ItemType ItemType {
		get {
			return ItemType.FolderGroup;
		}
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

	/// <inheritdoc />
	public override FolderGroupExecutionProgramObjectModel CreateExecutionProgramObjectModel() {
		var model = new FolderGroupExecutionProgramObjectModel();
		return model;
	}

	/// <inheritdoc />
	public override FolderGroupExecutionProgramConfigViewModel CreateExecutionProgramConfigViewModel(FolderGroupExecutionProgramObjectModel model) {
		return new FolderGroupExecutionProgramConfigViewModel(model, this._serviceProvider.GetRequiredService<IMediaItemTypeService>(), this._serviceProvider.GetRequiredService<ExecutionConfigModel>());
	}

	/// <inheritdoc />
	public override FolderGroupExecutionConfigView CreateExecutionConfigView(FolderGroupExecutionProgramConfigViewModel viewModel) {
		return new FolderGroupExecutionConfigView {
			ViewModel = viewModel
		};
	}

	public override FolderGroupMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem, IServiceProvider scopedServiceProvider) {
		var model = new FolderGroupMediaItemModel(MediaItem.MediaItemId, MediaItem.FilePath, this._fileOperator, this, scopedServiceProvider);
		this.SetModelProperties(model, MediaItem);
		return model;
	}

	public override FolderGroupMediaItemViewModel CreateMediaItemViewModel(FolderGroupMediaItemModel fileModel) {
		return new FolderGroupMediaItemViewModel(fileModel, this);
	}

	public override FolderGroupDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(FolderGroupMediaItemViewModel fileViewModel) {
		return this._detailViewerPreviewControlView ??= new FolderGroupDetailViewerPreviewControlView();
	}

	public override FolderGroupThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<FolderGroupThumbnailPickerViewModel>();
	}

	public override IThumbnailControlView CreateThumbnailControlView(FolderGroupMediaItemViewModel fileViewModel) {
		return new FolderGroupThumbnailControlView { DataContext = fileViewModel };
	}

	public override FolderGroupThumbnailPickerView CreateThumbnailPickerView() {
		return new FolderGroupThumbnailPickerView();
	}

	public override IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems) {
		return MediaItems.Include(mf => mf.FolderGroupMetadata);
	}
}