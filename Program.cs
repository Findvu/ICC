
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;

namespace Instant_Cache_Cleaner
{
	class Program
	{
		// This order is important as the client will lock some files and not others
		static readonly string[] cacheFiles = new[]{
			"HttpCache",
			"PixmapCache",
			"productInfoCache.db",
			"_buddyState.pickle",
			"productAuth.pickle",
			"localstorage.pickle"
		};

		static void Main()
		{
			try
			{
				Logo();
				Console.WriteLine("This will create a backup and then remove cache files from the client within seconds. ");
				Console.WriteLine("Please exit the client and confirm the client is not running in the taskbar.");
				Console.WriteLine("");

				Prompt("Do you want to continue?", () =>
				{
					var appDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
					var imvuDir = Path.Combine(appDir, "IMVU");
					var backupDir = Path.Combine(imvuDir, "ICC Backups");

					Backup(imvuDir, backupDir);
					Success();
					Console.WriteLine("All finished! You may now re-open the client");

					if(Directory.EnumerateFiles(backupDir).Any())
					{
						Prompt("Would you like to calculate the size of the ICC backups? ETA ~3 seconds to 5 minutes", () =>
						{
							Delete(backupDir);
						});
					}
				});
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				Console.WriteLine("");
				Console.WriteLine("FAILURE! Program was not able to clear the cache. Report the above text for help");
			}

			Console.WriteLine("Press any key to exit");
			Console.ReadKey();
		}

		static void Logo()
		{
			Console.WriteLine(@"                                          ");
			Console.WriteLine(@"                                          ");
			Console.WriteLine(@"      ___           _              _      ");
			Console.WriteLine(@"     |_ _|_ __  ___| |_ __ _ _ __ | |_    ");
			Console.WriteLine(@"      | || '_ \/ __| __/ _` | '_ \| __|   ");
			Console.WriteLine(@"      | || | | \__ \ || (_| | | | | |_    ");
			Console.WriteLine(@"     |___|_| |_|___/\__\__,_|_| |_|\__|   ");
			Console.WriteLine(@"      / ___|__ _  ___| |__   ___          ");
			Console.WriteLine(@"     | |   / _` |/ __| '_ \ / _ \         ");
			Console.WriteLine(@"     | |__| (_| | (__| | | |  __/         ");
			Console.WriteLine(@"      \____\__,_|\___|_| |_|\___|         ");
			Console.WriteLine(@"      / ___| | ___  __ _ _ __   ___ _ __  ");
			Console.WriteLine(@"     | |   | |/ _ \/ _` | '_ \ / _ \ '__| ");
			Console.WriteLine(@"     | |___| |  __/ (_| | | | |  __/ |    ");
			Console.WriteLine(@"      \____|_|\___|\__,_|_| |_|\___|_|    ");
			Console.WriteLine(@"                                          ");
			Console.WriteLine(@"                    Version 1.0.0.0       ");
			Console.WriteLine(@"                    https://find.vu       ");
			Console.WriteLine(@"                                          ");
		}

		static void Success()
		{
			Console.WriteLine(@"      ____  _   _  ____ ____ _____ ____ ____    _ ");
			Console.WriteLine(@"     / ___|| | | |/ ___/ ___| ____/ ___/ ___|  | |");
			Console.WriteLine(@"     \___ \| | | | |  | |   |  _| \___ \___ \  | |");
			Console.WriteLine(@"      ___) | |_| | |__| |___| |___ ___) |__) | |_|");
			Console.WriteLine(@"     |____/ \___/ \____\____|_____|____/____/  (_)");
			Console.WriteLine(@"                                                  ");
		}


		static void Delete(string backupDir)
		{
			Try(() =>
			{
				Console.WriteLine("Calculating size of backups, please wait...");
				var size = DirectoryHelper.GetDirectorySize(new DirectoryInfo(backupDir));
				Console.WriteLine("The \"ICC Backups\" folder size is currently " + FormatBytes(size) + ".");

				Prompt("Do you want to delete all backups?", () =>
				{
					Console.WriteLine("Calculating number of total files and time it will take...");
					var files = DirectoryHelper.GetFilesFromDir(backupDir).ToList();

					const int numTrial = 3000;
					var startTime = DateTime.Now;
					DirectoryHelper.FastDirectoryDelete(files.Take(numTrial).ToArray(), backupDir);
					var filesPerSec = numTrial / (DateTime.Now - startTime).TotalSeconds;

					var estimatedMinutes = Math.Round(files.Count() * (1d / filesPerSec) / 60d, MidpointRounding.ToPositiveInfinity);
					Console.WriteLine($"Found {files.Count()} files, this should take about ~{estimatedMinutes} minute(s) or less...");
					Console.WriteLine("");

					Prompt("Do you wish to continue?", () =>
					{
						startTime = DateTime.Now;
						DirectoryHelper.FastDirectoryDelete(files.Skip(numTrial).ToArray(), backupDir);

						//Done just to be sure and to remove empty directories
						DirectoryHelper.DeleteSubDirectories(backupDir);

						Console.WriteLine("");
						Console.WriteLine("Total elapsed time was " + (DateTime.Now - startTime).TotalSeconds + " seconds");
						Console.WriteLine("All backups have been deleted.");
					});
				});

			}, "There was an issue removing backups, Do you want to try again?");
		}

