using MediaDeck.Composition.Bases;
using MediaDeck.Database.Tables;
using MediaDeck.Models.FileDetailManagers;

namespace MediaDeck.ViewModels.Tags;
[AddTransient]
public class TagViewModel : ViewModelBase {
	public TagViewModel(TagCategoryViewModel parent,Tag tag, TagsManager tagsManager) {
		this.TagName.Value = tag.TagName;
		this.Detail.Value = tag.Detail;
		this._tagAliases.AddRange(tag.TagAliases.Select(x => new TagAliasViewModel(x, this)));
		this.TagAliases = this._tagAliases.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
		this.TagCategory.Value = parent;
		this.UpdateTagCommand = this.TagName.CombineLatest(this.Detail, (x,y) => !string.IsNullOrWhiteSpace(x) && !string.IsNullOrWhiteSpace(y)).ToReactiveCommand();
		this.UpdateTagCommand.Subscribe(async _ => {
			if (!this._editedFlag) {
				return;
			}
			await tagsManager.UpdateTagAsync(
				tag.TagId,
				this.TagCategory.Value.TagCategoryId,
				this.TagName.Value,
				this.Detail.Value,
				this.TagAliases.Select(x =>
					new TagAlias() {
						Alias = x.Alias.Value,
						Ruby = string.IsNullOrEmpty(x.Ruby.Value) ? null : x.Ruby.Value
					}));
			this._editedFlag = false;
		});
		this.RemoveTagAliasCommand.Subscribe(x => {
			this._tagAliases.Remove(x);
		});
		this.AddTagAliasCommand.Subscribe(_ => {
			this._tagAliases.Add(new());
		});

		this.TagName
			.ToUnit()
			.Merge(this.Detail.ToUnit())
			.Merge(Reactive.Bindings.Extensions.INotifyCollectionChangedExtensions.CollectionChangedAsObservable(this.TagAliases).ToObservable().ToUnit())
			.Merge(this.TagCategory.ToUnit())
			.Subscribe(_ => {
			this._editedFlag = true;
		});
		this._editedFlag = false;
	}

	private bool _editedFlag = false;
	private readonly ObservableList<TagAliasViewModel> _tagAliases = [];
	public BindableReactiveProperty<string> TagName {
		get;
	} = new();

	public BindableReactiveProperty<string> Detail {
		get;
	} = new();

	public BindableReactiveProperty<TagCategoryViewModel> TagCategory {
		get;
	} = new();

	public INotifyCollectionChangedSynchronizedViewList<TagAliasViewModel> TagAliases {
		get;
	}

	public ReactiveCommand UpdateTagCommand {
		get;
	} = new();

	public ReactiveCommand AddTagAliasCommand {
		get;
	} = new();

	public ReactiveCommand<TagAliasViewModel> RemoveTagAliasCommand {
		get;
	} = new();

	public ReactiveProperty<string?> RepresentativeTextForSearch {
		get;
	} = new();

	public void MarkAsEdited() {
		this._editedFlag = true;
	}
}
