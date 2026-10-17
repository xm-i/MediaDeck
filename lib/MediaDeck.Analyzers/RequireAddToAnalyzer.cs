using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MediaDeck.Analyzers {
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class RequireAddToAnalyzer : DiagnosticAnalyzer {
		public const string DiagnosticId = "MD0001";
		private static readonly LocalizableString Title = "IDisposable requires AddTo or Using/Dispose";
		private static readonly LocalizableString MessageFormat = "The returned IDisposable must be disposed or added to CompositeDisposable using AddTo(...)";
		private static readonly LocalizableString Description = "Ensure that objects like ReactiveProperty, ReactiveCommand, ObservableList, and methods like Subscribe/SubscribeAwait are either disposed or added to CompositeDisposable so memory leaks do not occur.";
		private const string Category = "Usage";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			DiagnosticId,
			Title,
			MessageFormat,
			Category,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		public override void Initialize(AnalysisContext context) {
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterSyntaxNodeAction(AnalyzeExpression, SyntaxKind.InvocationExpression, SyntaxKind.ObjectCreationExpression);
		}

		private static void AnalyzeExpression(SyntaxNodeAnalysisContext context) {
			var expression = (ExpressionSyntax)context.Node;
			CheckIsDisposedOrAdded(context, expression);
		}


		private static void CheckIsDisposedOrAdded(SyntaxNodeAnalysisContext context, ExpressionSyntax node) {
			if (IsAddToInvocation(node)) {
				return;
			}

			if (IsIgnoredInvocation(node)) {
				return;
			}

			// If it's part of a Chained .AddTo(...) call, it's handled.
			if (HasChainedAddTo(node)) {
				return;
			}

			// Check if it is "handled" by language constructs or patterns.
			// Assignments to .Disposable (e.g. SerialDisposable.Disposable) or static members are permitted.
			// Other assignments (AssignmentExpressionSyntax) without AddTo are flagged to detect leaks.
			var ancestor = node.FirstAncestorOrSelf<SyntaxNode>(n =>
				n is ReturnStatementSyntax ||
				n is YieldStatementSyntax ||
				n is ArrowExpressionClauseSyntax ||
				n is EqualsValueClauseSyntax ||
				n is ArgumentSyntax ||
				(n is LocalDeclarationStatementSyntax l && l.UsingKeyword.IsKind(SyntaxKind.UsingKeyword)) ||
				n is UsingStatementSyntax ||
				IsHandledAssignment(n, context.SemanticModel));

			if (ancestor != null) {
				return;
			}

			// A more robust check using semantic model to see if it returns `IDisposable`.
			var typeInfo = context.SemanticModel.GetTypeInfo(node);
			ITypeSymbol? type = typeInfo.Type;

			if (type == null) {
				var symbolInfo = context.SemanticModel.GetSymbolInfo(node);
				if (symbolInfo.Symbol is IMethodSymbol methodSymbol) {
					type = methodSymbol.ReturnType;
				}
			}

			if (type != null && ImplementsIDisposable(type)) {
				// If not handled and missing AddTo, report it.
				var diagnostic = Diagnostic.Create(Rule, node.GetLocation());
				context.ReportDiagnostic(diagnostic);
			}
		}

		private static bool HasChainedAddTo(SyntaxNode node) {
			var current = node;
			while (current?.Parent is MemberAccessExpressionSyntax ma) {
				if (ma.Name.Identifier.Text == "AddTo") {
					return true;
				}

				current = ma.Parent; // ma.Parent is InvocationExpression (e.g. `.Subscribe(...)`), which becomes the new `current` to check its parent in the next iteration.
			}
			return false;
		}

		private static bool IsAddToInvocation(SyntaxNode node) {
			if (node is InvocationExpressionSyntax inv && inv.Expression is MemberAccessExpressionSyntax ma) {
				return ma.Name.Identifier.Text == "AddTo";
			}
			return false;
		}

		/// <summary>
		/// 代入式が破棄管理されている（SerialDisposable.Disposable への代入や静的メンバへの代入など）かどうかを判定します。
		/// </summary>
		/// <param name="n">判定対象の構文ノード</param>
		/// <param name="semanticModel">セマンティックモデル</param>
		/// <returns>破棄管理された代入式の場合 true</returns>
		private static bool IsHandledAssignment(SyntaxNode n, SemanticModel semanticModel) {
			if (n is AssignmentExpressionSyntax assignment) {
				// SerialDisposable.Disposable = ... のような Disposable プロパティへの代入は破棄管理されているため許可する
				if (assignment.Left is MemberAccessExpressionSyntax ma && ma.Name.Identifier.Text == "Disposable") {
					return true;
				}

				// 静的メンバ（例: Log.Logger）への代入は CompositeDisposable に追加できないため許可する
				var leftSymbol = semanticModel.GetSymbolInfo(assignment.Left).Symbol;
				if (leftSymbol?.IsStatic == true) {
					return true;
				}

				return false;
			}
			return false;
		}

		/// <summary>
		/// メソッド呼び出しが解析対象から除外すべき呼び出し（DI コンテナからのサービス取得など）かどうかを判定します。
		/// </summary>
		/// <param name="node">判定対象の式</param>
		/// <returns>除外すべき呼び出しの場合 true</returns>
		private static bool IsIgnoredInvocation(ExpressionSyntax node) {
			if (node is InvocationExpressionSyntax inv && inv.Expression is MemberAccessExpressionSyntax ma) {
				var name = ma.Name.Identifier.Text;
				// DI コンテナからのサービス取得はコンテナがライフサイクルを管理するため除外する
				if (name is "GetRequiredService" or "GetService" or "CreateScope") {
					return true;
				}
			}
			return false;
		}

		private static bool ImplementsIDisposable(ITypeSymbol type) {
			if (type == null)
				return false;

			// Exclude Task and ValueTask from being flagged, as they are rarely disposed manually.
			if (type.Name.Contains("Task")) {
				return false;
			}

			if (type.Name == "IDisposable" && type.ContainingNamespace.Name == "System")
				return true;

			foreach (var iface in type.AllInterfaces) {
				if (iface.Name == "IDisposable" && iface.ContainingNamespace.Name == "System") {
					return true;
				}
			}
			return false;
		}
	}
}