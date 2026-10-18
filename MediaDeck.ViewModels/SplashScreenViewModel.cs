using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces;

namespace MediaDeck.ViewModels;

/// <summary>
/// スプラッシュ画面のViewModel。
/// </summary>
public class SplashScreenViewModel : ViewModelBase {
	/// <summary>
	/// 現在の状態メッセージ。
	/// </summary>
	public BindableReactiveProperty<string> StatusMessage {
		get;
	}

	/// <summary>
	/// アプリケーションのバージョン。
	/// </summary>
	public string AppVersion {
		get;
	}

	private readonly ReactiveProperty<string> _statusMessage;

	/// <summary>
	/// <see cref="SplashScreenViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="stringProvider">文字列プロバイダー</param>
	public SplashScreenViewModel(IStringProvider? stringProvider = null) {
		var initialMessage = stringProvider?.GetString("Splash_Starting") ?? "起動しています...";
		this._statusMessage = new ReactiveProperty<string>(initialMessage).AddTo(this.CompositeDisposable);
		this.StatusMessage = this._statusMessage.ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty<string>();
		this.StatusMessage.AddTo(this.CompositeDisposable);

		// バージョン情報の取得
		var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
		this.AppVersion = version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v1.0.0";
	}

	/// <summary>
	/// ステータスメッセージを更新します。
	/// </summary>
	/// <param name="message">メッセージ</param>
	public void UpdateStatus(string message) {
		this._statusMessage.Value = message;
	}
}