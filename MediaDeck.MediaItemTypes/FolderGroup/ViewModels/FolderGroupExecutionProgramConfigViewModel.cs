using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.FolderGroup.Models;

namespace MediaDeck.MediaItemTypes.FolderGroup.ViewModels;

/// <summary>
/// フォルダグループ固有の実行設定用 ViewModel。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class FolderGroupExecutionProgramConfigViewModel : DefaultExecutionProgramConfigViewModel, IExecutionProgramConfigViewModel {
	public ExecutionType[] ExecutionTypeConditions { get; } = Enum.GetValues<ExecutionType>();

	// 固有プロパティを ViewModel 直下で公開
	public BindableReactiveProperty<ExecutionType> ExecutionType {
		get;
		private set;
	} = null!;

	public FolderGroupExecutionProgramConfigViewModel(IMediaItemTypeService mediaItemTypeService, ExecutionConfigModel executionConfig)
		: base(mediaItemTypeService, executionConfig) {
	}

	public void Initialize(FolderGroupExecutionProgramObjectModel model) {
		base.Initialize(model);
		this.ExecutionType = model.ExecutionType.ToTwoWayBindableReactiveProperty(Composition.Enum.ExecutionType.External, this.CompositeDisposable).AddTo(this.CompositeDisposable);
	}
}