using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.Config.Model.Objects;
using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;

namespace MediaDeck.ViewModels.Preferences.Config;

[Inject(InjectServiceLifetime.Scoped)]
public class ExtensionConfigViewModel : ViewModelBase {
	public BindableReactiveProperty<string> Extension {
		get;
	}

	public BindableReactiveProperty<MediaType> MediaType {
		get;
	}

	public ExtensionConfigViewModel(ExtensionObjectModel extensionConfigModel) {
		this.Extension = extensionConfigModel.Extension.ToTwoWayBindableReactiveProperty(string.Empty).AddTo(this.CompositeDisposable);
		this.MediaType = extensionConfigModel.MediaType.ToTwoWayBindableReactiveProperty(Composition.Enum.MediaType.Image).AddTo(this.CompositeDisposable);
	}
}