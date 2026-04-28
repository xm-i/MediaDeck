using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base;
using MediaDeck.MediaItemTypes.UI.Base.Views;
using MediaDeck.MediaItemTypes.UI.Unknown.Views;
using MediaDeck.MediaItemTypes.Unknown.Models;
using MediaDeck.MediaItemTypes.Unknown.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.MediaItemTypes.UI.Unknown;

[Inject(InjectServiceLifetime.Singleton, typeof(IMediaItemFactory))]
public class UnknownMediaItemFactory : BaseMediaItemFactory<UnknownMediaItemOperator, UnknownMediaItemModel, DefaultExecutionProgramObjectModel, UnknownMediaItemViewModel, DefaultExecutionProgramConfigViewModel, UnknownDetailViewerPreviewControlView, UnknownThumbnailPickerViewModel, UnknownThumbnailPickerView, DefaultExecutionConfigView> {
	private UnknownDetailViewerPreviewControlView? _unknownDetailViewerPreviewControlView;
	private readonly UnknownMediaItemOperator _UnknownMediaItemOperator;
	private readonly IServiceProvider _serviceProvider;

	public UnknownMediaItemFactory(
		UnknownMediaItemOperator UnknownMediaItemOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IServiceProvider serviceProvider)
		: base(config, tagsManager, MediaType.Unknown) {
		this._UnknownMediaItemOperator = UnknownMediaItemOperator;
		this._serviceProvider = serviceProvider;
	}

	public override UnknownMediaItemOperator CreateMediaItemOperator() {
		return this._UnknownMediaItemOperator;
	}

	public override ItemType ItemType {
		get {
			return ItemType.Unknown;
		}
	}

	public override bool IsTargetPath(string path) {
		return false;
	}

	public override UnknownMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem, IServiceProvider scopedServiceProvider) {
		var ifm = new UnknownMediaItemModel(MediaItem.MediaItemId, MediaItem.FilePath, this.CreateMediaItemOperator(), this, scopedServiceProvider);
		this.SetModelProperties(ifm, MediaItem);
		return ifm;
	}

	public override UnknownMediaItemViewModel CreateMediaItemViewModel(UnknownMediaItemModel fileModel) {
		return new UnknownMediaItemViewModel(fileModel, this);
	}

	public override UnknownDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(UnknownMediaItemViewModel fileViewModel) {
		return this._unknownDetailViewerPreviewControlView ??= new UnknownDetailViewerPreviewControlView();
	}

	public override UnknownThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<UnknownThumbnailPickerViewModel>();
	}

	public override IThumbnailControlView CreateThumbnailControlView(UnknownMediaItemViewModel fileViewModel) {
		return new UnknownThumbnailControlView { DataContext = fileViewModel };
	}

	public override UnknownThumbnailPickerView CreateThumbnailPickerView() {
		return new UnknownThumbnailPickerView();
	}


	public override DefaultExecutionProgramObjectModel CreateExecutionProgramObjectModel() {
		return new DefaultExecutionProgramObjectModel() {
			MediaType = this.MediaType
		};
	}

	public override DefaultExecutionProgramConfigViewModel CreateExecutionProgramConfigViewModel(DefaultExecutionProgramObjectModel model) {
		return new DefaultExecutionProgramConfigViewModel(model, this._serviceProvider.GetRequiredService<IMediaItemTypeService>(), this._serviceProvider.GetRequiredService<ExecutionConfigModel>());
	}

	public override DefaultExecutionConfigView CreateExecutionConfigView(DefaultExecutionProgramConfigViewModel viewModel) {
		return new DefaultExecutionConfigView() {
			ViewModel = viewModel
		};
	}
	public override IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems) {
		return MediaItems;
	}
}