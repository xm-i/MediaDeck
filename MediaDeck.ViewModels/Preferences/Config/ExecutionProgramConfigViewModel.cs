using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.Config.Model.Objects;

namespace MediaDeck.ViewModels.Preferences.Config;

[Inject(InjectServiceLifetime.Scoped)]
public class ExecutionProgramConfigViewModel : ViewModelBase {
	/// <summary>
	/// 実行ファイルパス
	/// </summary>
	public BindableReactiveProperty<string> Path {
		get;
		set;
	}

	/// <summary>
	/// 起動引数
	/// </summary>
	public BindableReactiveProperty<string> Args {
		get;
		set;
	}

	/// <summary>
	/// メディアタイプ
	/// </summary>
	public BindableReactiveProperty<MediaType> MediaType {
		get;
		set;
	}

	/// <summary>
	/// メディアタイプの選択肢一覧
	/// </summary>
	public MediaType[] MediaTypeConditions {
		get;
	} = Enum.GetValues<MediaType>();

	/// <summary>
	/// このプログラムを削除するコマンド
	/// </summary>
	public ReactiveCommand RemoveCommand {
		get;
	} = new();

	public ExecutionProgramConfigViewModel(ExecutionProgramObjectModel executionProgramConfigModel, ExecutionConfigModel executionConfig) {
		this.Path = executionProgramConfigModel.Path.ToTwoWayBindableReactiveProperty(string.Empty).AddTo(this.CompositeDisposable);
		this.Args = executionProgramConfigModel.Args.ToTwoWayBindableReactiveProperty(string.Empty).AddTo(this.CompositeDisposable);
		this.MediaType = executionProgramConfigModel.MediaType.ToTwoWayBindableReactiveProperty(Composition.Enum.MediaType.Image).AddTo(this.CompositeDisposable);

		this.RemoveCommand.Subscribe(_ => {
			executionConfig.RemoveExecutionProgram(executionProgramConfigModel);
		})
			.AddTo(this.CompositeDisposable);
	}
}