using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Core.Models.FileDetailManagers;
using MediaDeck.Core.Models.FileDetailManagers.Objects;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Models.NotificationDispatcher;
using MediaDeck.Core.Primitives;
using MediaDeck.ViewModels.Tags;

namespace MediaDeck.ViewModels.Panes.DetailPanes;

public record NewTagRequestedContext(string TagName, IEnumerable<ITagCategoryModel> TagCategories);

[Inject(InjectServiceLifetime.Transient)]
public class DetailSelectorViewModel : ViewModelBase {
	public Subject<NewTagRequestedContext> NewTagRequested {
		get;
	} = new();

	private readonly DetailSelectorModel _model;
	private readonly System.Collections.Concurrent.ConcurrentDictionary<int, TagCategoryViewModel> _categoryViewModels = new();

	public DetailSelectorViewModel(DetailSelectorModel model,
		SearchConditionNotificationDispatcher searchConditionNotificationDispatcher,
		ITagModelFactory tagModelFactory) {
		this._model = model;
		this.RepresentativeFilePath = model.RepresentativeFilePath.ToBindableReactiveProperty(string.Empty);
		this.Properties = model.Properties.ToBindableReactiveProperty([]);
		this.Rate = model.Rate.ToBindableReactiveProperty();
		this.Description = model.Description.ToBindableReactiveProperty(string.Empty);
		this.UsageCount = model.UsageCount.ToBindableReactiveProperty();
		this._model.AddTo(this.CompositeDisposable);

		this.TagCandidates = model.TagModels.CreateView(x => {
			var categoryViewModel = this._categoryViewModels.GetOrAdd(x.TagCategoryId, _ => new TagCategoryViewModel(x.TagCategory, model.TagsManager, tagModelFactory));
			return new TagViewModel(categoryViewModel, x, model.TagsManager, tagModelFactory);
		});
		this.FilteredTagCandidates = this.TagCandidates.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
		this.Tags = model.Tags.CreateView(x => {
			var categoryViewModel = this._categoryViewModels.GetOrAdd(x.Value.TagCategoryId, _ => new TagCategoryViewModel(x.Value.TagCategory, model.TagsManager, tagModelFactory));
			return new ValueCountPair<TagViewModel>(new TagViewModel(categoryViewModel, x.Value, model.TagsManager, tagModelFactory), x.Count);
		})
			.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

		Observable.Merge(this.TargetFiles.Where(x => x != null).Select(_ => Unit.Default),
				model.ContentChanged.AsObservable())
			.Subscribe(_ => this._model.Refresh(this.TargetFileModels));

		this.Rate.Subscribe(async x => {
			if (!double.IsInteger(x) || this.TargetFiles.Value is null) {
				return;
			}
			await model.UpdateRateAsync(this.TargetFileModels, (int)x);
		});

		this.LoadTagCandidatesCommand.Subscribe(async _ => await model.LoadTagCandidatesAsync());
		this.RefreshFilteredTagCandidatesCommand.Subscribe(_ => this.RefreshTagCandidateFilter());
		this.UpdateDescriptionCommand.Subscribe(async _ => {
			await model.UpdateDescriptionAsync(this.TargetFileModels.First(), this.Description.Value);
		});
		this.RemoveTagCommand.Subscribe(async x => {
			await model.RemoveTagAsync(this.TargetFileModels, x.Value.Model.TagId);
		});
		this.AddTagCommand.Subscribe(async _ => {
			if (string.IsNullOrEmpty(this.Text.Value)) {
				return;
			}

			await model.AddTagByNameAsync(this.TargetFileModels, this.Text.Value);
			this.Text.Value = string.Empty;
		});

		this.SearchTaggedFilesCommand.Subscribe(x => {
			searchConditionNotificationDispatcher.UpdateRequest.OnNext(conditions => {
				conditions.Clear();
				var condition = new TagSearchCondition {
					TargetTag = x.Value.Model
				};
				conditions.Add(condition);
			});
		});

		this.Text.Subscribe(_ => this.RefreshTagCandidateFilter());
	}

	public BindableReactiveProperty<string> RepresentativeFilePath {
		get;
	}

	public BindableReactiveProperty<IFileModel[]?> TargetFiles {
		get;
	} = new();

	private IFileModel[] TargetFileModels {
		get {
			return this.TargetFiles.Value ?? Array.Empty<IFileModel>();
		}
	}

	public BindableReactiveProperty<string> Text {
		get;
	} = new(string.Empty);

	public ISynchronizedView<ITagModel, TagViewModel> TagCandidates {
		get;
	}

	public INotifyCollectionChangedSynchronizedViewList<TagViewModel> FilteredTagCandidates {
		get;
	}

	public ReactiveCommand LoadTagCandidatesCommand {
		get;
	} = new();

	public ReactiveCommand RefreshFilteredTagCandidatesCommand {
		get;
	} = new();

	public INotifyCollectionChangedSynchronizedViewList<ValueCountPair<TagViewModel>> Tags {
		get;
	}

	public ReactiveCommand<ValueCountPair<TagViewModel>> SearchTaggedFilesCommand {
		get;
	} = new();

	public BindableReactiveProperty<FileProperty[]> Properties {
		get;
	}

	public BindableReactiveProperty<double> Rate {
		get;
	}

	public BindableReactiveProperty<string> Description {
		get;
	}

	public BindableReactiveProperty<double> UsageCount {
		get;
	}

	public ReactiveCommand UpdateDescriptionCommand {
		get;
	} = new();

	public ReactiveCommand<ValueCountPair<TagViewModel>> RemoveTagCommand {
		get;
	} = new();

	public ReactiveCommand AddTagCommand {
		get;
	} = new();

	public ITagsManager GetTagsManager() {
		return this._model.TagsManager;
	}

	public async Task OnNewTagCreated(ITagModel tag) {
		await this._model.AddTagAsync(this.TargetFileModels, tag);
		this.Text.Value = string.Empty;
	}

	private void RefreshTagCandidateFilter() {
		this.TagCandidates.AttachFilter(x => {
			return DetailSelectorModel.MatchesTagFilter(x, this.Text.Value, out _);
		});
	}
}