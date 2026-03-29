using MediaDeck.Database.Tables;

namespace MediaDeck.Core.Models.NotificationDispatcher;

public static class FileNotifications {
	public static Subject<MediaFile> FileRegistered {
		get;
	} = new();
}