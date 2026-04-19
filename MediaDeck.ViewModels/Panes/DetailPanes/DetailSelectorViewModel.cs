using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Core.Models.Files;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Models.NotificationDispatcher;
using MediaDeck.Core.Primitives;
using MediaDeck.ViewModels.Tags;

namespace MediaDeck.ViewModels.Panes.DetailPanes;

public record NewTagRequestedContext(string TagName, ITagCategoryModel? SelectedCategory);

[Inject(InjectServiceLifetime.Scoped)]
public class DetailSelectorViewModel : ViewModelBase {
	public Subject<NewTagRequestedContext> NewTagRequested {
		get;
	} = new();

	private readonly DetailSelectorModel _model;
	private readonly System.Collections.Concurrent.ConcurrentDictionary<int, TagCategoryViewModel> _categoryViewModels = new();

	public ITagModelFactory TagModelFactory {
		get;
	}

	public DetailSelectorViewModel(DetailSelectorModel model,
		SearchConditionNotificationDispatcher searchConditionNotificationDispatcher,
		ITagModelFactory tagModelFactory) {
		this._model = model;
		this.TagModelFactory = tagModelFactory;
		this.RepresentativeFilePath = model.RepresentativeFilePath.ToBindableReactiveProperty(string.Empty).AddTo(this.CompositeDisposable);
		this.Properties = model.Properties.ToBindableReactiveProperty([]).AddTo(this.CompositeDisposable);
		this.Rate = model.Rate.ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.Description = model.Description.ToBindableReactiveProperty(string.Empty).AddTo(this.CompositeDisposable);
		this.UsageCount = model.UsageCount.ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this._model.AddTo(this.CompositeDisposable);

		this.TagCandidates = model.TagModels.CreateView(x => {
			var categoryViewModel = this.GetCategoryViewModel(x.TagCategory, model.TagsManager, tagModelFactory);
			return new TagViewModel(categoryViewModel, x, model.TagsManager, tagModelFactory);
		});
		this.FilteredTagCandidates = this.TagCandidates.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current).AddTo(this.CompositeDisposable);

		this.Tags = model.Tags.CreateView(x => {
			var categoryViewModel = this.GetCategoryViewModel(x.Value.TagCategory, model.TagsManager, tagModelFactory);
			return new ValueCountPair<TagViewModel>(new TagViewModel(categoryViewModel, x.Value, model.TagsManager, tagModelFactory), x.Count);
		})
			.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current).AddTo(this.CompositeDisposable);

		Observable.Merge(this.TargetFiles.Where(x => x != null).Select(_ => Unit.Default),
				model.ContentChanged.AsObservable())
			.Subscribe(_ => this._model.Refresh(this.TargetFileModels))
			.AddTo(this.CompositeDisposable);

		this.UpdateRateCommand.SubscribeAwait(async (x, ct) => {
			if (!double.IsInteger(x) || this.TargetFiles.Value is null) {
				return;
			}
			await model.UpdateRateAsync(this.TargetFileModels, (int)x);
		}, AwaitOperation.Drop).AddTo(this.CompositeDisposable);

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

	public ReactiveCommand<TagViewModel> AddSpecificTagCommand {
		get;
	} = new();

	public ReactiveCommand<double> UpdateRateCommand {
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
		this.TagCandidates.AttachFilter((m, vm) => {
			var result = DetailSelectorModel.MatchesTagFilter(m, this.Text.Value, out var representativeText);
			vm.RepresentativeText.Value = representativeText ?? string.Empty;
			return result;
		});
	}

	private TagCategoryViewModel GetCategoryViewModel(ITagCategoryModel category, ITagsManager tagsManager, ITagModelFactory factory) {
		// ConcurrentDictionaryのキーはnotnull制約があるため、IDがnull(未設定)の場合はint.MinValueをキーとして使用します。
		// このマジックナンバーはキャッシュ管理のためだけに内部で使用され、外部（モデルやUI）には影響しません。
		var key = category.TagCategoryId ?? int.MinValue;
		return this._categoryViewModels.GetOrAdd(key, _ => new TagCategoryViewModel(category, tagsManager, factory));
	}
}