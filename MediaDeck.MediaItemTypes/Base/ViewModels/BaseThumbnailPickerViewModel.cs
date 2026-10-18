using System.IO;
using System.Threading.Tasks;
using ImageMagick;
using MediaDeck.Common.Base;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.Base.ViewModels;

public abstract class BaseThumbnailPickerViewModel<TThumbnailPickerModel> : ViewModelBase, IThumbnailPickerViewModel
	where TThumbnailPickerModel : BaseThumbnailPickerModel {
	public BaseThumbnailPickerViewModel(TThumbnailPickerModel thumbnailPickerModel, IFilePickerService filePickerService, ConfigModel config) {
		this._filePickerService = filePickerService;
		this._config = config;
		this.RecreateThumbnailCommand.SubscribeAwait(async (_, ct) => await this.RecreateThumbnailAsync(), AwaitOperation.Drop).AddTo(this.CompositeDisposable);
		this.PickThumbnailFromFileCommand.SubscribeAwait(async (_, ct) => await this.PickThumbnailFromFileAsync(), AwaitOperation.Drop).AddTo(this.CompositeDisposable);
		this.SaveCommand.SubscribeAwait(async (_, ct) => await this.SaveAsync(), AwaitOperation.Drop).AddTo(this.CompositeDisposable);
		this.thumbnailPickerModel = thumbnailPickerModel;

	}

	private readonly IFilePickerService _filePickerService;
	protected readonly ConfigModel _config;

	protected IMediaItemViewModel? targetFileViewModel;
	protected TThumbnailPickerModel thumbnailPickerModel;

	public MediaType MediaType {
		get;
		private set;
	} = MediaType.Unknown;

	public BindableReactiveProperty<byte[]?> OriginalThumbnail {
		get;
	} = new();

	public BindableReactiveProperty<byte[]?> CandidateThumbnail {
		get;
	} = new();

	public ReactiveCommand RecreateThumbnailCommand {
		get;
	} = new();

	public ReactiveCommand PickThumbnailFromFileCommand {
		get;
	} = new();

	public ReactiveCommand SaveCommand {
		get;
	} = new();

	public abstract Task RecreateThumbnailAsync();

	/// <summary>
	/// ユーザーが選択した任意の画像ファイルを候補サムネイルとして読み込む。
	/// 実体への保存は <see cref="SaveAsync"/> 経由で既存パイプラインに委譲する。
	/// </summary>
	protected virtual async Task PickThumbnailFromFileAsync() {
		var bytes = await this._filePickerService.PickImageAsync();
		if (bytes is null) {
			return;
		}

		var thumbSize = (uint)this._config.ThumbnailConfig.ThumbnailSize.Value;
		this.CandidateThumbnail.Value = await Task.Run(() => {
			using var input = new MemoryStream(bytes);
			using var output = new MemoryStream();
			using var image = new MagickImage(input);
			image.AutoOrient();
			image.Thumbnail(thumbSize, thumbSize);
			image.Format = MagickFormat.Jpg;
			image.Write(output);
			return output.ToArray();
		});
	}

	public virtual async Task SaveAsync() {
		if (this.targetFileViewModel is null) {
			return;
		}
		if (this.CandidateThumbnail.Value is null) {
			return;
		}
		await this.thumbnailPickerModel.UpdateThumbnailAsync(this.targetFileViewModel.FileModel, this.CandidateThumbnail.Value, this._config.ThumbnailConfig.ThumbnailSize.Value);
		this.targetFileViewModel.RefreshThumbnail();
	}

	public virtual async Task LoadAsync(IMediaItemViewModel fileViewModel) {
		this.targetFileViewModel = fileViewModel;
		this.MediaType = fileViewModel.MediaType;
		this.CandidateThumbnail.Value = null;
		this.OriginalThumbnail.Value = await this.thumbnailPickerModel.LoadThumbnailAsync(fileViewModel.FileModel);
	}
}