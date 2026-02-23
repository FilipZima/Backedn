using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileStorage
{
    public interface IContactStore
    {
        public Task<string> StoreContactAsync(ContactMessage contact);
        public string GetAllJson();

        // Wait until the storage changes (version > sinceVersion) or timeout. Returns the current JSON and version.
        public Task<(string json, long version)> WaitForChangesAsync(long sinceVersion, int timeoutMs, CancellationToken cancellationToken);
    }
}
