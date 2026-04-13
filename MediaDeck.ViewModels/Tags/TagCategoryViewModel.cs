using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Interfaces.Tags;

namespace MediaDeck.ViewModels.Tags;

public class TagCategoryViewModel {
	public TagCategoryViewModel(ITagCategoryModel tagCategory, ITagsManager tagsManager, ITagModelFactory tagModelFactory) {
		this.Model = tagCategory;
		this.TagCategoryId = tagCategory.TagCategoryId;
		this.TagCategoryName.Value = tagCategory.TagCategoryName;
		this.Detail.Value = tagCategory.Detail;
		this.Tags = tagCategory.Tags.CreateView(x => new TagViewModel(this, x, tagsManager, tagModelFactory));
		this.FilteredTags = this.Tags.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

		this.UpdateTagCategoryCommand = this.TagCategoryName.CombineLatest(this.Detail, (x, y) => !string.IsNullOrWhiteSpace(x) && !string.IsNullOrWhiteSpace(y)).ToReactiveCommand();
		this.UpdateTagCategoryCommand.Subscribe(async _ => {
			if (this.TagCategoryId.HasValue) {
				await tagsManager.UpdateTagCategoryAsync(this.TagCategoryId.Value,
					this.TagCategoryName.Value,
					this.Detail.Value);
			} else {
				await tagsManager.CreateTagCategoryAsync(this.TagCategoryName.Value,
					this.Detail.Value);
				// プレースホルダー（自分自身に関連付けられた空モデル）を削除
				// これにより SynchronizedView 経由で自分が破棄され、新しいモデル用の ViewModel が生成される
				tagsManager.TagCategories.Remove(this.Model);
			}

			foreach (var tag in this.Tags) {
				tag.UpdateTagCommand.Execute(Unit.Default);
			}
		});

		this.FilterText.ThrottleLast(TimeSpan.FromMilliseconds(300))
			.Subscribe(_ => {
				this.RefreshTagCandidateFilter();
			});
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

	public INotifyCollectionChangedSynchronizedViewList<TagViewModel> FilteredTags {
		get;
	}

	public BindableReactiveProperty<TagViewModel> SelectedTag {
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

	public ReactiveCommand UpdateTagCategoryCommand {
		get;
	} = new();

	private void RefreshTagCandidateFilter() {
		this.Tags.AttachFilter(tag => {
			var text = this.FilterText.Value ?? "";
			if (text.Length == 0) {
				return true;
			}
			if (tag.TagName.Contains(text) ||
				(tag.TagName.KatakanaToHiragana().HiraganaToRomaji()?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false)) {
				tag.RepresentativeText.Value = null;
				return true;
			}
			var result =
				tag
					.TagAliases
					.FirstOrDefault(x =>
						x.Alias.Contains(text, StringComparison.CurrentCultureIgnoreCase) ||
						(x.Ruby?.Contains(text) ?? false) ||
						((x.Ruby ?? x.Alias.KatakanaToHiragana()).HiraganaToRomaji()?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false));
			tag.RepresentativeText.Value = result?.Alias;
			return result != null;
		});
	}
}