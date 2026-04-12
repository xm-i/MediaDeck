using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.Tags;

namespace MediaDeck.ViewModels.Tags;

[Inject(InjectServiceLifetime.Transient)]
public class TagManagerViewModel : ViewModelBase {
	public TagManagerViewModel(ITagsManager tagsManager, ITagModelFactory tagModelFactory) {
		this._tagCategories = tagsManager.TagCategories.CreateView(x => new TagCategoryViewModel(x, tagsManager, tagModelFactory));
		this.TagCategories = this._tagCategories.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
		this.LoadCommand.Subscribe(async _ => await tagsManager.Load());
		this.SaveCommand.Subscribe(async _ => {
			foreach (var tagCategory in this.TagCategories) {
				tagCategory.UpdateTagCategoryCommand.Execute(Unit.Default);
			}
			await tagsManager.Load();
		});
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