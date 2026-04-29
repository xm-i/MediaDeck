using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.Base.ViewModels;

/// <summary>
/// 標準的な（パスと引数）実行設定用の ViewModel。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class DefaultExecutionProgramConfigViewModel : ViewModelBase, IExecutionProgramConfigViewModel {
	private readonly IMediaItemTypeService _mediaItemTypeService;
	private readonly ExecutionConfigModel _executionConfig;

	public MediaType MediaType {
		get;
		private set;
	}

	private BindableReactiveProperty<IExecutionConfigView?>? _configView;
	public BindableReactiveProperty<IExecutionConfigView?> ConfigView {
		get {
			return this._configView ?? throw this.CreateNotInitializedException();
		}
	}

	private BindableReactiveProperty<string>? _path;
	public BindableReactiveProperty<string> Path {
		get {
			return this._path ?? throw this.CreateNotInitializedException();
		}
	}

	private BindableReactiveProperty<string>? _args;
	public BindableReactiveProperty<string> Args {
		get {
			return this._args ?? throw this.CreateNotInitializedException();
		}
	}

	public ReactiveCommand RemoveCommand {
		get;
	} = new();

	public DefaultExecutionProgramConfigViewModel(IMediaItemTypeService mediaItemTypeService, ExecutionConfigModel executionConfig) {
		this._mediaItemTypeService = mediaItemTypeService;
		this._executionConfig = executionConfig;
	}

	public void Initialize(DefaultExecutionProgramObjectModel model) {
		this.MediaType = model.MediaType;
		this._path = model.Path.ToTwoWayBindableReactiveProperty(string.Empty, this.CompositeDisposable).AddTo(this.CompositeDisposable);
		this._args = model.Args.ToTwoWayBindableReactiveProperty(string.Empty, this.CompositeDisposable).AddTo(this.CompositeDisposable);

		this._configView = new BindableReactiveProperty<IExecutionConfigView?>(this._mediaItemTypeService.CreateExecutionConfigView(this));

		this.RemoveCommand.Subscribe(_ => {
			this._executionConfig.RemoveExecutionProgram(model);
		}).AddTo(this.CompositeDisposable);
	}

	private InvalidOperationException CreateNotInitializedException() {
		return new($"{this.GetType().Name} is not initialized.");
	}
}