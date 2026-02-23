using MediaDeck.Composition.Bases;
using MediaDeck.Database.Tables;
using MediaDeck.Models.FileDetailManagers;

namespace MediaDeck.ViewModels.Tags;

[Inject(InjectServiceLifetime.Transient)]
public class NewTagDialogViewModel : ViewModelBase {
	public NewTagDialogViewModel(TagsManager tagsManager) {
		this.TagCategories = tagsManager.TagCategories.ToArray();
		this.SelectedCategory.Value = this.TagCategories.FirstOrDefault();
		this.ConfirmCommand.Subscribe(async _ => {
			// タグを作成
			this.CreatedTag.Value = await tagsManager.CreateTagAsync(
				this.SelectedCategory.Value?.TagCategoryId ?? 0,
				this.TagName.Value,
				this.Detail.Value,
				[]
			);
			await tagsManager.Load();
			this.Result.Value = DialogResult.Confirmed;
		});
		this.CancelCommand.Subscribe(_ => {
			this.Result.Value = DialogResult.Canceled;
		});
	}
	public BindableReactiveProperty<string> TagName { get; } = new("");
	public BindableReactiveProperty<string> Detail { get; } = new("");
	public BindableReactiveProperty<TagCategory?> SelectedCategory { get; } = new();
	public BindableReactiveProperty<DialogResult> Result { get; } = new(DialogResult.None);
	public BindableReactiveProperty<Tag?> CreatedTag { get; } = new();

	public TagCategory[] TagCategories { get; private set; } = [];

	public ReactiveCommand ConfirmCommand { get; } = new();
	public ReactiveCommand CancelCommand { get; } = new();
}

public enum DialogResult {
	None,
	Confirmed,
	Canceled
}
