using MediaDeck.Composition.Enum;

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
	/// このプログラム設定を削除するコマンド
	/// </summary>
	public ReactiveCommand RemoveCommand {
		get;
	}
}