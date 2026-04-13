using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.Tags;

namespace MediaDeck.ViewModels.Tags;

[Inject(InjectServiceLifetime.Transient)]
public class TagManagerViewModel : ViewModelBase {
	public TagManagerViewModel(ITagsManager tagsManager, ITagModelFactory tagModelFactory) {
		this._tagCategories = tagsManager.TagCategories.CreateView(x => new TagCategoryViewModel(x, tagsManager, tagModelFactory));
		this.TagCategories = this._tagCategories.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
		this.SaveCommand.SubscribeAwait(async (_, ct) => {
			foreach (var category in this.TagCategories) {
				category.SyncToModel();
			}
			await tagsManager.SaveAsync();
		}, AwaitOperation.Drop);
		this.AddTagCategoryCommand.Subscribe(_ => {
			var model = tagModelFactory.CreateCategory();
			model.TagCategoryName = "";
			model.Detail = "";
			tagsManager.TagCategories.Add(model);
		});
	}


	private readonly ISynchronizedView<ITagCategoryModel, TagCategoryViewModel> _tagCategories;

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