		private static string FormatBytes(long bytes)
		{
			string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
			int i;
			double dblSByte = bytes;
			for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
			{
				dblSByte = bytes / 1024.0;
			}

			return String.Format("{0:0.##} {1}", dblSByte, Suffix[i]);
		}

		static void Backup(string imvuDir, string backupDir)
		{
			Try(() =>
			{
				if (!Directory.Exists(backupDir))
				{
					Directory.CreateDirectory(backupDir);
				}

				var tsStr = DateTimeOffset.Now.ToUnixTimeSeconds() + "_";

				foreach (var cacheFile in cacheFiles)
				{
					var oldPath = $"{imvuDir}\\{cacheFile}";
					var newPath = $"{backupDir}\\{tsStr}_{cacheFile}";

					if (Directory.Exists(oldPath))
						DirectoryHelper.RenameDirectory(oldPath, newPath);
					else if (File.Exists(oldPath))
						File.Move(oldPath, newPath);
					else
						newPath = "CACHE NOT FOUND - This is normal if you recently cleaned the cache.";

					Console.WriteLine($"{oldPath}");
					Console.WriteLine($"  => {newPath}");
				}
			}, "There was an issue with doing the backup, Do you want to try again?");
		}

		static void Try(Action action, string msg)
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);

				if (ex is IOException)
				{
					Console.WriteLine("Exit out of the client if it is currently running !");
				}

