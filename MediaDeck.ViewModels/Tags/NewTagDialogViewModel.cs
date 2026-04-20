using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.Tags;

namespace MediaDeck.ViewModels.Tags;

[Inject(InjectServiceLifetime.Transient)]
public class NewTagDialogViewModel : ViewModelBase {
	public NewTagDialogViewModel(ITagsManager tagsManager, ITagModelFactory tagModelFactory) {
		this.TagCategories = tagsManager.TagCategories.Select(x => new TagCategoryViewModel(x, tagsManager, tagModelFactory)).ToArray();
		this.SelectedCategory.Value = this.TagCategories.FirstOrDefault();
		this.ConfirmCommand.Subscribe(async _ => {
			// タグを作成
			this.CreatedTag.Value = await tagsManager.CreateTagImmediatelyAsync(this.SelectedCategory.Value?.Model.TagCategoryId,
				this.TagName.Value,
				this.Ruby.Value,
				this.Detail.Value,
				[]);
			this.Result.Value = DialogResult.Confirmed;
		}).AddTo(this.CompositeDisposable);
		this.CancelCommand.Subscribe(_ => {
			this.Result.Value = DialogResult.Canceled;
		}).AddTo(this.CompositeDisposable);
	}

	public BindableReactiveProperty<string> TagName {
		get;
	} = new("");

	public BindableReactiveProperty<string> Ruby {
		get;
	} = new("");

	public BindableReactiveProperty<string> Detail {
		get;
	} = new("");

	public BindableReactiveProperty<TagCategoryViewModel?> SelectedCategory {
		get;
	} = new();

	public BindableReactiveProperty<DialogResult> Result {
		get;
	} = new(DialogResult.None);

	public BindableReactiveProperty<ITagModel?> CreatedTag {
		get;
	} = new();

	public TagCategoryViewModel[] TagCategories {
		get;
		private set;
	} = [];

	public ReactiveCommand ConfirmCommand {
		get;
	} = new();

	public ReactiveCommand CancelCommand {
		get;
	} = new();
}

public enum DialogResult {
	None,
	Confirmed,
	Canceled
}