using MediaDeck.Composition.Bases;
using MediaDeck.Models.FileDetailManagers;

namespace MediaDeck.ViewModels.Tags;
[Inject(InjectServiceLifetime.Transient)]
public class TagManagerViewModel : ViewModelBase {
	public TagManagerViewModel(TagsManager tagsManager) {
		this._tagCategories = [.. tagsManager.TagCategories.Select(x => new TagCategoryViewModel(x, tagsManager))];
		this.TagCategories = this._tagCategories.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
		this.LoadCommand.Subscribe(async _ => await tagsManager.Load());
		this.SaveCommand.Subscribe(async _ => {
			foreach (var tagCategory in this.TagCategories) {
				tagCategory.UpdateTagCategoryCommand.Execute(Unit.Default);
			}
			await tagsManager.Load();
			this._tagCategories.Clear();
			this._tagCategories.AddRange(tagsManager.TagCategories.Select(x => new TagCategoryViewModel(x, tagsManager)));
		});
		this.AddTagCategoryCommand.Subscribe(_ => {
			this._tagCategories.Add(new(new() {
				TagCategoryName = "",
				Tags = [],
				Detail = ""
			}, tagsManager));
		});
	}


	private readonly ObservableList<TagCategoryViewModel> _tagCategories = [];
	public INotifyCollectionChangedSynchronizedViewList<TagCategoryViewModel> TagCategories {
		get;
	}

	public BindableReactiveProperty<TagCategoryViewModel> SelectedTagCategory {
		get;
	} = new();

	public ReactiveCommand LoadCommand {
		get;
	} = new();

	public ReactiveCommand SaveCommand {
		get;
	} = new();
	public ReactiveCommand AddTagCategoryCommand {
		get;
	} = new();
}
