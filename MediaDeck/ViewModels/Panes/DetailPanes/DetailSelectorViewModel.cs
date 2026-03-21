using MediaDeck.Composition.Bases;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.FileTypes.Base.ViewModels.Interfaces;
using MediaDeck.Models.FileDetailManagers;
using MediaDeck.Models.FileDetailManagers.Objects;
using MediaDeck.Models.Files.SearchConditions;
using MediaDeck.Models.NotificationDispatcher;
using MediaDeck.Utils.Objects;
using MediaDeck.Database.Tables;
using System.Threading.Tasks;

namespace MediaDeck.ViewModels.Panes.DetailPanes;

[Inject(InjectServiceLifetime.Transient)]
public class DetailSelectorViewModel : ViewModelBase {
	private readonly DetailSelectorModel _model;

	public DetailSelectorViewModel(DetailSelectorModel model, SearchConditionNotificationDispatcher searchConditionNotificationDispatcher) {
		this._model = model;
		this.RepresentativeFilePath = model.RepresentativeFilePath.ToBindableReactiveProperty(string.Empty);
		this.Properties = model.Properties.ToBindableReactiveProperty([]);
		this.Rate = model.Rate.ToBindableReactiveProperty();
		this.Description = model.Description.ToBindableReactiveProperty(string.Empty);
		this.UsageCount = model.UsageCount.ToBindableReactiveProperty();
		this._model.AddTo(this.CompositeDisposable);

		this.TagCandidates = model.TagModels.CreateView(x => x);
		this.FilteredTagCandidates = this.TagCandidates.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
		this.Tags = model.Tags.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

		Observable.Merge(
			this.TargetFiles.Where(x => x != null).Select(_ => Unit.Default),
			model.ContentChanged.AsObservable()
		).Subscribe(_ => this._model.Refresh(this.TargetFileModels));

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
			await model.RemoveTagAsync(this.TargetFileModels, x.Value.TagId);
		});
		this.AddTagCommand.Subscribe(async _ => {
			if (string.IsNullOrEmpty(this.Text.Value)) {
				return;
			}

			var tag = await model.FindTagByNameAsync(this.Text.Value);
			if (tag is null) {
				this.NewTagRequested.OnNext(new NewTagRequestedContext(this.Text.Value, model.TagCategories));
				return;
			}

			await model.AddTagAsync(this.TargetFileModels, tag);
			this.Text.Value = "";
		});
		this.SearchTaggedFilesCommand.Subscribe(x => {
			searchConditionNotificationDispatcher.AddRequest.OnNext(new TagSearchCondition(x.Value));
		});
	}

	private IFileModel[] TargetFileModels {
		get {
			var files = this.TargetFiles.Value;
			return files is null ? [] : [.. files.Select(x => x.FileModel)];
		}
	}

	public BindableReactiveProperty<IFileViewModel[]> TargetFiles {
		get;
	} = new([]);

	public BindableReactiveProperty<string> RepresentativeFilePath {
		get;
	}

	public BindableReactiveProperty<string> Text {
		get;
	} = new();

	public ISynchronizedView<ITagModel, ITagModel> TagCandidates {
		get;
	}

	public INotifyCollectionChangedSynchronizedViewList<ITagModel> FilteredTagCandidates {
		get;
	}

	public ReactiveCommand RefreshFilteredTagCandidatesCommand {
		get;
	} = new();

	public ReactiveCommand LoadTagCandidatesCommand {
		get;
	} = new();

	public INotifyCollectionChangedSynchronizedViewList<ValueCountPair<ITagModel>> Tags {
		get;
	}

	public ReactiveCommand<ValueCountPair<ITagModel>> SearchTaggedFilesCommand {
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

	public ReactiveCommand<ValueCountPair<ITagModel>> RemoveTagCommand {
		get;
	} = new();

	public ReactiveCommand AddTagCommand {
		get;
	} = new();

	public Subject<NewTagRequestedContext> NewTagRequested {
		get;
	} = new();

	public async Task OnNewTagCreated(Tag tag) {
		await this._model.AddTagAsync(this.TargetFileModels, tag);
		this.Text.Value = "";
	}

	internal TagsManager GetTagsManager() {
		return this._model.TagsManager;
	}

	private void RefreshTagCandidateFilter() {
		this.TagCandidates.AttachFilter(tag => {
			var matched = DetailSelectorModel.MatchesTagFilter(tag, this.Text.Value ?? "", out var representativeText);
			tag.RepresentativeText.Value = representativeText;
			return matched;
		});
	}
}

public record NewTagRequestedContext(string TagName, ObservableList<TagCategory> TagCategories);
