using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;

namespace MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;

/// <summary>
/// 実行設定項目の ViewModel インターフェース。
/// </summary>
public interface IExecutionProgramConfigViewModel {
	/// <summary>
	/// メディアタイプ
	/// </summary>
	public MediaType MediaType {
		get;
	}

	/// <summary>
	/// メディアタイプ固有の実行設定ビュー
	/// </summary>
	public BindableReactiveProperty<IExecutionConfigView?> ConfigView {
		get;
	}

	/// <summary>
	/// このプログラム設定を削除するコマンド
	/// </summary>
	public ReactiveCommand RemoveCommand {
		get;
	}
}