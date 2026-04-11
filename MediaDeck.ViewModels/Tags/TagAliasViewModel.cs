using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.Tags;

namespace MediaDeck.ViewModels.Tags;

public class TagAliasViewModel {
	public TagAliasViewModel(ITagAliasModel tagAlias, TagViewModel parent) {
		this.Model = tagAlias;
		this.Alias.Value = tagAlias.Alias;
		this.Ruby.Value = tagAlias.Ruby ?? string.Empty;
		this.Parent = parent;

		this.Alias.Subscribe(x => {
			tagAlias.Alias = x;
			parent.MarkAsEdited();
		});
		this.Ruby.Subscribe(x => {
			tagAlias.Ruby = string.IsNullOrEmpty(x) ? null : x;
			parent.MarkAsEdited();
		});
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