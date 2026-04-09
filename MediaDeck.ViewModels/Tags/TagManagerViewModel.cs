using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Core.Models.FileDetailManagers;
using MediaDeck.Core.Models.Files;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.ViewModels.Tags;

[Inject(InjectServiceLifetime.Transient)]
public class TagManagerViewModel : ViewModelBase {
	public TagManagerViewModel(TagsManager tagsManager, ITagModelFactory tagModelFactory, IServiceProvider serviceProvider) {
		this._tagCategories = [.. tagsManager.TagCategories.Select(x => new TagCategoryViewModel(tagModelFactory.Create(x), tagsManager, x.Tags.Select(tagModelFactory.Create)))];
		this.TagCategories = this._tagCategories.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
		this.LoadCommand.Subscribe(async _ => await tagsManager.Load());
		this.SaveCommand.Subscribe(async _ => {
			foreach (var tagCategory in this.TagCategories) {
				tagCategory.UpdateTagCategoryCommand.Execute(Unit.Default);
			}
			await tagsManager.Load();
			this._tagCategories.Clear();
			this._tagCategories.AddRange(tagsManager.TagCategories.Select(x => new TagCategoryViewModel(tagModelFactory.Create(x), tagsManager, x.Tags.Select(tagModelFactory.Create))));
		});
		this.AddTagCategoryCommand.Subscribe(_ => {
			var model = serviceProvider.GetRequiredService<ITagCategoryModel>();
			model.TagCategoryName = "";
			model.Detail = "";
			this._tagCategories.Add(new(model, tagsManager));
		});
	}


	private readonly ObservableList<TagCategoryViewModel> _tagCategories = [];

	public INotifyCollectionChangedSynchronizedViewList<TagCategoryViewModel> TagCategories {
		get;
	}

	public BindableReactiveProperty<TagCategoryViewModel> SelectedTagCategory {
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
}