using System.Collections.Generic;
using System.Linq.Expressions;

namespace MediaDeck.Utils.Tools;

/// <summary>
/// パラメータ上書きクラス
/// </summary>
/// <remarks>
/// コンストラクタ
/// </remarks>
/// <param name="parameters">上書きするパラメータ</param>
public class ParameterVisitor(IEnumerable<ParameterExpression> parameters) : ExpressionVisitor {
	/// <summary>
	/// パラメータ保持
	/// </summary>
	private readonly IDictionary<(Type, string?), ParameterExpression> _parameters = parameters.ToDictionary(p => (p.Type, p.Name));

	/// <summary>
	/// パラメータ
	/// </summary>
	public ICollection<ParameterExpression> Parameters {
		get {
			return this._parameters.Values;
		}
	}

	/// <summary>
	/// パラメータ選択
	/// </summary>
	/// <remarks>
	/// 対象のパラメータと同一型、同一名のパラメータを保持していれば上書きする。
	/// </remarks>
	/// <param name="node">対象パラメータ</param>
	/// <returns>上書きするパラメータ</returns>
	protected override Expression VisitParameter(ParameterExpression node) {
		var key = (node.Type, node.Name);
		return this._parameters.TryGetValue(key, out ParameterExpression? value) ? value : node;
	}
}