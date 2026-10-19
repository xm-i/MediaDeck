using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.Notifications;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Core.Models.Files;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Models.Tags;
using MediaDeck.Core.Primitives;
using MediaDeck.ViewModels.Tags;

namespace MediaDeck.ViewModels.Panes.DetailPanes;

/// <summary>
/// 新規タグ作成リクエストのコンテキスト情報。
/// </summary>
/// <param name="TagName">要求されたタグ名</param>
/// <param name="SelectedCategory">選択されたカテゴリ</param>
public record NewTagRequestedContext(string TagName, ITagCategoryModel? SelectedCategory);

/// <summary>
/// タグ詳細ペイン（タグ付与・サジェスト・タグ検索等）の ViewModel。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class TagsDetailViewModel : ViewModelBase {
	/// <summary>
	/// 新規タグダイアログの表示リクエスト
	/// </summary>
	public Subject<NewTagRequestedContext> NewTagRequested {
		get;
	} = new();

	/// <summary>
	/// タグ管理画面を開くリクエスト
	/// </summary>
	public Subject<TagViewModel> OpenTagManagerRequested {
		get;
	} = new();

	private readonly TagsDetailModel _model;
	private readonly System.Collections.Concurrent.ConcurrentDictionary<int, TagCategoryViewModel> _categoryViewModels = new();

	/// <summary>
	/// タグモデルファクトリ
	/// </summary>
	public ITagModelFactory TagModelFactory {
		get;
	}

	/// <summary>
	/// 対象ファイル群
	/// </summary>
	public BindableReactiveProperty<IMediaItemModel[]?> TargetFiles {
		get;
	} = new();

	private IMediaItemModel[] TargetFileModels {
		get {
			return this.TargetFiles.Value ?? Array.Empty<IMediaItemModel>();
		}
	}

	/// <summary>
	/// 入力中のテキスト
	/// </summary>
	public BindableReactiveProperty<string> Text {
		get;
	} = new(string.Empty);

	/// <summary>
	/// タグ候補一覧（ビュー）
	/// </summary>
	public ISynchronizedView<ITagModel, TagViewModel> TagCandidates {
		get;
	}

	/// <summary>
	/// フィルタリング済みタグ候補コレクション
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<TagViewModel> FilteredTagCandidates {
		get;
	}

	/// <summary>
	/// タグ候補フィルタ更新コマンド
	/// </summary>
	public ReactiveCommand RefreshFilteredTagCandidatesCommand {
		get;
	} = new();

	/// <summary>
	/// 付与済みタグ一覧
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<ValueCountPair<TagViewModel>> Tags {
		get;
	}

	/// <summary>
	/// タグ付きファイル検索コマンド
	/// </summary>
	public ReactiveCommand<ValueCountPair<TagViewModel>> SearchTaggedFilesCommand {
		get;
	} = new();

	/// <summary>
	/// タグ管理画面を開くコマンド
	/// </summary>
	public ReactiveCommand<ValueCountPair<TagViewModel>> OpenTagManagerCommand {
		get;
	} = new();

	/// <summary>
	/// タグ削除コマンド
	/// </summary>
	public ReactiveCommand<ValueCountPair<TagViewModel>> RemoveTagCommand {
		get;
	} = new();

	/// <summary>
	/// 入力テキストに基づくタグ追加コマンド
	/// </summary>
	public ReactiveCommand AddTagCommand {
		get;
	} = new();

	/// <summary>
	/// 指定タグの追加コマンド
	/// </summary>
	public ReactiveCommand<TagViewModel> AddSpecificTagCommand {
		get;
	} = new();

	/// <summary>
	/// <see cref="TagsDetailViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="model">タグ詳細モデル</param>
	/// <param name="searchConditionNotificationDispatcher">検索条件通知ディスパッチャー</param>
	/// <param name="tagModelFactory">タグモデルファクトリー</param>
	public TagsDetailViewModel(
		TagsDetailModel model,
		ISearchConditionNotificationDispatcher searchConditionNotificationDispatcher,
		ITagModelFactory tagModelFactory) {
		this._model = model.AddTo(this.CompositeDisposable);
		this.TagModelFactory = tagModelFactory;
		this.TargetFiles.AddTo(this.CompositeDisposable);
		this.Text.AddTo(this.CompositeDisposable);
		this.NewTagRequested.AddTo(this.CompositeDisposable);
		this.OpenTagManagerRequested.AddTo(this.CompositeDisposable);
		this.RefreshFilteredTagCandidatesCommand.AddTo(this.CompositeDisposable);
		this.SearchTaggedFilesCommand.AddTo(this.CompositeDisposable);
		this.OpenTagManagerCommand.AddTo(this.CompositeDisposable);
		this.RemoveTagCommand.AddTo(this.CompositeDisposable);
		this.AddTagCommand.AddTo(this.CompositeDisposable);
		this.AddSpecificTagCommand.AddTo(this.CompositeDisposable);

		this.TagCandidates = model.TagModels.CreateViewWithDisposal(x => {
			var categoryViewModel = this.GetCategoryViewModel(x.TagCategory, model.TagsManager, tagModelFactory);
			return new TagViewModel(categoryViewModel, x, model.TagsManager, tagModelFactory);
		}).AddTo(this.CompositeDisposable);
		this.FilteredTagCandidates = this.TagCandidates.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current).AddTo(this.CompositeDisposable);

		this.Tags = model.Tags.CreateViewWithDisposal(x => {
			var categoryViewModel = this.GetCategoryViewModel(x.Value.TagCategory, model.TagsManager, tagModelFactory);
			return new ValueCountPair<TagViewModel>(new TagViewModel(categoryViewModel, x.Value, model.TagsManager, tagModelFactory), x.Count);
		}).ToNotifyCollectionChangedWithDisposal(SynchronizationContextCollectionEventDispatcher.Current).AddTo(this.CompositeDisposable);

		this.TargetFiles
			.Where(x => x != null)
			.Subscribe(_ => this._model.Refresh(this.TargetFileModels))
			.AddTo(this.CompositeDisposable);

		this.RefreshFilteredTagCandidatesCommand.Subscribe(_ => this.RefreshTagCandidateFilter())
			.AddTo(this.CompositeDisposable);

		this.RemoveTagCommand.SubscribeAwait(async (x, ct) => {
			await model.RemoveTagAsync(this.TargetFileModels, x.Value.Model.TagId);
		}, AwaitOperation.Drop).AddTo(this.CompositeDisposable);

		this.AddTagCommand.SubscribeAwait(async (_, ct) => {
			if (string.IsNullOrEmpty(this.Text.Value)) {
				return;
			}

			var tag = await model.FindTagByNameAsync(this.Text.Value);
			if (tag is null) {
				this.NewTagRequested.OnNext(new NewTagRequestedContext(this.Text.Value, model.TagCategories.FirstOrDefault()));
				return;
			}

			await model.AddTagAsync(this.TargetFileModels, tag);
			this.Text.Value = string.Empty;
		}, AwaitOperation.Drop).AddTo(this.CompositeDisposable);

		this.AddSpecificTagCommand.SubscribeAwait(async (tagViewModel, ct) => {
			await model.AddTagAsync(this.TargetFileModels, tagViewModel.Model);
			this.Text.Value = string.Empty;
		}, AwaitOperation.Drop).AddTo(this.CompositeDisposable);

		this.SearchTaggedFilesCommand.Subscribe(x => {
			searchConditionNotificationDispatcher.UpdateRequest.OnNext(conditions => {
				conditions.Clear();
				var condition = new TagSearchCondition(this._model.TagsManager) {
					TagId = x.Value.Model.TagId
				};
				conditions.Add(condition);
			});
		}).AddTo(this.CompositeDisposable);

		this.OpenTagManagerCommand.Subscribe(x => {
			this.OpenTagManagerRequested.OnNext(x.Value);
		}).AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// タグマネージャーを取得します。
	/// </summary>
	public ITagsManager GetTagsManager() {
		return this._model.TagsManager;
	}

	/// <summary>
	/// 新規タグ作成時のコールバック処理。
	/// </summary>
	/// <param name="tag">作成されたタグ</param>
	public async Task OnNewTagCreated(ITagModel tag) {
		await this._model.AddTagAsync(this.TargetFileModels, tag);
		this.Text.Value = string.Empty;
	}

	private void RefreshTagCandidateFilter() {
		this.TagCandidates.AttachFilter((m, vm) => {
			var result = m.MatchesFilter(this.Text.Value, out var representativeText);
			vm.RepresentativeText.Value = representativeText ?? string.Empty;
			return result;
		});
	}

	private TagCategoryViewModel GetCategoryViewModel(ITagCategoryModel category, ITagsManager tagsManager, ITagModelFactory factory) {
		// ConcurrentDictionaryのキーはnotnull制約があるため、IDがnull(未設定)の場合はint.MinValueをキーとして使用します。
		var key = category.TagCategoryId ?? int.MinValue;
		return this._categoryViewModels.GetOrAdd(key, _ => new TagCategoryViewModel(category, tagsManager, factory));
	}

	/// <summary>
	/// リソースを破棄します。
	/// </summary>
	/// <param name="disposing">マネージドリソースを破棄するかどうか</param>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			foreach (var categoryVm in this._categoryViewModels.Values) {
				categoryVm.Dispose();
			}
			this._categoryViewModels.Clear();
		}
		base.Dispose(disposing);
	}
}