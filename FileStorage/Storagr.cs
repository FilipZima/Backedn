using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FileStorage
{
    // New class to represent stored file with a version and timestamp
    public record FileRecord(long Version, DateTime Timestamp, List<ContactMessage> Contacts);

    public class FileStoragr : IContactStore
    {
        private FileRecord _file;
        private readonly string _path;
        private readonly object _lock = new();
        private readonly List<TaskCompletionSource<FileRecord>> _waiters = new();
        private readonly FileSystemWatcher? _watcher;

        public FileStoragr()
        {
            _path = Path.Combine(Directory.GetCurrentDirectory(), "contacts.json");

            // Load initial file
            var contacts = JsonSerializer.Deserialize<List<ContactMessage>>(File.Exists(_path) ? File.ReadAllText(_path) : "[]") ?? new List<ContactMessage>();
            _file = new FileRecord(Version: 1, Timestamp: DateTime.UtcNow, Contacts: contacts);

            // Setup watcher to pick up external changes to the file
            try
            {
                var dir = Path.GetDirectoryName(_path) ?? Directory.GetCurrentDirectory();
                var name = Path.GetFileName(_path);
                _watcher = new FileSystemWatcher(dir, name)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
                };

                _watcher.Changed += OnFileChanged;
                _watcher.Created += OnFileChanged;
                _watcher.Renamed += OnFileRenamed;
                _watcher.EnableRaisingEvents = true;
            }
            catch
            {
                // ignore watcher errors - still functional without it
            }
        }

        public async Task<string> StoreContactAsync(ContactMessage contact)
        {
            try
            {
                FileRecord snapshot;

                lock (_lock)
                {
                    var newList = new List<ContactMessage>(_file.Contacts) { contact };
                    _file = new FileRecord(_file.Version + 1, DateTime.UtcNow, newList);
                    snapshot = _file;
                }

                // write to disk asynchronously
                var json = JsonSerializer.Serialize(snapshot.Contacts);
                await File.WriteAllTextAsync(_path, json).ConfigureAwait(false);

                // notify any waiters
                List<TaskCompletionSource<FileRecord>> toNotify;
                lock (_lock)
                {
                    toNotify = new List<TaskCompletionSource<FileRecord>>(_waiters);
                    _waiters.Clear();
                }

                foreach (var tcs in toNotify)
                {
                    tcs.TrySetResult(snapshot);
                }

                return json;
            }
            catch (Exception ex)
            {
                throw new Exception("Could not store contacts!", ex);
            }
        }

        public string GetAllJson()
        {
            lock (_lock)
            {
                return JsonSerializer.Serialize(_file.Contacts);
            }
        }

        // Long-polling method: waits until version > sinceVersion or timeout
        public async Task<(string json, long version)> WaitForChangesAsync(long sinceVersion, int timeoutMs, CancellationToken cancellationToken)
        {
            // Fast-path if there's a newer version already
            TaskCompletionSource<FileRecord>? tcs = null;

            lock (_lock)
            {
                if (_file.Version > sinceVersion)
                {
                    return (JsonSerializer.Serialize(_file.Contacts), _file.Version);
                }

                tcs = new TaskCompletionSource<FileRecord>(TaskCreationOptions.RunContinuationsAsynchronously);
                _waiters.Add(tcs);
            }

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked.CancelAfter(timeoutMs);

            try
            {
                var completed = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, linked.Token)).ConfigureAwait(false);
                if (completed == tcs.Task)
                {
                    var result = await tcs.Task.ConfigureAwait(false);
                    return (JsonSerializer.Serialize(result.Contacts), result.Version);
                }

                // timeout or cancellation
                lock (_lock)
                {
                    _waiters.Remove(tcs);
                }

                lock (_lock)
                {
                    return (JsonSerializer.Serialize(_file.Contacts), _file.Version);
                }
            }
            catch (OperationCanceledException)
            {
                lock (_lock)
                {
                    _waiters.Remove(tcs);
                }

                lock (_lock)
                {
                    return (JsonSerializer.Serialize(_file.Contacts), _file.Version);
                }
            }
        }

        private void OnFileChanged(object? sender, FileSystemEventArgs e)
        {
            HandleExternalChange();
        }

        private void OnFileRenamed(object? sender, RenamedEventArgs e)
        {
            HandleExternalChange();
        }

        private void HandleExternalChange()
        {
            try
            {
                // Debounce: wait briefly for file write to complete
                Task.Delay(50).Wait();
                var text = File.Exists(_path) ? File.ReadAllText(_path) : "[]";
                var contacts = JsonSerializer.Deserialize<List<ContactMessage>>(text) ?? new List<ContactMessage>();

                List<TaskCompletionSource<FileRecord>> toNotify = new();
                lock (_lock)
                {
                    if (!AreListsEqual(_file.Contacts, contacts))
                    {
                        _file = new FileRecord(_file.Version + 1, DateTime.UtcNow, contacts);
                        toNotify.AddRange(_waiters);
                        _waiters.Clear();
                    }
                }

                foreach (var tcs in toNotify)
                {
                    tcs.TrySetResult(_file);
                }
            }
            catch
            {
                // ignore
            }
        }

        private static bool AreListsEqual(List<ContactMessage> a, List<ContactMessage> b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (!a[i].Equals(b[i])) return false;
            }
            return true;
        }
    }
}
