using MediaDeck.Composition.Enum;

using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Interfaces.MediaItemTypes.Models;

/// <summary>
/// 実行設定オブジェクトのインターフェース。
/// 各メディアタイプはこのインターフェースを実装し、固有のプロパティを定義する。
/// </summary>
[GenerateR3JsonConfigDto]
public interface IExecutionProgramObjectModel {
	/// <summary>
	/// 対象のメディアタイプ
	/// </summary>
	public MediaType MediaType {
		get;
		set;
	}
}