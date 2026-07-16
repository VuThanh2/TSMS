import { useState } from 'react';
import type { SorterResult } from 'antd/es/table/interface';

// Cầu nối giữa sorter của antd và cặp sortBy/sortDir mà BE hiểu.
// Dùng cho các lưới PHÂN TRANG SERVER-SIDE (Users, Courses): antd chỉ nắm dữ liệu của
// trang hiện tại nên không thể tự sort đúng — phải để SQL sort trên toàn bộ tập kết quả.
// Lưới không phân trang server (Reporting, My courses…) KHÔNG cần hook này: cứ khai
// `sorter: (a, b) => …` để antd sort tại chỗ là đủ và đúng.

export type SortDirection = 'asc' | 'desc';

export interface SortState {
  sortBy?: string;
  sortDir?: SortDirection;
}

// `columnKey` của cột chính là tên field gửi xuống BE — đặt key trùng token trong
// whitelist của repository để không phải nuôi thêm một bảng ánh xạ nữa.
function toSortState<T>(sorter: SorterResult<T> | SorterResult<T>[]): SortState {
  // antd trả mảng khi bật multi-sort; các lưới hiện tại đều single-sort nên lấy phần tử đầu.
  const single = Array.isArray(sorter) ? sorter[0] : sorter;

  // Bấm header lần thứ 3 → antd trả order = undefined (mode None) → bỏ sort,
  // BE rơi về thứ tự mặc định của nó.
  if (!single?.order) return {};

  const field = single.columnKey ?? single.field;
  if (field === undefined || Array.isArray(field)) return {};

  return {
    sortBy: String(field),
    sortDir: single.order === 'ascend' ? 'asc' : 'desc',
  };
}

export function useTableSort() {
  const [sort, setSort] = useState<SortState>({});

  function applySorter<T>(sorter: SorterResult<T> | SorterResult<T>[]) {
    setSort(toSortState(sorter));
  }

  return { sort, applySorter };
}
