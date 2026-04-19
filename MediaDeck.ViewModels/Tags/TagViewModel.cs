using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Interfaces.Tags;

namespace MediaDeck.ViewModels.Tags;

[Inject(InjectServiceLifetime.Transient)]
public class TagViewModel : ViewModelBase {
	public TagViewModel(TagCategoryViewModel parent, ITagModel tag, ITagsManager tagsManager, ITagModelFactory tagModelFactory) {
		this.Model = tag;
		this.TagName.Value = tag.TagName;
		this.Detail.Value = tag.Detail;
		this.UsageCount = tag.UsageCount.ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this._tagAliases.AddRange(tag.TagAliases.Select(x => new TagAliasViewModel(x, this)));
		this.TagAliases = this._tagAliases.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
		this.TagCategory.Value = parent;

		this.RemoveTagAliasCommand.Subscribe(x => {
			this._tagAliases.Remove(x);
		}).AddTo(this.CompositeDisposable);
		this.AddTagAliasCommand.Subscribe(_ => {
			this._tagAliases.Add(new(tagModelFactory.CreateAlias(), this));
		}).AddTo(this.CompositeDisposable);

		this.TagName
			.ToUnit()
			.Merge(this.Detail.ToUnit())
			.Merge(this.TagAliases.ToObservable().ToUnit())
			.Merge(this.TagCategory.ToUnit())
			.Subscribe(_ => {
				this._editedFlag = true;
			}).AddTo(this.CompositeDisposable);
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

	public void SyncToModel() {
		if (!this._editedFlag) {
			return;
		}
		this.Model.TagName = this.TagName.Value;
		this.Model.Detail = this.Detail.Value;
		this.Model.TagCategoryId = this.TagCategory.Value.TagCategoryId;
		this.Model.TagAliases = this.TagAliases.Select(x => {
			var alias = x.Model;
			alias.Alias = x.Alias.Value;
			alias.Ruby = string.IsNullOrEmpty(x.Ruby.Value) ? null : x.Ruby.Value;
			return alias;
		}).ToList();
		this._editedFlag = false;
	}

	public ReactiveCommand AddTagAliasCommand {
		get;
	} = new();

	public ReactiveCommand<TagAliasViewModel> RemoveTagAliasCommand {
		get;
	} = new();

	public BindableReactiveProperty<string?> RepresentativeText {
		get;
	} = new();

	public BindableReactiveProperty<int> UsageCount {
		get;
	}

	public void MarkAsEdited() {
		this._editedFlag = true;
	}
}