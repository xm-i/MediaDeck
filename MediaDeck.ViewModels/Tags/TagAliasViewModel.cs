using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Database.Tables;

namespace MediaDeck.ViewModels.Tags;

public class TagAliasViewModel {
	public TagAliasViewModel(ITagAliasModel tagAlias, TagViewModel parent) {
		this.Model = tagAlias;
		this.Alias.Value = tagAlias.Alias;
		this.Ruby.Value = tagAlias.Ruby;
		this.Ruby.ToUnit()
			.Concat(this.Alias.ToUnit())
			.Subscribe(_ => {
				parent.MarkAsEdited();
			});
	}

	public TagAliasViewModel() {
		this.Model = null!;
	}

	public ITagAliasModel Model {
		get;
	}

	public BindableReactiveProperty<string> Alias {
		get;
	} = new();

	public BindableReactiveProperty<string?> Ruby {
		get;
	} = new();
}