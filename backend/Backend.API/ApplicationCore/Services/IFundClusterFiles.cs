namespace Backend.API.ApplicationCore.Services;

/// <summary>
/// Locates the precomputed fund cluster JSON files (written by the FundClustering pipeline)
/// so they can be served as-is. Never parses or changes their contents beyond the manifest.
/// </summary>
public interface IFundClusterFiles
{
    /// <summary>
    /// Returns the full path of <c>methods.json</c>, or <c>null</c> if it does not exist.
    /// </summary>
    string? GetManifestPath();

    /// <summary>
    /// Returns the full path of <c>funds.json</c>, or <c>null</c> if it does not exist.
    /// </summary>
    string? GetFundsPath();

    /// <summary>
    /// Returns the full path of the file for a clustering method, or <c>null</c> if the id
    /// is not listed in <c>methods.json</c> or its file does not exist.
    /// </summary>
    /// <param name="methodId">Method id from the manifest, e.g. <c>cosine</c>.</param>
    string? GetMethodPath(string methodId);
}
