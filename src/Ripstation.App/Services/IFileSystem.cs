namespace Ripstation.Services;

/// <summary>
/// Abstraction over System.IO file operations so that services can be
/// unit-tested without touching the real filesystem.
/// </summary>
public interface IFileSystem
{
    bool FileExists(string path);
    void FileDelete(string path);
    void DirectoryCreate(string path);
    bool DirectoryExists(string path);
}
