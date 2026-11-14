using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Interfaces.Tags;

namespace MediaDeck.ViewModels.Tags;

/// <summary>
/// タグ管理画面の ViewModel クラス。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class TagManagerViewModel : ViewModelBase {
	/// <summary>
	/// <see cref="TagManagerViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="tagsManager">タグマネージャー</param>
	/// <param name="tagModelFactory">タグモデルファクトリー</param>
	public TagManagerViewModel(ITagsManager tagsManager, ITagModelFactory tagModelFactory) {
		this.SelectedTagCategory = new BindableReactiveProperty<TagCategoryViewModel?>().AddTo(this.CompositeDisposable);
		this.LoadCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.AddTagCategoryCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.DeleteTagCategoryCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.AddTagCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.DeleteTagCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);

		var tagCategoriesView = tagsManager.TagCategories
			.CreateViewWithDisposal(x => new TagCategoryViewModel(x, tagsManager, tagModelFactory, movedTag => this.SelectTag(movedTag.TagId)))
			.AddTo(this.CompositeDisposable);
		this.TagCategories = tagCategoriesView
			.ToNotifyCollectionChangedWithDisposal(SynchronizationContextCollectionEventDispatcher.Current)
			.AddTo(this.CompositeDisposable);

		this.AddTagCategoryCommand.SubscribeAwait(async (_, ct) => {
			var createdCategory = await tagsManager.CreateTagCategoryAsync();
			var createdVm = this.TagCategories.FirstOrDefault(x => x.Model == createdCategory);
			if (createdVm != null) {
				this.SelectedTagCategory.Value = createdVm;
			}
		}, AwaitOperation.Drop).AddTo(this.CompositeDisposable);

		this.DeleteTagCategoryCommand.SubscribeAwait(async (_, ct) => {
			var category = this.SelectedTagCategory.Value;
			if (category != null && category.IsDeletable) {
				await tagsManager.DeleteTagCategoryAsync(category.Model);
				this.SelectedTagCategory.Value = null;
			}
		}, AwaitOperation.Drop).AddTo(this.CompositeDisposable);

		this.AddTagCommand.SubscribeAwait(async (_, ct) => {
			var targetCategory = this.SelectedTagCategory.Value?.Model ?? tagsManager.NoCategory;
			var createdTag = await tagsManager.CreateTagImmediatelyAsync(
				targetCategory.TagCategoryId,
				string.Empty,
				null,
				string.Empty,
				[]);
			if (createdTag != null) {
				this.SelectTag(createdTag.TagId);
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

		this.LoadCommand.SubscribeAwait(async (_, ct) => {
			await tagsManager.InitializeAsync();
		}, AwaitOperation.Drop).AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// タグカテゴリーの一覧を取得します。
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<TagCategoryViewModel> TagCategories {
		get;
	}

	/// <summary>
	/// 選択されているタグカテゴリー
	/// </summary>
	public BindableReactiveProperty<TagCategoryViewModel?> SelectedTagCategory {
		get;
	}

	/// <summary>
	/// データを読み込むコマンドを取得します。
	/// </summary>
	public ReactiveCommand LoadCommand {
		get;
	}

	/// <summary>
	/// タグカテゴリーを追加するコマンドを取得します。
	/// </summary>
	public ReactiveCommand AddTagCategoryCommand {
		get;
	}

	/// <summary>
	/// 選択中のタグカテゴリーを削除するコマンドを取得します。
	/// </summary>
	public ReactiveCommand DeleteTagCategoryCommand {
		get;
	}

	/// <summary>
	/// 新規タグを追加するコマンドを取得します。
	/// </summary>
	public ReactiveCommand AddTagCommand {
		get;
	}

	/// <summary>
	/// 選択中のタグを削除するコマンドを取得します。
	/// </summary>
	public ReactiveCommand DeleteTagCommand {
		get;
	}

	/// <summary>
	/// 指定したIDのタグを選択状態にします。
	/// </summary>
	/// <param name="tagId">選択するタグのID</param>
	public void SelectTag(int tagId) {
		foreach (var category in this.TagCategories) {
			var tagVm = category.Tags.FirstOrDefault(t => t.Model.TagId == tagId);
			if (tagVm != null) {
				this.SelectedTagCategory.Value = category;
				category.SelectedTag.Value = tagVm;
				return;
			}
		}
	}
}