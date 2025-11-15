using MediaDeck.Database.Tables;
using MediaDeck.Models.FileDetailManagers;

namespace MediaDeck.ViewModels.Tags;

public class TagCategoryViewModel {
	public TagCategoryViewModel(TagCategory tagCategory, TagsManager tagsManager) {
		this.TagCategoryId = tagCategory.TagCategoryId;
		this.TagCategoryName.Value = tagCategory.TagCategoryName;
		this.Detail.Value = tagCategory.Detail;
		this.Tags = this._tags.CreateView(x => x);
		this.FilteredTags = this.Tags.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
		
		this._tags.AddRange(tagCategory.Tags.Select(x => new TagViewModel(this, x, tagsManager)));
		this.UpdateTagCategoryCommand = this.TagCategoryName.CombineLatest(this.Detail, (x, y) => !string.IsNullOrWhiteSpace(x) && !string.IsNullOrWhiteSpace(y)).ToReactiveCommand();
		this.UpdateTagCategoryCommand.Subscribe(async _ => {
			if (tagCategory.TagCategoryId != -1) {
				await tagsManager.UpdateTagCategoryAsync(
					tagCategory.TagCategoryId,
					this.TagCategoryName.Value,
					this.Detail.Value);
			}
			foreach (var tag in this.Tags) {
				tag.UpdateTagCommand.Execute(Unit.Default);
			}
		});

		this.FilterText.ThrottleLast(TimeSpan.FromMilliseconds(300)).Subscribe(_ => {
			this.RefreshTagCandidateFilter();
		});
	}

	private readonly ObservableList<TagViewModel> _tags = [];

	public int TagCategoryId {
		get;
	}

	public ISynchronizedView<TagViewModel, TagViewModel> Tags {
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
			if (tag.TagName.Value.Contains(text) ||
				(tag.TagName.Value.KatakanaToHiragana().HiraganaToRomaji()?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false)){
				tag.RepresentativeTextForSearch.Value = null;
				return true;
			}
			var result =
				tag
					.TagAliases
					.FirstOrDefault(
						x =>
							x.Alias.Value.Contains(text, StringComparison.CurrentCultureIgnoreCase) ||
							(x.Ruby.Value?.Contains(text) ?? false) ||
							((x.Ruby.Value ?? x.Alias.Value.KatakanaToHiragana()).HiraganaToRomaji()?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false)
					);
			tag.RepresentativeTextForSearch.Value = result?.Alias.Value;
			return result != null;
		});
	}
}
