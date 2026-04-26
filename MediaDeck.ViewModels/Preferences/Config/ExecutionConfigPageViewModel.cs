using MediaDeck.Common.Base;
using MediaDeck.Composition.Enum;
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
	} = "外部プログラム";

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
	} = "メディアを開く外部プログラムを設定します";


	private readonly ExecutionConfigModel _executionConfig;

	public ExecutionConfigPageViewModel(ExecutionConfigModel executionConfig, IMediaItemTypeService mediaItemTypeService) {
		this._executionConfig = executionConfig;

		this.AvailableMediaTypes = Enum.GetValues<MediaType>().Where(x => x != MediaType.Unknown).ToArray();
		this.SelectedMediaType = new ReactiveProperty<MediaType>(this.AvailableMediaTypes.First());

		var collectionChanged = Observable.Merge(
			this._executionConfig.ExecutionPrograms.ObserveAdd().Select(_ => Unit.Default),
			this._executionConfig.ExecutionPrograms.ObserveRemove().Select(_ => Unit.Default),
			this._executionConfig.ExecutionPrograms.ObserveReset().Select(_ => Unit.Default)
		).Prepend(Unit.Default);

		var canAdd = this.SelectedMediaType
			.CombineLatest(collectionChanged, (type, _) => !this._executionConfig.ExecutionPrograms.Any(x => x.MediaType == type));

		this.AddExecutionProgramCommand = canAdd.ToReactiveCommand();
		this.AddExecutionProgramCommand.Subscribe(_ => {
			this._executionConfig.AddExecutionProgram(this.SelectedMediaType.Value);
		})
			.AddTo(this.CompositeDisposable);

		this.ExecutionPrograms =
			this._executionConfig
				.ExecutionPrograms
				.CreateView(x => mediaItemTypeService.CreateExecutionConfigViewModel(x))
				.ToNotifyCollectionChanged();
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
	}

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