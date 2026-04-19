using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.Tags;

namespace MediaDeck.ViewModels.Tags;

public class TagCategoryViewModel : ViewModelBase {
	private readonly ITagsManager _tagsManager;

	public TagCategoryViewModel(ITagCategoryModel tagCategory, ITagsManager tagsManager, ITagModelFactory tagModelFactory) {
		this._tagsManager = tagsManager;
		this.Model = tagCategory;
		this.TagCategoryId = tagCategory.TagCategoryId;
		this.TagCategoryName.Value = tagCategory.TagCategoryName;
		this.Detail.Value = tagCategory.Detail;
		this.Tags = tagCategory.Tags.CreateView(x => new TagViewModel(this, x, tagsManager, tagModelFactory));
		this.FilteredTags = this.Tags.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

		this.FilterText.ThrottleLast(TimeSpan.FromMilliseconds(300))
			.ObserveOnCurrentSynchronizationContext()
			.Subscribe(_ => {
				this.RefreshTagCandidateFilter();
			}).AddTo(this.CompositeDisposable);
	}

	public ITagCategoryModel Model {
		get;
	}

	public int? TagCategoryId {
		get;
	}

	public ISynchronizedView<ITagModel, TagViewModel> Tags {
		get;
	}

	/// <summary>
	/// このカテゴリが削除可能かどうか（システム定義の未分類カテゴリ以外は削除可能）
	/// </summary>
	public bool IsDeletable {
		get {
			return this.Model != this._tagsManager.NoCategory;
		}
	}

	public INotifyCollectionChangedSynchronizedViewList<TagViewModel> FilteredTags {
		get;
	}

	/// <summary>
	/// 選択されているタグ
	/// </summary>
	public BindableReactiveProperty<TagViewModel?> SelectedTag {
		get;
	} = new();

	public BindableReactiveProperty<string> TagCategoryName {
		get;
	} = new();

	public BindableReactiveProperty<string> Detail {
		get;
	} = new();

	public BindableReactiveProperty<string> FilterText {
		get;
	} = new();

	public void SyncToModel() {
		this.Model.TagCategoryName = this.TagCategoryName.Value;
		this.Model.Detail = this.Detail.Value;
		foreach (var tag in this.Tags) {
			tag.SyncToModel();
		}
	}

	private void RefreshTagCandidateFilter() {
		this.Tags.AttachFilter((tag, tagVm) => {
			var text = this.FilterText.Value ?? "";
			if (text.Length == 0) {
				return true;
			}
			if (tag.TagName.Contains(text) ||
				(tag.Ruby?.Contains(text) ?? false) ||
				(tag.Romaji?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false)) {
				tagVm.RepresentativeText.Value = null;
				return true;
			}
			var result =
				tag
					.TagAliases
					.FirstOrDefault(x =>
						x.Alias.Contains(text, StringComparison.CurrentCultureIgnoreCase) ||
						(x.Ruby?.Contains(text) ?? false) ||
						(x.Romaji?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false));
			tagVm.RepresentativeText.Value = result?.Alias;
			return result != null;
		});
	}
}