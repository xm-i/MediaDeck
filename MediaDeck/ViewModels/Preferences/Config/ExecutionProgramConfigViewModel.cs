using MediaDeck.Composition.Bases;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.Config.Model.Objects;

namespace MediaDeck.ViewModels.Preferences.Config;

[Inject(InjectServiceLifetime.Scoped)]
public class ExecutionProgramConfigViewModel: ViewModelBase {
	public BindableReactiveProperty<string> Path {
		get;
		set;
	}

	public BindableReactiveProperty<string> Args {
		get;
		set;
	}

	public BindableReactiveProperty<MediaType> MediaType {
		get;
		set;
	}

	public ExecutionProgramConfigViewModel(ExecutionProgramObjectModel executionProgramConfigModel) {
		this.Path = executionProgramConfigModel.Path.ToTwoWayBindableReactiveProperty(string.Empty).AddTo(this.CompositeDisposable);
		this.Args = executionProgramConfigModel.Args.ToTwoWayBindableReactiveProperty(string.Empty).AddTo(this.CompositeDisposable);
		this.MediaType = executionProgramConfigModel.MediaType.ToTwoWayBindableReactiveProperty(Composition.Enum.MediaType.Image).AddTo(this.CompositeDisposable);

	}
}
