namespace QueryFlow.Core.Cursor;

internal sealed record CursorKeyValue(string Field, string? Json, string? TypeName);

internal sealed record CursorPayload(IReadOnlyList<CursorKeyValue> Keys, bool Backward);
