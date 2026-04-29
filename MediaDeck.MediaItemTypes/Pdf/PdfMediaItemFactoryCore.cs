using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.MediaItemTypes.Base;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.Pdf.Models;
using MediaDeck.MediaItemTypes.Pdf.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.MediaItemTypes.Pdf;

public class PdfMediaItemFactoryCore : BaseMediaItemFactoryCore<PdfMediaItemOperator, PdfMediaItemModel, DefaultExecutionProgramObjectModel, PdfMediaItemViewModel, DefaultExecutionProgramConfigViewModel, PdfThumbnailPickerViewModel> {
	private readonly PdfMediaItemOperator _PdfMediaItemOperator;
	private readonly IServiceProvider _serviceProvider;

	public PdfMediaItemFactoryCore(
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
}