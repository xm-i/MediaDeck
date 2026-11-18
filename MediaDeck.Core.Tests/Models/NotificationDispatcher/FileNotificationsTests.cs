using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Models.NotificationDispatcher;

using R3;

using Shouldly;

namespace MediaDeck.Core.Tests.Models.NotificationDispatcher;

public class FileNotificationsTests {
	[Fact]
	public void FileRegistered_IsNotNull() {
		FileNotifications.FileRegistered.ShouldNotBeNull();
	}

	[Fact]
	public void FileRegistered_EmitsValue() {
		MediaItem? received = null;
		using var subscription = FileNotifications.FileRegistered.Subscribe(x => received = x);

		var item = new MediaItem { FilePath = "test.jpg", MediaType = MediaType.Image, DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false };
		FileNotifications.FileRegistered.OnNext(item);

		received.ShouldNotBeNull();
		received!.FilePath.ShouldBe("test.jpg");
	}

	[Fact]
	public void FilesChanged_EmitsSignal() {
		var received = false;
		using var subscription = FileNotifications.FilesChanged.Subscribe(_ => received = true);

		FileNotifications.FilesChanged.OnNext(Unit.Default);

		received.ShouldBeTrue();
	}
}