namespace Ripstation.Services;

public class FileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);
    public void FileDelete(string path) => File.Delete(path);
    public void DirectoryCreate(string path) => Directory.CreateDirectory(path);
    public bool DirectoryExists(string path) => Directory.Exists(path);
}
