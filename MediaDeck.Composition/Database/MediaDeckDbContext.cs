using MediaDeck.Composition.Tables;
using MediaDeck.Composition.Tables.Metadata;

using Microsoft.EntityFrameworkCore;

namespace MediaDeck.Composition.Database;

/// <summary>
/// コンストラクタ
/// </summary>
/// <param name="dbConnection"></param>
public class MediaDeckDbContext(DbContextOptions dbContextOptions) : DbContext(dbContextOptions) {
	/// <summary>
	/// メディアアイテムテーブル
	/// </summary>
	public DbSet<MediaItem> MediaItems {
		get;
		set;
	} = null!;

	/// <summary>
	/// メディアアイテムテーブル
	/// </summary>
	public DbSet<ImageFile> ImageFiles {
		get;
		set;
	} = null!;

	/// <summary>
	/// メディアアイテムテーブル
	/// </summary>
	public DbSet<VideoFile> VideoFiles {
		get;
		set;
	} = null!;

	/// <summary>
	/// 動画メタデータテーブル
	/// </summary>
	public DbSet<VideoMetadataValue> VideoMetadataValues {
		get;
		set;
	} = null!;

	/// <summary>
	/// 位置情報テーブル
	/// </summary>
	public DbSet<Position> Positions {
		get;
		set;
	} = null!;

	/// <summary>
	/// 位置情報(住所)テーブル
	/// </summary>
	public DbSet<PositionAddress> PositionAddresses {
		get;
		set;
	} = null!;

	/// <summary>
	/// 位置情報(別名)テーブル
	/// </summary>
	public DbSet<PositionNameDetail> PositionNameDetails {
		get;
		set;
	} = null!;

	/// <summary>
	/// タグテーブル
	/// </summary>
	public DbSet<Tag> Tags {
		get;
		set;
	} = null!;

	/// <summary>
	/// タグ別名テーブル
	/// </summary>
	public DbSet<TagAlias> TagAliases {
		get;
		set;
	} = null!;

	/// <summary>
	/// タグ分類テーブル
	/// </summary>
	public DbSet<TagCategory> TagCategories {
		get;
		set;
	} = null!;

	/// <summary>
	/// メディアアイテム・タグ中間テーブル
	/// </summary>
	public DbSet<MediaItemTag> MediaItemTags {
		get;
		set;
	} = null!;

	/// <summary>
	/// フォルダグループメタデータテーブル
	/// </summary>
	public DbSet<FolderGroupMetadata> FolderGroupMetadatas {
		get;
		set;
	} = null!;

	/// <summary>
	/// Jpegメタデータテーブル
	/// </summary>
	public DbSet<Jpeg> Jpegs {
		get;
		set;
	} = null!;

	/// <summary>
	/// Pngメタデータテーブル
	/// </summary>
	public DbSet<Png> Pngs {
		get;
		set;
	} = null!;

	/// <summary>
	/// Bmpメタデータテーブル
	/// </summary>
	public DbSet<Bmp> Bmps {
		get;
		set;
	} = null!;

	/// <summary>
	/// Gifメタデータテーブル
	/// </summary>
	public DbSet<Gif> Gifs {
		get;
		set;
	} = null!;

	/// <summary>
	/// Heifメタデータテーブル
	/// </summary>
	public DbSet<Heif> Heifs {
		get;
		set;
	} = null!;

	/// <summary>
	/// Containerメタデータテーブル
	/// </summary>
	public DbSet<Container> Containers {
		get;
		set;
	} = null!;

