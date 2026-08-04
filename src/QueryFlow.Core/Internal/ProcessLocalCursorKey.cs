using System.Security.Cryptography;

namespace QueryFlow.Core.Internal;

/// <summary>
/// Fallback signing key used when <see cref="Abstractions.Options.QueryFlowOptions.CursorSigningKey"/>
/// is not configured. Generated once per process — cursors signed with it are valid only for the
/// lifetime of that process. Set <c>CursorSigningKey</c> explicitly for multi-instance deployments.
/// </summary>
internal static class ProcessLocalCursorKey
{
    public static readonly byte[] Value = RandomNumberGenerator.GetBytes(32);
}
