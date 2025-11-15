using MediaDeck.Composition.Bases;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.Config.Model.Objects;

namespace MediaDeck.ViewModels.Preferences.Config;

[AddScoped]
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
