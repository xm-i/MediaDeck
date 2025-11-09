using Microsoft.EntityFrameworkCore;
using MediaDeck.Database.Tables;
using MediaDeck.Database.Tables.Metadata;

namespace MediaDeck.Database;
/// <summary>
/// コンストラクタ
/// </summary>
/// <param name="dbConnection"></param>
public class MediaDeckDbContext(DbContextOptions dbContextOptions) : DbContext(dbContextOptions) {

	/// <summary>
	/// メディアファイルテーブル
	/// </summary>
	public DbSet<MediaFile> MediaFiles {
		get;
		set;
	} = null!;

	/// <summary>
	/// メディアファイルテーブル
	/// </summary>
	public DbSet<ImageFile> ImageFiles {
		get;
		set;
	} = null!;

	/// <summary>
	/// メディアファイルテーブル
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
	/// メディアファイル・タグ中間テーブル
	/// </summary>
	public DbSet<MediaFileTag> MediaFileTags {
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
		modelBuilder.Entity<MediaFile>().HasKey(mf => mf.MediaFileId);
		modelBuilder.Entity<ImageFile>().HasKey(i => i.MediaFileId);
		modelBuilder.Entity<VideoFile>().HasKey(v => v.MediaFileId);
		modelBuilder.Entity<VideoMetadataValue>().HasKey(v => new { v.MediaFileId, v.Key });
		modelBuilder.Entity<Position>().HasKey(p => new { p.Latitude, p.Longitude });
		modelBuilder.Entity<PositionAddress>().HasKey(pa => new { pa.Latitude, pa.Longitude, pa.Type });
		modelBuilder.Entity<PositionNameDetail>().HasKey(pnd => new { pnd.Latitude, pnd.Longitude, pnd.Desc });
		modelBuilder.Entity<MediaFileTag>().HasKey(mft => new { mft.MediaFileId, mft.TagId });
		modelBuilder.Entity<Tag>().HasKey(t => t.TagId);
		modelBuilder.Entity<TagAlias>().HasKey(ta => new { ta.TagId, ta.TagAliasId });
		modelBuilder.Entity<TagCategory>().HasKey(tc => tc.TagCategoryId);
		modelBuilder.Entity<Jpeg>().HasKey(j => j.MediaFileId);
		modelBuilder.Entity<Png>().HasKey(p => p.MediaFileId);
		modelBuilder.Entity<Bmp>().HasKey(b => b.MediaFileId);
		modelBuilder.Entity<Gif>().HasKey(b => b.MediaFileId);
		modelBuilder.Entity<Heif>().HasKey(b => b.MediaFileId);
		modelBuilder.Entity<Container>().HasKey(b => b.MediaFileId);

		// Index
		modelBuilder.Entity<MediaFile>()
			.HasIndex(x => x.FilePath)
			.IsUnique();

		// Relation
		modelBuilder.Entity<ImageFile>()
			.HasOne(i => i.MediaFile)
			.WithOne(m => m.ImageFile)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<VideoFile>()
			.HasOne(v => v.MediaFile)
			.WithOne(m => m.VideoFile)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<VideoMetadataValue>()
			.HasOne(v => v.VideoFile)
			.WithMany(v => v.VideoMetadataValues)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Position>()
			.HasMany(p => p.MediaFiles)
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

		modelBuilder.Entity<MediaFileTag>()
			.HasOne(mft => mft.MediaFile)
			.WithMany(mf => mf.MediaFileTags)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<MediaFileTag>()
			.HasOne(mft => mft.Tag)
			.WithMany(t => t.MediaFileTags)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<TagAlias>()
			.HasOne(t => t.Tag)
			.WithMany(t => t.TagAliases)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<TagCategory>()
			.HasMany(tc => tc.Tags)
			.WithOne(t => t.TagCategory)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Jpeg>()
			.HasOne(j => j.MediaFile)
			.WithOne(m => m.Jpeg)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Png>()
			.HasOne(p => p.MediaFile)
			.WithOne(m => m.Png)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Bmp>()
			.HasOne(b => b.MediaFile)
			.WithOne(m => m.Bmp)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Gif>()
			.HasOne(g => g.MediaFile)
			.WithOne(m => m.Gif)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Heif>()
			.HasOne(g => g.MediaFile)
			.WithOne(m => m.Heif)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Container>()
			.HasOne(g => g.MediaFile)
			.WithOne(m => m.Container)
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
