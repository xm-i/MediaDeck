using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Interfaces.Tags;

namespace MediaDeck.ViewModels.Tags;

public class TagCategoryViewModel {
	public TagCategoryViewModel(ITagCategoryModel? tagCategory, ITagsManager tagsManager, ITagModelFactory tagModelFactory) {
		this.Model = tagCategory;
		this.TagCategoryId = tagCategory?.TagCategoryId;
		this.TagCategoryName.Value = tagCategory?.TagCategoryName ?? "未設定";
		this.Detail.Value = tagCategory?.Detail ?? "カテゴリーが設定されていないタグ";
		this.Tags = tagCategory!.Tags.CreateView(x => new TagViewModel(this, x, tagsManager, tagModelFactory));
		this.FilteredTags = this.Tags.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

		this.UpdateTagCategoryCommand = this.TagCategoryName.CombineLatest(this.Detail, (x, y) => !string.IsNullOrWhiteSpace(x) && !string.IsNullOrWhiteSpace(y)).ToReactiveCommand();
		this.UpdateTagCategoryCommand.Subscribe(async _ => {
			if (tagCategory?.TagCategoryId.HasValue ?? false) {
				await tagsManager.UpdateTagCategoryAsync(tagCategory.TagCategoryId.Value,
					this.TagCategoryName.Value,
					this.Detail.Value);
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

	public ITagCategoryModel? Model {
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