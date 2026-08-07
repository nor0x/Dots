using System.Diagnostics;
using System.IO;
using System.Reactive.Threading.Tasks;
using Akavache;

namespace Dots.Services;

/// <param name="Bytes">Total size on disk.</param>
public readonly record struct CacheBucket(int Files, long Bytes)
{
	public bool IsEmpty => Files == 0;
	public string Display => $"{Files} file{(Files == 1 ? "" : "s")} - {CacheService.FormatSize(Bytes)}";
}

public readonly record struct CacheStats(CacheBucket Installers, CacheBucket Metadata, CacheBucket Other)
{
	public long TotalBytes => Installers.Bytes + Metadata.Bytes + Other.Bytes;
}

/// <summary>
/// Reports and clears what Dots has written to <see cref="Constants.AppDataPath"/>. Nothing here
/// runs on its own - installers are kept until the user asks for them to go, so a failed install
/// can be retried without re-downloading a few hundred megabytes.
/// </summary>
public class CacheService
{
	static readonly string[] InstallerExtensions = [".exe", ".pkg", ".gz", ".partial"];

	public static string FormatSize(long bytes)
	{
		string[] units = ["B", "KB", "MB", "GB", "TB"];
		double size = bytes;
		var unit = 0;
		while (size >= 1024 && unit < units.Length - 1)
		{
			size /= 1024;
			unit++;
		}
		return unit == 0 ? $"{bytes} B" : $"{size:0.#} {units[unit]}";
	}

	public CacheStats GetStats()
	{
		var installers = new List<System.IO.FileInfo>();
		var metadata = new List<System.IO.FileInfo>();
		var other = new List<System.IO.FileInfo>();

		try
		{
			if (Directory.Exists(Constants.AppDataPath))
			{
				foreach (var file in new DirectoryInfo(Constants.AppDataPath).EnumerateFiles("*", SearchOption.AllDirectories))
				{
					Classify(file).Add(file);
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex);
		}

		return new CacheStats(Bucket(installers), Bucket(metadata), Bucket(other));

		List<System.IO.FileInfo> Classify(System.IO.FileInfo file) =>
			IsInstaller(file.Name) ? installers
			: file.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase) ? metadata
			: other;

		static CacheBucket Bucket(List<System.IO.FileInfo> files) => new(files.Count, files.Sum(f => f.Length));
	}

	// ".tar.gz" only ever shows up as ".gz" through FileInfo.Extension, hence matching on the name
	static bool IsInstaller(string name) =>
		InstallerExtensions.Any(e => name.EndsWith(e, StringComparison.OrdinalIgnoreCase));

	/// <summary>Deletes downloaded SDK installers and any partial transfers. Returns bytes freed.</summary>
	public long ClearInstallers()
	{
		long freed = 0;
		try
		{
			if (!Directory.Exists(Constants.AppDataPath))
			{
				return 0;
			}

			foreach (var file in new DirectoryInfo(Constants.AppDataPath).EnumerateFiles("*", SearchOption.AllDirectories))
			{
				if (!IsInstaller(file.Name))
				{
					continue;
				}

				try
				{
					var length = file.Length;
					file.Delete();
					freed += length;
				}
				catch (Exception ex)
				{
					// an installer that is currently running is locked - skip it, don't abort
					Debug.WriteLine(ex);
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex);
		}
		return freed;
	}

	/// <summary>
	/// Deletes the cached release metadata. The Akavache keys have to go with the files: they point
	/// at paths, so a key that outlives its file sends the next read to a file that isn't there.
	/// </summary>
	public async Task<long> ClearMetadata()
	{
		long freed = 0;
		try
		{
			if (Directory.Exists(Constants.AppDataPath))
			{
				foreach (var file in new DirectoryInfo(Constants.AppDataPath).EnumerateFiles("*.json", SearchOption.TopDirectoryOnly))
				{
					try
					{
						var length = file.Length;
						file.Delete();
						freed += length;
					}
					catch (Exception ex)
					{
						Debug.WriteLine(ex);
					}
				}
			}

			await CacheDatabase.UserAccount.InvalidateAll().ToTask();
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex);
		}
		return freed;
	}
}
