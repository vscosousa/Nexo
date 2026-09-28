using System.Text.RegularExpressions;
using MimeKit;

namespace Nexo.FakeSmtp;

public record MessageSummary(
    Guid Id,
    string FromName,
    string FromAddress,
    string[] To,
    string Subject,
    string Snippet,
    DateTimeOffset ReceivedAt,
    bool Read,
    bool HasAttachments);

public record AttachmentInfo(int Index, string FileName, string ContentType, long Size);

public record MessageDetail(
    Guid Id,
    string FromName,
    string FromAddress,
    string[] To,
    string[] Cc,
    string Subject,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? SentAt,
    bool Read,
    string? Html,
    string? Text,
    AttachmentInfo[] Attachments);

/// <summary>
/// Keeps every received message as an <c>.eml</c> file in one directory, so mail survives restarts.
/// A message is named <c>{received:yyyyMMddHHmmssfff}_{id}.eml</c>; a sibling <c>.read</c> file marks it read.
/// </summary>
public sealed partial class MailStore
{
    private const int SnippetLength = 140;

    private readonly string _directory;
    private readonly object _gate = new();
    private readonly Dictionary<Guid, Entry> _entries = [];

    public MailStore(string directory)
    {
        _directory = Directory.CreateDirectory(directory).FullName;
        foreach (var path in Directory.EnumerateFiles(_directory, "*.eml"))
            TryLoad(path);
    }

    /// <summary>Parses and stores a raw RFC 5322 message; returns its id.</summary>
    public Guid Add(byte[] raw)
    {
        var message = MimeMessage.Load(new MemoryStream(raw));
        var id = Guid.NewGuid();
        var receivedAt = DateTimeOffset.UtcNow;
        var path = Path.Combine(_directory, $"{receivedAt:yyyyMMddHHmmssfff}_{id:N}.eml");
        File.WriteAllBytes(path, raw);
        lock (_gate) _entries[id] = new Entry(id, receivedAt, path, Summarize(message, id, receivedAt), Read: false);
        return id;
    }

    /// <summary>Lists messages newest first; <paramref name="query"/> filters sender, recipients, subject and text.</summary>
    public IReadOnlyList<MessageSummary> List(string? query)
    {
        lock (_gate)
        {
            return _entries.Values
                .OrderByDescending(e => e.ReceivedAt)
                .Select(e => e.Summary with { Read = e.Read })
                .Where(s => string.IsNullOrWhiteSpace(query) || Matches(s, query.Trim()))
                .ToList();
        }
    }

    public MessageDetail? Get(Guid id)
    {
        var entry = Find(id);
        if (entry is null) return null;
        var m = Load(entry);
        return new MessageDetail(
            id,
            entry.Summary.FromName,
            entry.Summary.FromAddress,
            entry.Summary.To,
            [.. m.Cc.Mailboxes.Select(Display)],
            entry.Summary.Subject,
            entry.ReceivedAt,
            m.Date == DateTimeOffset.MinValue ? null : m.Date,
            entry.Read,
            m.HtmlBody,
            m.TextBody,
            [.. m.Attachments.OfType<MimePart>().Select((a, i) => new AttachmentInfo(i, FileNameOf(a, i), a.ContentType.MimeType, Size(a)))]);
    }

    public string? RawPath(Guid id) => Find(id)?.Path;

    public (byte[] Content, string ContentType, string FileName)? GetAttachment(Guid id, int index)
    {
        var entry = Find(id);
        if (entry is null) return null;
        var parts = Load(entry).Attachments.OfType<MimePart>().ToList();
        if (index < 0 || index >= parts.Count) return null;
        using var buffer = new MemoryStream();
        parts[index].Content?.DecodeTo(buffer);
        return (buffer.ToArray(), parts[index].ContentType.MimeType, FileNameOf(parts[index], index));
    }

    public bool MarkRead(Guid id, bool read)
    {
        lock (_gate)
        {
            if (!_entries.TryGetValue(id, out var entry)) return false;
            _entries[id] = entry with { Read = read };
            var marker = Path.ChangeExtension(entry.Path, ".read");
            if (read) File.WriteAllBytes(marker, []);
            else File.Delete(marker);
            return true;
        }
    }

    public bool Delete(Guid id)
    {
        lock (_gate)
        {
            if (!_entries.Remove(id, out var entry)) return false;
            File.Delete(entry.Path);
            File.Delete(Path.ChangeExtension(entry.Path, ".read"));
            return true;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            foreach (var id in _entries.Keys.ToList()) Delete(id);
        }
    }

    private sealed record Entry(Guid Id, DateTimeOffset ReceivedAt, string Path, MessageSummary Summary, bool Read);

    private Entry? Find(Guid id)
    {
        lock (_gate) return _entries.GetValueOrDefault(id);
    }

    private static MimeMessage Load(Entry entry)
    {
        using var stream = File.OpenRead(entry.Path);
        return MimeMessage.Load(stream);
    }

    private void TryLoad(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path).Split('_');
        if (name.Length != 2
            || !DateTimeOffset.TryParseExact(name[0], "yyyyMMddHHmmssfff", null, System.Globalization.DateTimeStyles.AssumeUniversal, out var receivedAt)
            || !Guid.TryParseExact(name[1], "N", out var id))
            return;
        try
        {
            using var stream = File.OpenRead(path);
            var summary = Summarize(MimeMessage.Load(stream), id, receivedAt);
            _entries[id] = new Entry(id, receivedAt, path, summary, File.Exists(Path.ChangeExtension(path, ".read")));
        }
        catch (FormatException)
        {
            // A damaged file is skipped rather than stopping the server.
        }
    }

    private static MessageSummary Summarize(MimeMessage m, Guid id, DateTimeOffset receivedAt)
    {
        var from = m.From.Mailboxes.FirstOrDefault();
        var text = m.TextBody ?? StripTags(m.HtmlBody ?? "");
        var snippet = Whitespace().Replace(text, " ").Trim();
        return new MessageSummary(
            id,
            from is null ? "" : from.Name ?? "",
            from?.Address ?? "",
            [.. m.To.Mailboxes.Concat(m.Cc.Mailboxes).Select(Display)],
            string.IsNullOrWhiteSpace(m.Subject) ? "(no subject)" : m.Subject,
            snippet.Length > SnippetLength ? snippet[..SnippetLength] : snippet,
            receivedAt,
            Read: false,
            HasAttachments: m.Attachments.Any());
    }

    private static bool Matches(MessageSummary s, string query) =>
        new[] { s.FromName, s.FromAddress, s.Subject, s.Snippet }.Concat(s.To)
            .Any(field => field.Contains(query, StringComparison.OrdinalIgnoreCase));

    private static string Display(MailboxAddress mailbox) => mailbox.Address;

    private static string FileNameOf(MimePart part, int index) => part.FileName ?? $"attachment-{index + 1}";

    private static long Size(MimePart part)
    {
        using var buffer = new MemoryStream();
        part.Content?.DecodeTo(buffer);
        return buffer.Length;
    }

    private static string StripTags(string html) => Tags().Replace(html, " ");

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex Tags();
}
