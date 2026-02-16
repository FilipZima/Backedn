using System.Text.Json;

namespace FileStorage
{
    public class FileStoragr : IContactStore
    {
        private readonly List<ContactMessage> _contacts;
        private readonly string _path;  

        public FileStoragr()
        {
            _path = Path.Combine(Environment.CurrentDirectory, "contacts.json");
            _contacts = JsonSerializer.Deserialize<List<ContactMessage>>(File.Exists(_path) ? File.ReadAllText(_path) : "[]") ?? new List<ContactMessage>();
        }

        public async Task<string> StoreContactAsync(ContactMessage contact)
        {
            try
            {
                _contacts.Add(contact);
                string json = JsonSerializer.Serialize(_contacts);

                await File.WriteAllTextAsync(_path, json);

                return json;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
