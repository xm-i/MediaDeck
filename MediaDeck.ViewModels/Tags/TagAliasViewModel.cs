using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.Tags;

namespace MediaDeck.ViewModels.Tags;

public class TagAliasViewModel : ViewModelBase {
	public TagAliasViewModel(ITagAliasModel tagAlias, TagViewModel parent) {
		this.Model = tagAlias;
		this.Alias.Value = tagAlias.Alias;
		this.Ruby.Value = tagAlias.Ruby ?? string.Empty;
		this.Parent = parent;

		this.Alias.Subscribe(x => {
			tagAlias.Alias = x;
			parent.MarkAsEdited();
		}).AddTo(this.CompositeDisposable);
		this.Ruby.Subscribe(x => {
			tagAlias.Ruby = string.IsNullOrEmpty(x) ? null : x;
			parent.MarkAsEdited();
		}).AddTo(this.CompositeDisposable);
	}

	public ITagAliasModel Model {
		get;
	}

	public TagViewModel Parent {
		get;
	}

	public BindableReactiveProperty<string> Alias {
		get;
	} = new();

	public BindableReactiveProperty<string> Ruby {
		get;
	} = new();
}