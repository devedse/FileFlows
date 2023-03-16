using System.ComponentModel;
using FileFlows.Server.Helpers;
using FileFlows.ServerShared.Models;
using FileFlows.Server.Controllers;
using FileFlows.Server.Database;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Cache for library files service
/// </summary>
public partial class LibraryFileService 
{
    private static Dictionary<Guid, LibraryFile> Data = new Dictionary<Guid, LibraryFile>();

    static LibraryFileService()
    {
        Refresh().Wait();
    }

    /// <summary>
    /// Refreshes the data
    /// </summary>
    public static async Task Refresh()
    {
        using var db = await GetDbWithMappings();
        var data = await db.Db.FetchAsync<LibraryFile>("select * from LibraryFile");
        var dict = data.ToDictionary(x => x.Uid, x => x);
        Data = dict;
    }
    
    /// <summary>
    /// Adds a file to the data
    /// </summary>
    /// <param name="file">the file being added</param>
    private void AddFile(LibraryFile file)
        => Data.TryAdd(file.Uid, file);

    /// <summary>
    /// Updates a file 
    /// </summary>
    /// <param name="file">the file being updated</param>
    private void UpdateFile(LibraryFile file)
    {
        lock (Data)
        {
            if (Data.ContainsKey(file.Uid))
                Data[file.Uid] = file;
            else
                Data.Add(file.Uid, file);
        };
    }

    /// <summary>
    /// Remove files from the cache
    /// </summary>
    /// <param name="uids">UIDs to remove</param>
    private void Remove(params Guid[] uids)
    {
        lock (Data)
        {
            foreach (var uid in uids)
            {
                if (Data.ContainsKey(uid))
                    Data.Remove(uid);
            }
        }
    }

    /// <summary>
    /// Remove files from where the libraries match a given library
    /// </summary>
    /// <param name="uids">UIDs of the libraries</param>
    private void RemoveLibraries(params Guid[] libraryUids)
        => Data = Data.Where(x =>
            x.Value.LibraryUid == null || libraryUids.Contains(x.Value.LibraryUid.Value) == false
        ).ToDictionary(x => x.Key, x => x.Value);

    /// <summary>
    /// Deletes a file to the data
    /// </summary>
    /// <param name="file">the file being deleted</param>
    private void DeleteFile(LibraryFile file)
    {
        lock (Data)
        {
            if (Data.ContainsKey(file.Uid))
                Data.Remove(file.Uid);
        }
    }

}