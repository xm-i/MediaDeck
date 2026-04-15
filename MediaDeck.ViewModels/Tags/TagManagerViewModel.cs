using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.Tags;

namespace MediaDeck.ViewModels.Tags;

[Inject(InjectServiceLifetime.Transient)]
public class TagManagerViewModel : ViewModelBase {
	public TagManagerViewModel(ITagsManager tagsManager, ITagModelFactory tagModelFactory) {
		var tagCategoriesView = tagsManager.TagCategories.CreateView(x => new TagCategoryViewModel(x, tagsManager, tagModelFactory));
		this.TagCategories = tagCategoriesView.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
		this.SaveCommand.SubscribeAwait(async (_, ct) => {
			foreach (var category in this.TagCategories) {
				category.SyncToModel();
			}
			await tagsManager.SaveAsync();
		}, AwaitOperation.Drop).AddTo(this.CompositeDisposable);
		this.AddTagCategoryCommand.Subscribe(_ => {
			tagsManager.AddTagCategory();
		}).AddTo(this.CompositeDisposable);
		this.DeleteTagCategoryCommand.SubscribeAwait(async (_, ct) => {
			var category = this.SelectedTagCategory.Value;
			if (category != null && category.IsDeletable) {
				await tagsManager.DeleteTagCategoryAsync(category.Model);
				this.SelectedTagCategory.Value = null;
			}
		}, AwaitOperation.Drop).AddTo(this.CompositeDisposable);
		this.DeleteTagCommand.SubscribeAwait(async (_, ct) => {
			var category = this.SelectedTagCategory.Value;
			var tag = category?.SelectedTag.Value;
			if (tag != null) {
				await tagsManager.DeleteTagAsync(tag.Model);
				category!.SelectedTag.Value = null;
			}
		}, AwaitOperation.Drop).AddTo(this.CompositeDisposable);
		this.CancelCommand.SubscribeAwait(async (_, ct) => {
			await tagsManager.ReloadAsync();
		}, AwaitOperation.Drop).AddTo(this.CompositeDisposable);
		this.LoadCommand.SubscribeAwait(async (_, ct) => {
			await tagsManager.InitializeAsync();
		}, AwaitOperation.Drop).AddTo(this.CompositeDisposable);
	}

	public INotifyCollectionChangedSynchronizedViewList<TagCategoryViewModel> TagCategories {
		get;
	}

	/// <summary>
	/// 選択されているタグカテゴリー
	/// </summary>
	public BindableReactiveProperty<TagCategoryViewModel?> SelectedTagCategory {
		get;
	} = new();

	public ReactiveCommand LoadCommand {
		get;
	} = new();

	public ReactiveCommand SaveCommand {
		get;
	} = new();

	public ReactiveCommand CancelCommand {
		get;
	} = new();

	public ReactiveCommand AddTagCategoryCommand {
		get;
	} = new();

	public ReactiveCommand DeleteTagCategoryCommand {
		get;
	} = new();

	public ReactiveCommand DeleteTagCommand {
		get;
	} = new();
}