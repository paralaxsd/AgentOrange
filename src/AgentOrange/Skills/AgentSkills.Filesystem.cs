using System.ComponentModel;
using System.Text;

namespace AgentOrange.Skills
{
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

        [Description("Sucht rekursiv nach einem Text in allen Dateien des Projektverzeichnisses und gibt die Dateinamen sowie die Zeilennummern der Fundstellen zurück.")]
        public string SearchInProject(
            [Description("Der Text oder reguläre Ausdruck, nach dem gesucht werden soll.")] string query,
            [Description("Optionale Dateiendung (z. B. '.cs', '.json'), um die Suche einzuschränken.")] string fileExtension = "")
        {
            var root = Directory.GetCurrentDirectory();
            var results = new List<string>();
            var comparison = StringComparison.OrdinalIgnoreCase;

            bool isRegex;
            System.Text.RegularExpressions.Regex? regex = null;

            // Try to compile as regex, fallback to plain string if invalid
            try
            {
                regex = new System.Text.RegularExpressions.Regex(query, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                isRegex = true;
            }
            catch
            {
                isRegex = false;
            }

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                    .Where(f =>
                        string.IsNullOrEmpty(fileExtension) ||
                        f.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                return $"Fehler beim Durchsuchen der Dateien: {ex.Message}";
            }

            foreach (var file in files)
            {
                try
                {
                    int lineNum = 0;
                    foreach (var line in File.ReadLines(file))
                    {
                        lineNum++;
                        bool found = isRegex
                            ? regex!.IsMatch(line)
                            : line.IndexOf(query, comparison) >= 0;
                        if (found)
                        {
                            results.Add($"{file} (Zeile {lineNum}): {line.Trim()}");
                        }
                    }
                }
                catch
                {
                    // Ignore unreadable files
                }
            }

            if (results.Count == 0)
                return "Keine Fundstellen gefunden.";

            return string.Join(Environment.NewLine, results);
        }

        [Description("Gibt eine übersichtliche, hierarchische Baumansicht (Tree) aller Dateien und Ordner im Projekt aus.")]
        public string GetFileTree(
            [Description("Der Startpfad für den Baum. Standard ist das Projektverzeichnis.")] string rootPath = ".",
            [Description("Gibt an, ob versteckte Ordner wie .git oder bin/obj ignoriert werden sollen.")] bool ignoreBuildFolders = true)
        {
            var sb = new StringBuilder();
            void PrintTree(string path, string indent, bool isLast)
            {
                var dirInfo = new DirectoryInfo(path);
                if (ignoreBuildFolders && (dirInfo.Name == ".git" || dirInfo.Name == "bin" || dirInfo.Name == "obj"))
                    return;

                sb.AppendLine($"{indent}{(isLast ? "└──" : "├──")}{dirInfo.Name}");

                var files = dirInfo.GetFiles();
                for (int i = 0; i < files.Length; i++)
                {
                    sb.AppendLine($"{indent}{(isLast ? "    " : "│   ")}{(i == files.Length - 1 ? "└──" : "├──")}{files[i].Name}");
                }

                var subDirs = dirInfo.GetDirectories()
                    .Where(d => !ignoreBuildFolders || (d.Name != ".git" && d.Name != "bin" && d.Name != "obj"))
                    .ToArray();

                for (int i = 0; i < subDirs.Length; i++)
                {
                    PrintTree(subDirs[i].FullName, indent + (isLast ? "    " : "│   "), i == subDirs.Length - 1);
                }
            }

            try
            {
                PrintTree(Path.GetFullPath(rootPath), "", true);
            }
            catch (Exception ex)
            {
                return $"Fehler beim Erstellen des Baums: {ex.Message}";
            }

            return sb.ToString();
        }

        [Description("Setzt das aktuelle Arbeitsverzeichnis.")]
        public void SetCurrentDirectory(string path)
            => Directory.SetCurrentDirectory(path);

        [Description("Listet Dateien und Ordner im aktuellen Verzeichnis seitenweise auf.")]
        public IEnumerable<string> ListDirectoryPaged(int page = 1, int pageSize = 20)
        {
            var entries = Directory.EnumerateFileSystemEntries(Directory.GetCurrentDirectory()).ToList();
            return entries.Skip((page - 1) * pageSize).Take(pageSize);
        }
    }
}
