namespace DotfilesLinker.Services;

internal readonly record struct LinkSummary(int Created, int Replaced, int Skipped, int Failed)
{
    public int Total => Created + Replaced + Skipped + Failed;
}

internal readonly record struct LinkResult(LinkSummary Summary, int CleanupFailed)
{
    public bool HasErrors => Summary.Failed > 0 || CleanupFailed > 0;
}

internal readonly record struct LinkOperation(string Source, string Target, bool SourceIsDirectory);

internal readonly record struct ValidatedLinkOperation(
    LinkOperation Operation,
    LinkDisposition Disposition);

internal enum LinkDisposition
{
    Create,
    Replace,
    Skip,
    Conflict
}
