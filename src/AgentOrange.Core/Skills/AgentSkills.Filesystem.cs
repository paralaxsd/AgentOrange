using System.ComponentModel;
using System.Text;

namespace AgentOrange.Core.Skills;

sealed partial class AgentSkills
{
    [Description("Listet alle Dateien und Ordner in einem Verzeichnis auf.")]
    public IEnumerable<string> ListDirectory(string path)
        => Directory.EnumerateFileSystemEntries(path);

    [Description("Liest den Inhalt einer Datei (optional begrenzt auf maxBytes).")]
    public string ReadFile(string path, int? maxBytes = null)
    {
        using var stream = File.OpenRead(path);
        if (maxBytes.HasValue)
        {
            var buffer = new byte[maxBytes.Value];
            var read = stream.Read(buffer, 0, maxBytes.Value);
            return Encoding.UTF8.GetString(buffer, 0, read);
        }
        return File.ReadAllText(path, Encoding.UTF8);
    }

    [Description("Schreibt Inhalt in eine Datei (überschreibt oder hängt an).")]
    public void WriteFile(string path, string content, bool append = false)
    {
        if (append)
            File.AppendAllText(path, content, Encoding.UTF8);
        else
            File.WriteAllText(path, content, Encoding.UTF8);
    }

    [Description("Löscht eine Datei.")]
    public void DeleteFile(string path)
        => File.Delete(path);

    [Description("Sucht Dateien nach Pattern (z.B. *.txt) in einem Verzeichnis.")]
    public IEnumerable<string> SearchFiles(string directory, string pattern)
        => Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories);

    [Description("Gibt Dateiinformationen wie Größe und Änderungsdatum zurück.")]
    public string GetFileInfo(string path)
    {
        var info = new FileInfo(path);
        return $"Name: {info.Name}, Größe: {info.Length} Bytes, Geändert: {info.LastWriteTime}";
    }
}
