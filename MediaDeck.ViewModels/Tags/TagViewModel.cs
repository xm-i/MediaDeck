using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Core.Models.Tags;

namespace MediaDeck.ViewModels.Tags;

[Inject(InjectServiceLifetime.Transient)]
public class TagViewModel : ViewModelBase {
	public TagViewModel(TagCategoryViewModel parent, ITagModel tag, ITagsManager tagsManager, ITagModelFactory tagModelFactory) {
		this.Model = tag;
		this.TagName.Value = tag.TagName;
		this.Detail.Value = tag.Detail;
		this.RepresentativeText = tag.RepresentativeText.ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.UsageCount = tag.UsageCount.ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this._tagAliases.AddRange(tag.TagAliases.Select(x => new TagAliasViewModel(x, this)));
		this.TagAliases = this._tagAliases.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
		this.TagCategory.Value = parent;
		this.UpdateTagCommand = this.TagName.CombineLatest(this.Detail, (x, y) => !string.IsNullOrWhiteSpace(x) && !string.IsNullOrWhiteSpace(y)).ToReactiveCommand();
		this.UpdateTagCommand.Subscribe(async _ => {
			if (!this._editedFlag) {
				return;
			}
			await tagsManager.UpdateTagAsync(tag.TagId,
				this.TagCategory.Value.TagCategoryId,
				this.TagName.Value,
				this.Detail.Value,
				this.TagAliases.Select(x => {
					var alias = x.Model;
					alias.Alias = x.Alias.Value;
					alias.Ruby = string.IsNullOrEmpty(x.Ruby.Value) ? null : x.Ruby.Value;
					return alias;
				}));
			this._editedFlag = false;
		});
		this.RemoveTagAliasCommand.Subscribe(x => {
			this._tagAliases.Remove(x);
		});
		this.AddTagAliasCommand.Subscribe(_ => {
			this._tagAliases.Add(new(tagModelFactory.CreateAlias(), this));
		});

		this.TagName
			.ToUnit()
			.Merge(this.Detail.ToUnit())
			.Merge(this.TagAliases.ToObservable().ToUnit())
			.Merge(this.TagCategory.ToUnit())
			.Subscribe(_ => {
				this._editedFlag = true;
			});
		this._editedFlag = false;
	}

	private bool _editedFlag = false;
	private readonly ObservableList<TagAliasViewModel> _tagAliases = [];

	public ITagModel Model {
		get;
	}

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

	public BindableReactiveProperty<string?> RepresentativeText {
		get;
	}

	public BindableReactiveProperty<int> UsageCount {
		get;
	}

	public BindableReactiveProperty<string?> RepresentativeTextForSearch {
		get;
	} = new();

	public void MarkAsEdited() {
		this._editedFlag = true;
	}
}