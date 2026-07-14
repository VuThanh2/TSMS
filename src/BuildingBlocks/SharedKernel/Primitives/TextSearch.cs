using System.Globalization;
using System.Text;

namespace SharedKernel.Primitives;

// Tiện ích so khớp text cho search IN-MEMORY (LINQ-to-Objects): bỏ qua hoa/thường
// LẪN dấu tiếng Việt — gõ "Vu" khớp "Vũ", "vu". Dùng khi dữ liệu đã nằm trong RAM
// (không dịch được sang SQL). Phía truy vấn chạy dưới SQL Server dùng COLLATE
// SQL_Latin1_General_CP1_CI_AI để đạt hiệu ứng tương đương ngay trong DB.
public static class TextSearch {
    public static bool ContainsNormalized(string? source, string? keyword) {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(keyword))
            return false;
        return RemoveDiacritics(source)
            .Contains(RemoveDiacritics(keyword), StringComparison.OrdinalIgnoreCase);
    }

    // Tách ký tự gốc khỏi dấu (á → a) qua chuẩn hóa Unicode FormD rồi bỏ NonSpacingMark.
    // đ/Đ là ký tự Latin độc lập, KHÔNG tách được bằng FormD nên thay tay.
    private static string RemoveDiacritics(string text) {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var c in normalized) {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }
        return builder.ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd')
            .Replace('Đ', 'D');
    }
}
