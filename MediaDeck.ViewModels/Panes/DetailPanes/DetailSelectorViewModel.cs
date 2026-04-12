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
		this.RepresentativeFilePath = model.RepresentativeFilePath.ToBindableReactiveProperty(string.Empty).AddTo(this.CompositeDisposable);
		this.Properties = model.Properties.ToBindableReactiveProperty([]).AddTo(this.CompositeDisposable);
		this.Rate = model.Rate.ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.Description = model.Description.ToBindableReactiveProperty(string.Empty).AddTo(this.CompositeDisposable);
		this.UsageCount = model.UsageCount.ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this._model.AddTo(this.CompositeDisposable);

		this.TagCandidates = model.TagModels.CreateView(x => {
			var categoryViewModel = this._categoryViewModels.GetOrAdd(x.TagCategoryId, _ => new TagCategoryViewModel(x.TagCategory, model.TagsManager, tagModelFactory));
			return new TagViewModel(categoryViewModel, x, model.TagsManager, tagModelFactory);
		});
		this.FilteredTagCandidates = this.TagCandidates.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current).AddTo(this.CompositeDisposable);
		this.Tags = model.Tags.CreateView(x => {
			var categoryViewModel = this._categoryViewModels.GetOrAdd(x.Value.TagCategoryId, _ => new TagCategoryViewModel(x.Value.TagCategory, model.TagsManager, tagModelFactory));
			return new ValueCountPair<TagViewModel>(new TagViewModel(categoryViewModel, x.Value, model.TagsManager, tagModelFactory), x.Count);
		})
			.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current).AddTo(this.CompositeDisposable);

		Observable.Merge(this.TargetFiles.Where(x => x != null).Select(_ => Unit.Default),
				model.ContentChanged.AsObservable())
			.Subscribe(_ => this._model.Refresh(this.TargetFileModels))
			.AddTo(this.CompositeDisposable);

		this.Rate.SubscribeAwait(async (x, ct) => {
			if (!double.IsInteger(x) || this.TargetFiles.Value is null) {
				return;
			}
			await model.UpdateRateAsync(this.TargetFileModels, (int)x);
		}, AwaitOperation.Drop).AddTo(this.CompositeDisposable);

		this.LoadTagCandidatesCommand.SubscribeAwait(async (_, ct) => await model.LoadTagCandidatesAsync(), AwaitOperation.Drop)
			.AddTo(this.CompositeDisposable);

		this.RefreshFilteredTagCandidatesCommand.Subscribe(_ => this.RefreshTagCandidateFilter())
			.AddTo(this.CompositeDisposable);

		this.UpdateDescriptionCommand.SubscribeAwait(async (_, ct) => {
			await model.UpdateDescriptionAsync(this.TargetFileModels.First(), this.Description.Value);
		}, AwaitOperation.Drop).AddTo(this.CompositeDisposable);

		this.RemoveTagCommand.SubscribeAwait(async (x, ct) => {
			await model.RemoveTagAsync(this.TargetFileModels, x.Value.Model.TagId);
		}, AwaitOperation.Drop).AddTo(this.CompositeDisposable);

		this.AddTagCommand.SubscribeAwait(async (_, ct) => {
			if (string.IsNullOrEmpty(this.Text.Value)) {
				return;
			}

			var tag = await model.FindTagByNameAsync(this.Text.Value);
			if (tag is null) {
				this.NewTagRequested.OnNext(new NewTagRequestedContext(this.Text.Value, model.TagCategories));
				return;
			}

			await model.AddTagAsync(this.TargetFileModels, tag);
			this.Text.Value = string.Empty;
		}, AwaitOperation.Drop).AddTo(this.CompositeDisposable);

		this.SearchTaggedFilesCommand.Subscribe(x => {
			searchConditionNotificationDispatcher.UpdateRequest.OnNext(conditions => {
				conditions.Clear();
				var condition = new TagSearchCondition {
					TargetTag = x.Value.Model
				};
				conditions.Add(condition);
			});
		}).AddTo(this.CompositeDisposable);

		this.Text.Subscribe(_ => this.RefreshTagCandidateFilter())
			.AddTo(this.CompositeDisposable);
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