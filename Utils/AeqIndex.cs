using System.Collections;
using System.Net;
using System.Text;
using System.Text.Json;
using AutoEqApi.Model;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;
using Timer = System.Timers.Timer;

namespace AutoEqApi.Utils;

public static class AeqIndexCache
{
    private static Dictionary<long, AeqSearchResult> index = new();
    private static readonly Timer _timer;

    static AeqIndexCache()
    {
        _ = Reload();
        
        _timer = new Timer
        {
            Interval = 60 * 60000, // every 60 minutes
            AutoReset = true
        };
        _timer.Elapsed += async (_, _) => await CheckForUpdates();
        _timer.Start();
    }

    public static async Task CheckForUpdates()
    {
        using var client = new HttpClient();
        
        AeqPackageVersion[]? localVersions = null;
        try
        {
            localVersions = await JsonFileReader.ReadAsync<AeqPackageVersion[]>(@"database/version.json");
        }
        catch(DirectoryNotFoundException) {}
        catch(FileNotFoundException) {}

        var localVersion = localVersions?.FirstOrDefault(x => x.Type?.Contains("GraphicEQ") ?? false);

        var remoteVersionsResponse = await client.GetAsync("https://github.com/ThePBone/AutoEqPackages/raw/main/version.json");
        if (!remoteVersionsResponse.IsSuccessStatusCode)
        {
            Console.WriteLine("Failed to query remote version.json");
            return;
        }

        var remoteVersions = await JsonSerializer.DeserializeAsync<AeqPackageVersion[]>(await remoteVersionsResponse.Content.ReadAsStreamAsync());
        if (remoteVersions == null)
        {
            Console.WriteLine("Failed to parse remote version.json");
            return;
        }

        var remoteVersion = remoteVersions.FirstOrDefault(x => x.Type?.Contains("GraphicEQ") ?? false);
        if (remoteVersion == null)
        {
            Console.WriteLine("No suitable package found in remote version.json");
            return;
        }

        if (remoteVersion.Commit == localVersion?.Commit)
        {
            // No new updates available
            return;
        }
        
        Console.WriteLine($"Downloading new version ({remoteVersion.Commit}) from {remoteVersion.PackageUrl}");

        if(Directory.Exists("update"))
            Directory.Delete("update", true);
        Directory.CreateDirectory("update");

#pragma warning disable SYSLIB0014
        using var webClient = new WebClient();
#pragma warning restore SYSLIB0014
        try
        {
            await webClient.DownloadFileTaskAsync(remoteVersion.PackageUrl ?? string.Empty, "update/archive.tar.gz");
        }
        catch (WebException ex)
        {
            Console.WriteLine("Download failed: " + ex);
            return;
        }

        await using Stream streamGz = File.OpenRead("update/archive.tar.gz");
        using var archiveGz = ArchiveFactory.Open(streamGz);
        var options = new ExtractionOptions()
        {
            ExtractFullPath = true,
            Overwrite = true
        };
        
        Console.WriteLine("Decompressing update");
        archiveGz.WriteToDirectory("update", options);
        
        if(File.Exists("update/archive.tar.gz"))
            File.Delete("update/archive.tar.gz");
                
        await using Stream streamTar = File.OpenRead("update/archive.tar");
        using var archiveTar = ArchiveFactory.Open(streamTar);
        
        Console.WriteLine("Extracting update tarball");
        archiveTar.WriteToDirectory("update", options);
        
        if(File.Exists("update/archive.tar"))
            File.Delete("update/archive.tar");
        
        // Remove CSV metadata
        var directoriesQueue = new Queue<string>();
        directoriesQueue.Enqueue("update");
        while (directoriesQueue.Count > 0)
        {
            var currentPath = directoriesQueue.Dequeue();
            var directories = Directory.GetDirectories(currentPath);
 
            foreach (var directory in directories)
            {
                directoriesQueue.Enqueue(directory);
            }

            foreach(var csv in Directory.GetFiles(currentPath, "*.csv"))
            {
                File.Delete(csv);
            }
        }
        
        if(Directory.Exists("database"))
            Directory.Delete("database", true);
        Directory.CreateDirectory("database");

        Console.WriteLine("Applying update");
        CopyUtils.Copy("update", "database");
        await Reload();
        
        if(Directory.Exists("update"))
            Directory.Delete("update", true);
        
        Console.WriteLine($"Update {remoteVersion.Commit} applied");
    }

    public static async Task Reload()
    {
        var newIndex = new Dictionary<long, AeqSearchResult>();
        var items = await JsonFileReader.ReadAsync<AeqSearchResult[]>(@"database/index.json");
        if (items == null)
        {
            Console.WriteLine("AeqIndexCache.Reload: failed to parse index.json");
            return;
        }
        
        newIndex.EnsureCapacity(items.Length);

        foreach (var item in items)
        {
            newIndex.Add(item.Id, item);
        }
        
        index.Clear();
        index = newIndex;
    }

    public static AeqSearchResult? LookupId(long id) => index.ContainsKey(id) ? index[id] : null;

    public static AeqSearchResult[] Search(string query)
    {
        if (query.Trim().Length < 1)
            return Array.Empty<AeqSearchResult>();

        return index.Values
            .Where(x => x.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
            .Take(50)
            .ToArray();
    }
}