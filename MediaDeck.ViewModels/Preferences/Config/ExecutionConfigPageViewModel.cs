using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Stores.Config.Model;

namespace MediaDeck.ViewModels.Preferences.Config;

[Inject(InjectServiceLifetime.Transient)]
public class ExecutionConfigPageViewModel : ViewModelBase, IConfigPageViewModel {
	/// <summary>
	/// ページ名
	/// </summary>
	public string PageName {
		get;
	}

	/// <summary>
	/// ページのアイコン（Segoe Fluent Icons のグリフ文字）
	/// </summary>
	public string PageIconGlyph {
		get;
	} = "\uE756";

	/// <summary>
	/// ページの説明
	/// </summary>
	public string PageDescription {
		get;
	}


	private readonly ExecutionConfigModel _executionConfig;

	public ExecutionConfigPageViewModel(ExecutionConfigModel executionConfig, IMediaItemTypeService mediaItemTypeService, IStringProvider stringProvider) {
		this._executionConfig = executionConfig;
		this.PageName = stringProvider.GetString("Config_Execution_Name");
		this.PageDescription = stringProvider.GetString("Config_Execution_Description");

		this.AvailableMediaTypes = Enum.GetValues<MediaType>().Where(x => x != MediaType.Unknown).ToArray();
		this.SelectedMediaType = new(this.AvailableMediaTypes.First());

		this.AddExecutionProgramCommand.Subscribe(_ => {
			this._executionConfig.AddExecutionProgram(this.SelectedMediaType.Value);
		})
			.AddTo(this.CompositeDisposable);

		this.ExecutionPrograms =
			this._executionConfig
				.ExecutionPrograms
				.CreateViewWithDisposal(x => mediaItemTypeService.CreateExecutionConfigViewModel(x))
				.ToNotifyCollectionChangedWithDisposal()
				.AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// 外部プログラムの一覧
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<IExecutionProgramConfigViewModel> ExecutionPrograms {
		get;
	}

	/// <summary>
	/// プログラム追加コマンド
	/// </summary>
	public ReactiveCommand AddExecutionProgramCommand {
		get;
	} = new();

	/// <summary>
	/// 選択されたメディアタイプ
	/// </summary>
	public ReactiveProperty<MediaType> SelectedMediaType {
		get;
	}

	/// <summary>
	/// 利用可能なメディアタイプの一覧
	/// </summary>
	public MediaType[] AvailableMediaTypes {
		get;
	}
}