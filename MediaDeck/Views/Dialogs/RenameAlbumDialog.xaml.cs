using System.Threading.Tasks;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Models.Repositories;
using MediaDeck.Core.Models.Repositories.Objects;
using MediaDeck.Core.Stores.Config;
using MediaDeck.Views.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views.Dialogs;

/// <summary>
/// アルバムの名前やパスを変更するダイアログ。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public sealed partial class RenameAlbumDialog : ContentDialog {
	private readonly CompositeDisposable _disposable = new();
	private readonly IStringProvider _stringProvider;
	private Func<string, Task<AlbumRenameResult>>? _renameFunc;

	/// <summary>
	/// <see cref="RenameAlbumDialog"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="configStore">設定ストア。</param>
	/// <param name="stringProvider">ローカライズ文字列プロバイダ。</param>
	public RenameAlbumDialog(IConfigStore configStore, IStringProvider stringProvider) {
		this._stringProvider = stringProvider;
		this.AlbumName = new BindableReactiveProperty<string>(string.Empty).AddTo(this._disposable);
		this.AlbumPath = new BindableReactiveProperty<string>(string.Empty).AddTo(this._disposable);
		this.ErrorMessage = new BindableReactiveProperty<string>(string.Empty).AddTo(this._disposable);

		this.InitializeComponent();

		this.AlbumName
			.Merge(this.AlbumPath)
			.Subscribe(_ => this.ErrorMessage.Value = string.Empty)
			.AddTo(this._disposable);

		ThemeHelper.BindTheme(this, configStore, this._disposable);
		this.Closed += (_, _) => this._disposable.Dispose();
	}

	/// <summary>
	/// アルバム名を取得します。
	/// </summary>
	public BindableReactiveProperty<string> AlbumName {
		get;
	}

	/// <summary>
	/// 親アルバムのパスを取得します。
	/// </summary>
	public BindableReactiveProperty<string> AlbumPath {
		get;
	}

	/// <summary>
	/// エラーメッセージを取得します。
	/// </summary>
	public BindableReactiveProperty<string> ErrorMessage {
		get;
	}

	/// <summary>
	/// 入力されたアルバムパスとアルバム名を結合したフルパスを取得します。
	/// </summary>
	public string FullAlbumPath {
		get {
			return string.Join(
				AlbumObject.Separator,
				new[] { this.AlbumPath.Value, this.AlbumName.Value }
					.SelectMany(part => (part ?? string.Empty)
						.Replace('\\', AlbumObject.Separator)
						.Split(AlbumObject.Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)));
		}
	}

	/// <summary>
	/// 現在のアルバムパスとリネーム実行処理を指定してダイアログを初期化します。
	/// </summary>
	/// <param name="currentAlbumPath">変更前のアルバムパス。</param>
	/// <param name="renameFunc">確定時に新しいパスを受け取ってリネームを実行するコールバック。</param>
	public void Initialize(string currentAlbumPath, Func<string, Task<AlbumRenameResult>> renameFunc) {
		this._renameFunc = renameFunc;
		var lastSlash = currentAlbumPath.LastIndexOf(AlbumObject.Separator);
		if (lastSlash >= 0) {
			this.AlbumPath.Value = currentAlbumPath[..lastSlash];
			this.AlbumName.Value = currentAlbumPath[(lastSlash + 1)..];
		} else {
			this.AlbumPath.Value = string.Empty;
			this.AlbumName.Value = currentAlbumPath;
		}
		this.ErrorMessage.Value = string.Empty;
	}

	private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
		var nameSegments = (this.AlbumName.Value ?? string.Empty)
			.Replace('\\', AlbumObject.Separator)
			.Split(AlbumObject.Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (nameSegments.Length == 0) {
			this.ErrorMessage.Value = this._stringProvider.GetString("RenameAlbumDialog_Error_EmptyName");
			args.Cancel = true;
			return;
		}

		var newPath = this.FullAlbumPath;
		if (string.IsNullOrWhiteSpace(newPath) || this._renameFunc is null) {
			args.Cancel = true;
			return;
		}

		var deferral = args.GetDeferral();
		try {
			var result = await this._renameFunc(newPath);
			if (result == AlbumRenameResult.Success) {
				this.ErrorMessage.Value = string.Empty;
				return;
			}
			this.ErrorMessage.Value = result switch {
				AlbumRenameResult.InvalidDestinationUnderSource => this._stringProvider.GetString("RenameAlbumDialog_Error_UnderSource"),
				AlbumRenameResult.DuplicatePath => this._stringProvider.GetString("RenameAlbumDialog_Error_Duplicate"),
				AlbumRenameResult.NotFound => this._stringProvider.GetString("RenameAlbumDialog_Error_NotFound"),
				_ => string.Empty,
			};
			args.Cancel = true;
		} finally {
			deferral.Complete();
		}
	}

	private void AlbumNameTextBox_Loaded(object sender, RoutedEventArgs e) {
		this.AlbumNameTextBox.Focus(FocusState.Programmatic);
		this.AlbumNameTextBox.SelectAll();
	}
}