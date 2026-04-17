using MediaDeck.Core.Services.FileChangeMonitor;

namespace MediaDeck.Core.Tests.Models.Services;

/// <summary>
/// <see cref="FileChangeItem"/> のテストクラスです。
/// </summary>
public class FileChangeItemTests {
	/// <summary>
	/// デフォルト値が正しく設定されているかをテストします。
	/// </summary>
	[Fact]
	public void DefaultConstructor_SetsExpectedDefaultValues() {
		// Arrange & Act
		var item = new FileChangeItem();

		// Assert
		Assert.Null(item.MediaFileId);

		// CreatedAt は現在時刻に近い値であることを確認
		var timeDifference = DateTime.UtcNow - item.CreatedAt;
		Assert.True(timeDifference.TotalSeconds < 1, "CreatedAt が現在時刻に近い値ではありません");

		Assert.False(item.IsPending);
		Assert.Equal(string.Empty, item.OldPath);
		Assert.Equal(string.Empty, item.NewPath);
		Assert.Equal(default, item.ChangeType);
		Assert.False(item.IsHashing);
		Assert.Equal(0, item.FileSize);
		Assert.Equal(string.Empty, item.OldHash);
		Assert.Equal(string.Empty, item.NewHash);

		// デフォルトの ChangeType は 0 (Deleted) になるため、対応するテキストを検証
		Assert.Equal("削除", item.ChangeTypeText);
	}

	/// <summary>
	/// 各プロパティの Get/Set が正しく動作するかをテストします。
	/// </summary>
	[Fact]
	public void Properties_CanBeSetAndRetrieved() {
		// Arrange
		var item = new FileChangeItem();
		var now = DateTime.UtcNow;

		// Act
		item.MediaFileId = 123;
		item.CreatedAt = now;
		item.IsPending = true;
		item.OldPath = "C:\\old\\path.txt";
		item.NewPath = "C:\\new\\path.txt";
		item.ChangeType = FileChangeType.Moved;
		item.IsHashing = true;
		item.FileSize = 1024;
		item.OldHash = "oldhash";
		item.NewHash = "newhash";

		// Assert
		Assert.Equal(123, item.MediaFileId);
		Assert.Equal(now, item.CreatedAt);
		Assert.True(item.IsPending);
		Assert.Equal("C:\\old\\path.txt", item.OldPath);
		Assert.Equal("C:\\new\\path.txt", item.NewPath);
		Assert.Equal(FileChangeType.Moved, item.ChangeType);
		Assert.True(item.IsHashing);
		Assert.Equal(1024, item.FileSize);
		Assert.Equal("oldhash", item.OldHash);
		Assert.Equal("newhash", item.NewHash);
	}

	/// <summary>
	/// <see cref="FileChangeType"/> に応じて正しい <see cref="FileChangeItem.ChangeTypeText"/> が返されるかをテストします。
	/// </summary>
	[Theory]
	[InlineData(FileChangeType.Deleted, "削除")]
	[InlineData(FileChangeType.Renamed, "変更")]
	[InlineData(FileChangeType.Moved, "移動")]
	[InlineData(FileChangeType.Added, "追加")]
	public void ChangeTypeText_ReturnsExpectedText(FileChangeType changeType, string expectedText) {
		// Arrange
		var item = new FileChangeItem {
			ChangeType = changeType
		};

		// Act
		var result = item.ChangeTypeText;

		// Assert
		Assert.Equal(expectedText, result);
	}

	/// <summary>
	/// 定義されていない <see cref="FileChangeType"/> の場合、"不明" が返されるかをテストします。
	/// </summary>
	[Fact]
	public void ChangeTypeText_WithInvalidChangeType_ReturnsUnknown() {
		// Arrange
		var item = new FileChangeItem {
			ChangeType = (FileChangeType)999
		};

		// Act
		var result = item.ChangeTypeText;

		// Assert
		Assert.Equal("不明", result);
	}
}