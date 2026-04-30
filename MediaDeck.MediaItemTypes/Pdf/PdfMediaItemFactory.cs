using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Tables;
using MediaDeck.MediaItemTypes.Base;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.Pdf.Models;
using MediaDeck.MediaItemTypes.Pdf.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.MediaItemTypes.Pdf;

[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactory))]
[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactoryOf<PdfMediaItemViewModel>))]
public class PdfMediaItemFactory :
	BaseMediaItemFactory<PdfMediaItemOperator, PdfMediaItemModel, DefaultExecutionProgramObjectModel, PdfMediaItemViewModel, DefaultExecutionProgramConfigViewModel, PdfThumbnailPickerViewModel>,
	IMediaItemFactory<PdfMediaItemOperator, PdfMediaItemModel, DefaultExecutionProgramObjectModel, PdfMediaItemViewModel, DefaultExecutionProgramConfigViewModel, PdfThumbnailPickerViewModel>,
	IMediaItemFactoryOf<PdfMediaItemViewModel> {
	private readonly PdfMediaItemOperator _PdfMediaItemOperator;
	private readonly IServiceProvider _serviceProvider;

	public PdfMediaItemFactory(
		PdfMediaItemOperator PdfMediaItemOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IServiceProvider serviceProvider)
		: base(config, tagsManager, MediaType.Pdf) {
		this._PdfMediaItemOperator = PdfMediaItemOperator;
		this._serviceProvider = serviceProvider;
	}

	public override PdfMediaItemOperator CreateMediaItemOperator() {
		return this._PdfMediaItemOperator;
	}

	public override ItemType ItemType {
		get {
			return ItemType.Pdf;
		}
	}

	public override PdfMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem) {
		var ifm = this._serviceProvider.GetRequiredService<PdfMediaItemModel>();
		ifm.Initialize(MediaItem.MediaItemId, MediaItem.FilePath);
		this.SetModelProperties(ifm, MediaItem);
		return ifm;
	}

	public override PdfMediaItemViewModel CreateMediaItemViewModel(PdfMediaItemModel fileModel) {
		var vm = this._serviceProvider.GetRequiredService<PdfMediaItemViewModel>();
		vm.Initialize(fileModel);
		return vm;
	}

	public override PdfThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<PdfThumbnailPickerViewModel>();
	}

	public override DefaultExecutionProgramObjectModel CreateExecutionProgramObjectModel() {
		var model = this._serviceProvider.GetRequiredService<DefaultExecutionProgramObjectModel>();
		model.MediaType = this.MediaType;
		return model;
	}

	public override DefaultExecutionProgramConfigViewModel CreateExecutionProgramConfigViewModel(DefaultExecutionProgramObjectModel model) {
		var vm = this._serviceProvider.GetRequiredService<DefaultExecutionProgramConfigViewModel>();
		vm.Initialize(model);
		return vm;
	}

	public override IBulkThumbnailConfigViewModel CreateBulkThumbnailConfigViewModel() {
		return this._serviceProvider.GetRequiredService<PdfBulkThumbnailConfigViewModel>();
	}
}