using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.FileTypes;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Base;
using MediaDeck.FileTypes.Unknown.Models;
using MediaDeck.FileTypes.Unknown.ViewModels;
using MediaDeck.FileTypes.Unknown.Views;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.FileTypes.Unknown;

[Inject(InjectServiceLifetime.Singleton, typeof(IFileType))]
internal class UnknownFileType : BaseFileType<UnknownFileOperator, UnknownFileModel, UnknownFileViewModel, UnknownDetailViewerPreviewControlView, UnknownThumbnailPickerViewModel, UnknownThumbnailPickerView> {
	private UnknownDetailViewerPreviewControlView? _unknownDetailViewerPreviewControlView;
	private readonly UnknownFileOperator _unknownFileOperator;
	private readonly IServiceProvider _serviceProvider;

	public UnknownFileType(
		UnknownFileOperator unknownFileOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IServiceProvider serviceProvider)
		: base(config, tagsManager, MediaType.Unknown) {
		this._unknownFileOperator = unknownFileOperator;
		this._serviceProvider = serviceProvider;
	}

	public override UnknownFileOperator CreateFileOperator() {
		return this._unknownFileOperator;
	}

	public override UnknownFileModel CreateFileModelFromRecord(MediaFile mediaFile) {
		var ifm = new UnknownFileModel(mediaFile.MediaFileId, mediaFile.FilePath, this.CreateFileOperator(), this._config);
		this.SetModelProperties(ifm, mediaFile);
		return ifm;
	}

	public override UnknownFileViewModel CreateFileViewModel(UnknownFileModel fileModel) {
		return new UnknownFileViewModel(fileModel);
	}

	public override UnknownDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(UnknownFileViewModel fileViewModel) {
		return this._unknownDetailViewerPreviewControlView ??= new UnknownDetailViewerPreviewControlView();
	}

	public override UnknownThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<UnknownThumbnailPickerViewModel>();
	}

	public override UnknownThumbnailPickerView CreateThumbnailPickerView() {
		return new UnknownThumbnailPickerView();
	}

	public override IQueryable<MediaFile> IncludeTables(IQueryable<MediaFile> mediaFiles) {
		return mediaFiles;
	}
}