using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.Config.Model.Objects;
using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;

namespace MediaDeck.ViewModels.Preferences.Config;

[Inject(InjectServiceLifetime.Scoped)]
public class ExtensionConfigViewModel : ViewModelBase {
	/// <summary>
	/// 拡張子
	/// </summary>
	public BindableReactiveProperty<string> Extension {
		get;
	}

	/// <summary>
	/// メディアタイプ
	/// </summary>
	public BindableReactiveProperty<MediaType> MediaType {
		get;
	}

	/// <summary>
	/// メディアタイプの選択肢一覧
	/// </summary>
	public MediaType[] MediaTypeConditions { get; } = Enum.GetValues<MediaType>();

	/// <summary>
	/// この拡張子を削除するコマンド
	/// </summary>
	public ReactiveCommand RemoveCommand { get; } = new();

	public ExtensionConfigViewModel(ExtensionObjectModel extensionConfigModel, ScanConfigModel scanConfig) {
		this.Extension = extensionConfigModel.Extension.ToTwoWayBindableReactiveProperty(string.Empty).AddTo(this.CompositeDisposable);
		this.MediaType = extensionConfigModel.MediaType.ToTwoWayBindableReactiveProperty(Composition.Enum.MediaType.Image).AddTo(this.CompositeDisposable);

		this.RemoveCommand.Subscribe(_ => {
			scanConfig.RemoveTargetExtension(extensionConfigModel);
		}).AddTo(this.CompositeDisposable);
	}
}