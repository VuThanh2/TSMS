using System.Linq.Expressions;

namespace SharedKernel.Primitives;

// Tham số sort đến từ client (?sortBy=fullName&sortDir=desc).
// Cố ý để dạng string thay vì enum: đây là dữ liệu thô ở biên hệ thống, chưa được tin.
// Việc quyết định field nào hợp lệ thuộc về từng repository (whitelist), không thuộc về type này.
public sealed record SortInput(string? SortBy, string? SortDir) {
    public static readonly SortInput None = new(null, null);

    // Chỉ "desc" mới là giảm dần; mọi giá trị khác (kể cả rác) đều coi là tăng dần.
    public bool IsDescending => string.Equals(SortDir, "desc", StringComparison.OrdinalIgnoreCase);
}

public static class QueryableSortExtensions {
    // Gom nhánh asc/desc vào 1 chỗ để repository chỉ cần khai báo cột, không lặp if/else.
    // Giữ nguyên TKey (không ép về object) — ép về object sinh node Convert khiến
    // EF Core không dịch được sang ORDER BY và rơi về sort in-memory.
    public static IOrderedQueryable<T> OrderByDirection<T, TKey>(
        this IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        bool descending)
        => descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
}
