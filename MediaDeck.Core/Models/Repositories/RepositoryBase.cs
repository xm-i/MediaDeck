using MediaDeck.Common.Base;

namespace MediaDeck.Core.Models.Repositories;

/// <summary>
/// メディアのリポジトリ階層の読み込みを管理する基底モデルクラス。
/// </summary>
public abstract class RepositoryBase : ModelBase {
	/// <summary>
	/// リポジトリの階層データを非同期で読み込みます。
	/// </summary>
	/// <param name="cancellationToken">キャンセルトークン。</param>
	/// <returns>読み込み処理を表すタスク。</returns>
	public abstract Task Load(CancellationToken cancellationToken = default);
}