				if (Prompt(msg, action) == ConsoleKey.N)
				{
					throw;
				}
			}
		}

		static ConsoleKey Prompt(string msg, Action action)
		{
			ConsoleKey response;
			do
			{
				Console.WriteLine(msg + " [y/n]");
				response = Console.ReadKey(false).Key;
				if (response != ConsoleKey.Enter)
				{
					Console.WriteLine();
				}
			} while (response != ConsoleKey.Y && response != ConsoleKey.N);

			if (response == ConsoleKey.Y)
			{
				action();
			}

			return response;
		}
	}

	//https://stackoverflow.com/questions/24918768/progress-bar-in-console-application
	class ConsoleUtility
	{
		const char _block = '■';
		const string _back = "\r";
		const string _twirl = "-\\|/";

		public static void WriteProgressBar(int percent, bool update = false)
		{
			if (update)
				Console.Write(_back);
			Console.Write("[");
			var p = (int)((percent / 10f) + .5f);
			for (var i = 0; i < 10; ++i)
			{
				if (i >= p)
					Console.Write(' ');
				else
					Console.Write(_block);
			}
			Console.Write("] {0,3:##0}%", percent);
		}

		public static void WriteProgress(int progress, bool update = false)
		{
			if (update)
				Console.Write("\b");
			Console.Write(_twirl[progress % _twirl.Length]);
		}
	}

	public class DirectoryHelper
	{
		//https://stackoverflow.com/questions/44699238/how-to-fast-delete-many-files
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool DeleteFile(string lpFileName);

		//https://stackoverflow.com/questions/468119/whats-the-best-way-to-calculate-the-size-of-a-directory-in-net
		public static long GetDirectorySize(DirectoryInfo directoryInfo, bool recursive = true)
		{
			var startDirectorySize = default(long);
			if (directoryInfo == null || !directoryInfo.Exists)
				return startDirectorySize; //Return 0 while Directory does not exist.

			//Add size of files in the Current Directory to main size.
			foreach (var fileInfo in directoryInfo.GetFiles())
				System.Threading.Interlocked.Add(ref startDirectorySize, fileInfo.Length);

			if (recursive) //Loop on Sub Direcotries in the Current Directory and Calculate it's files size.
				System.Threading.Tasks.Parallel.ForEach(directoryInfo.GetDirectories(), (subDirectory) =>
			System.Threading.Interlocked.Add(ref startDirectorySize, GetDirectorySize(subDirectory, recursive)));

			return startDirectorySize;  //Return full Size of this Directory.
		}

		//https://stackoverflow.com/questions/1817333/how-do-i-rename-a-folder-directory-in-c
		public static bool RenameDirectory(string sourceDirectoryPath, string newDirectoryNameWithNoPath, bool suppressExceptions = false)
		{
			try
			{
				DirectoryInfo dirInfo;
				try
				{
					//https://docs.microsoft.com/en-us/dotnet/api/system.io.directoryinfo.-ctor?view=net-5.0
					dirInfo = new DirectoryInfo(sourceDirectoryPath);
				}
				catch (ArgumentNullException e)
				{
					throw new Exception("Source directory path is null.", e);
				}
				catch (SecurityException e)
				{
					throw new Exception($"The caller does not have the required permission for Source Directory:{sourceDirectoryPath}.", e);
				}
				catch (ArgumentException e)
				{
					//Could reference: https://docs.microsoft.com/en-us/dotnet/api/system.io.path.getinvalidpathchars?view=net-5.0 
					throw new Exception($"Source directory path contains invalid character(s): {sourceDirectoryPath}", e);
				}
				catch (PathTooLongException e)
				{
					throw new Exception($"Source directory path is too long. Length={sourceDirectoryPath.Length}", e);
				}

				string destinationDirectoryPath = dirInfo.Parent == null ? newDirectoryNameWithNoPath : Path.Combine(dirInfo.Parent.FullName, newDirectoryNameWithNoPath);

				try
				{
					//https://docs.microsoft.com/en-us/dotnet/api/system.io.directoryinfo.moveto?view=net-5.0 
					dirInfo.MoveTo(destinationDirectoryPath);
				}
				catch (ArgumentNullException e)
				{
					throw new Exception("Destination directory is null.", e);
				}
				catch (ArgumentException e)
				{
					throw new Exception("Destination directory must not be empty.", e);
				}
				catch (SecurityException e)
				{
					throw new Exception($"The caller does not have the required permission for Directory rename:{destinationDirectoryPath}.", e);
				}
				catch (PathTooLongException e)
				{
					throw new Exception($"Rename path is too long. Length={destinationDirectoryPath.Length}", e);
				}
				catch (IOException e)
				{
					if (Directory.Exists(destinationDirectoryPath))
					{
						throw new Exception($"Cannot rename source directory, destination directory already exists: {destinationDirectoryPath}", e);
					}

					if (string.Equals(sourceDirectoryPath, destinationDirectoryPath, StringComparison.InvariantCultureIgnoreCase))
					{
						throw new Exception($"Source directory cannot be the same as Destination directory.", e);
					}

					throw new Exception($"IOException: {e.Message}", e);
				}
			}
			catch (Exception)
			{
				if (!suppressExceptions)
				{
					throw;
				}

				return false;
			}

			return true;
		}

		public static System.Collections.Generic.IEnumerable<string> GetFilesFromDir(string dir) =>
			 Directory.EnumerateFiles(dir).Concat(
			 Directory.EnumerateDirectories(dir)
					  .SelectMany(subdir => GetFilesFromDir(subdir)));

		public static void FastDirectoryDelete(string[] files, string path)
		{
			ConsoleUtility.WriteProgress(0, true);
			for (var i = 0; i < files.Count(); i++)
			{
				var progress = 100 * ((double)i / files.Count());
				DeleteFile(files[i]);
				ConsoleUtility.WriteProgressBar((int)progress, true);
			}
			ConsoleUtility.WriteProgressBar(100, true);
			Console.WriteLine("");
		}

		public static void DeleteSubDirectories(string parentPath)
		{
			var dirs = Directory.GetDirectories(parentPath);
			foreach (var dir in dirs)
			{
				DeleteDirectory(dir);
			}
		}

		//http://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true/329502#329502
		public static void DeleteDirectory(string directoryPath)
		{
			if (!Directory.Exists(directoryPath))
			{
				Console.WriteLine(
					string.Format("Directory '{0}' is missing and can't be removed.",
						directoryPath));

				return;
			}

			var files = Directory.GetFiles(directoryPath);
			var dirs = Directory.GetDirectories(directoryPath);

			foreach (var file in files)
			{
				File.SetAttributes(file, FileAttributes.Normal);
				File.Delete(file);
			}

			foreach (var dir in dirs)
			{
				DeleteDirectory(dir);
			}

			File.SetAttributes(directoryPath, FileAttributes.Normal);

			try
			{
				Directory.Delete(directoryPath, false);
			}
			catch (IOException)
			{
				Console.WriteLine(string.Format("{0}The directory '{1}' could not be deleted!" +
											  "{0}Most of the time, this is due to an external process accessing the files in the temporary repositories created during the test runs, and keeping a handle on the directory, thus preventing the deletion of those files." +
											  "{0}Known and common causes include:" +
											  "{0}- Windows Search Indexer (go to the Indexing Options, in the Windows Control Panel, and exclude the bin folder of LibGit2Sharp.Tests)" +
											  "{0}- Antivirus (exclude the bin folder of LibGit2Sharp.Tests from the paths scanned by your real-time antivirus){0}",
					Environment.NewLine, Path.GetFullPath(directoryPath)));
			}
		}
	}
}
