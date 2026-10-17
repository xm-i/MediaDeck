using System.Text.Json;
using MediaDeck.Common.Base;

namespace MediaDeck.ViewModels.Help;

/// <summary>
/// ライセンス情報ウィンドウのViewModel
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class LicenseWindowViewModel : ViewModelBase {
	private static readonly Lazy<IReadOnlyList<LicenseInfo>> _cachedLicenses = new(LoadLicenses);

	/// <summary>
	/// コンストラクタ
	/// </summary>
	public LicenseWindowViewModel() {
		this.Licenses = _cachedLicenses.Value;

		this.SelectedLicense = new BindableReactiveProperty<LicenseInfo?>().AddTo(this.CompositeDisposable);

		// 初期選択
		this.SelectedLicense.Value = this.Licenses.FirstOrDefault();
	}

	/// <summary>
	/// ライセンス情報のリスト
	/// </summary>
	public IReadOnlyList<LicenseInfo> Licenses {
		get;
	}

	/// <summary>
	/// 選択されているライセンス情報
	/// </summary>
	public BindableReactiveProperty<LicenseInfo?> SelectedLicense {
		get;
	}

	/// <summary>
	/// 埋め込みリソースからライセンス情報を読み込みます。
	/// </summary>
	/// <returns>読み込まれたライセンス情報のリスト</returns>
	private static IReadOnlyList<LicenseInfo> LoadLicenses() {
		var assembly = typeof(LicenseWindowViewModel).Assembly;
		var resourceName = assembly.GetManifestResourceNames()
			.FirstOrDefault(n => n.EndsWith("licenses.json", StringComparison.OrdinalIgnoreCase))
			?? throw new InvalidOperationException("licenses.json embedded resource not found.");

		using var stream = assembly.GetManifestResourceStream(resourceName)
			?? throw new InvalidOperationException($"Failed to load manifest resource stream for '{resourceName}'.");

		return JsonSerializer.Deserialize(stream, LicenseJsonSerializerContext.Default.ListLicenseInfo)
			?? [];
	}
}

/// <summary>
/// ライセンス情報
/// </summary>
/// <param name="LibraryName">ライブラリ名</param>
/// <param name="LicenseName">ライセンス名</param>
/// <param name="Url">URL</param>
/// <param name="LicenseText">ライセンス本文</param>
public record LicenseInfo(string LibraryName, string LicenseName, string Url, string LicenseText);