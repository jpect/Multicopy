namespace Multicopy2.Models;

/// <summary>
/// One item to copy. A file always lands at the destination root with its
/// filename. A folder is either flattened to the destination root (when
/// IncludeFolderName is false) or placed as a named subfolder.
/// </summary>
public record CopySource(string Path, bool IsFolder, bool IncludeFolderName = false);
