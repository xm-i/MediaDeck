using MediaDeck.Database.Tables;

namespace MediaDeck.ViewModels.Tags;

public class TagAliasViewModel {
	public TagAliasViewModel(TagAlias tagAlias, TagViewModel parent) {
		this.Alias.Value = tagAlias.Alias;
		this.Ruby.Value = tagAlias.Ruby;
		this.Ruby.ToUnit()
			.Concat(this.Alias.ToUnit())
			.Subscribe(_ => {
				parent.MarkAsEdited();
			});
	}

	public TagAliasViewModel() {
	}

	public BindableReactiveProperty<string> Alias {
		get;
	} = new();
	public BindableReactiveProperty<string?> Ruby {
		get;
	} = new();
}
