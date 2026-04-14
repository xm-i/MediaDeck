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
		}, AwaitOperation.Drop);
		this.AddTagCategoryCommand.Subscribe(_ => {
			var model = tagModelFactory.CreateCategory();
			model.TagCategoryName = "";
			model.Detail = "";
			tagsManager.TagCategories.Add(model);
		});
		this.DeleteTagCategoryCommand.SubscribeAwait(async (_, ct) => {
			var category = this.SelectedTagCategory.Value;
			if (category != null && category.IsDeletable) {
				if (category.TagCategoryId is null) {
					// 追加したばかりの未保存カテゴリの削除（キャンセル）
					tagsManager.TagCategories.Remove(category.Model);
				} else {
					// DBに存在するカテゴリの削除
					await tagsManager.DeleteTagCategoryAsync(category.Model);
				}
				this.SelectedTagCategory.Value = null;
			}
		}, AwaitOperation.Drop);
		this.DeleteTagCommand.SubscribeAwait(async (_, ct) => {
			var category = this.SelectedTagCategory.Value;
			var tag = category?.SelectedTag.Value;
			if (tag != null) {
				if (tag.Model.TagId > 0) {
					await tagsManager.DeleteTagAsync(tag.Model);
				} else {
					tagsManager.Tags.Remove(tag.Model);
					category!.Model.Tags.Remove(tag.Model);
				}
				category!.SelectedTag.Value = null;
			}
		}, AwaitOperation.Drop);
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