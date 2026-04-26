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
public class DefaultExecutionProgramConfigViewModel : ViewModelBase, IExecutionProgramConfigViewModel {

	public MediaType MediaType {
		get;
	}
	public BindableReactiveProperty<IExecutionConfigView?> ConfigView {
		get;
	}

	public BindableReactiveProperty<string> Path {
		get;
	}
	public BindableReactiveProperty<string> Args {
		get;
	}

	public ReactiveCommand RemoveCommand {
		get;
	} = new();

	public DefaultExecutionProgramConfigViewModel(DefaultExecutionProgramObjectModel model, IMediaItemTypeService mediaItemTypeService, ExecutionConfigModel executionConfig) {
		this.MediaType = model.MediaType;
		this.Path = model.Path.ToTwoWayBindableReactiveProperty(string.Empty, this.CompositeDisposable).AddTo(this.CompositeDisposable);
		this.Args = model.Args.ToTwoWayBindableReactiveProperty(string.Empty, this.CompositeDisposable).AddTo(this.CompositeDisposable);

		this.ConfigView = new BindableReactiveProperty<IExecutionConfigView?>(mediaItemTypeService.CreateExecutionConfigView(this));

		this.RemoveCommand.Subscribe(_ => {
			executionConfig.RemoveExecutionProgram(model);
		}).AddTo(this.CompositeDisposable);
	}
}