	/// <summary>
	/// テーブル設定
	/// </summary>
	/// <param name="modelBuilder"></param>
	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		// Primary Keys
		modelBuilder.Entity<MediaItem>().HasKey(mf => mf.MediaItemId);
		modelBuilder.Entity<ImageFile>().HasKey(i => i.MediaItemId);
		modelBuilder.Entity<VideoFile>().HasKey(v => v.MediaItemId);
		modelBuilder.Entity<VideoMetadataValue>().HasKey(v => new { v.MediaItemId, v.Key });
		modelBuilder.Entity<Position>().HasKey(p => new { p.Latitude, p.Longitude });
		modelBuilder.Entity<PositionAddress>().HasKey(pa => new { pa.Latitude, pa.Longitude, pa.Type });
		modelBuilder.Entity<PositionNameDetail>().HasKey(pnd => new { pnd.Latitude, pnd.Longitude, pnd.Desc });
		modelBuilder.Entity<MediaItemTag>().HasKey(mft => new { mft.MediaItemId, mft.TagId });
		modelBuilder.Entity<Tag>().HasKey(t => t.TagId);
		modelBuilder.Entity<TagAlias>().HasKey(ta => new { ta.TagId, ta.TagAliasId });
		modelBuilder.Entity<TagCategory>().HasKey(tc => tc.TagCategoryId);
		modelBuilder.Entity<Jpeg>().HasKey(j => j.MediaItemId);
		modelBuilder.Entity<Png>().HasKey(p => p.MediaItemId);
		modelBuilder.Entity<Bmp>().HasKey(b => b.MediaItemId);
		modelBuilder.Entity<Gif>().HasKey(b => b.MediaItemId);
		modelBuilder.Entity<Heif>().HasKey(b => b.MediaItemId);
		modelBuilder.Entity<Container>().HasKey(b => b.MediaItemId);
		modelBuilder.Entity<FolderGroupMetadata>().HasKey(f => f.MediaItemId);

		// Index
		modelBuilder.Entity<MediaItem>()
			.HasIndex(x => x.FilePath)
			.IsUnique();

		modelBuilder.Entity<MediaItem>()
			.HasIndex(x => x.FullHash);

		// Relation
		modelBuilder.Entity<ImageFile>()
			.HasOne(i => i.MediaItem)
			.WithOne(m => m.ImageFile)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<VideoFile>()
			.HasOne(v => v.MediaItem)
			.WithOne(m => m.VideoFile)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<VideoMetadataValue>()
			.HasOne(v => v.VideoFile)
			.WithMany(v => v.VideoMetadataValues)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Position>()
			.HasMany(p => p.MediaItems)
			.WithOne(m => m.Position!)
			.HasForeignKey(p => new { p.Latitude, p.Longitude })
			.OnDelete(DeleteBehavior.ClientSetNull);

		modelBuilder.Entity<PositionAddress>()
			.HasOne(pa => pa.Position)
			.WithMany(p => p.Addresses)
			.HasForeignKey(p => new { p.Latitude, p.Longitude })
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<PositionNameDetail>()
			.HasOne(pnd => pnd.Position)
			.WithMany(p => p.NameDetails)
			.HasForeignKey(p => new { p.Latitude, p.Longitude })
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<MediaItemTag>()
			.HasOne(mft => mft.MediaItem)
			.WithMany(mf => mf.MediaItemTags)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<MediaItemTag>()
			.HasOne(mft => mft.Tag)
			.WithMany(t => t.MediaItemTags)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<TagAlias>()
			.HasOne(t => t.Tag)
			.WithMany(t => t.TagAliases)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<TagCategory>()
			.HasMany(tc => tc.Tags)
			.WithOne(t => t.TagCategory!)
			.HasForeignKey(t => t.TagCategoryId)
			.IsRequired(false)
			.OnDelete(DeleteBehavior.SetNull);

		modelBuilder.Entity<Jpeg>()
			.HasOne(j => j.MediaItem)
			.WithOne(m => m.Jpeg)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Png>()
			.HasOne(p => p.MediaItem)
			.WithOne(m => m.Png)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Bmp>()
			.HasOne(b => b.MediaItem)
			.WithOne(m => m.Bmp)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Gif>()
			.HasOne(g => g.MediaItem)
			.WithOne(m => m.Gif)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Heif>()
			.HasOne(g => g.MediaItem)
			.WithOne(m => m.Heif)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Container>()
			.HasOne(g => g.MediaItem)
			.WithOne(m => m.Container)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<FolderGroupMetadata>()
			.HasOne(f => f.MediaItem)
			.WithOne(m => m.FolderGroupMetadata)
			.OnDelete(DeleteBehavior.Cascade);
	}

	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) {
		configurationBuilder.Properties<string>().UseCollation("NOCASE");
		base.ConfigureConventions(configurationBuilder);
	}

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
		base.OnConfiguring(optionsBuilder);

		//		optionsBuilder.LogTo(x => Debug.WriteLine(x), LogLevel.Information,DbContextLoggerOptions.SingleLine);
	}
}