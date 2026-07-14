import { useEffect, useState } from 'react';

// Trả về giá trị "ổn định" sau khi `value` ngừng thay đổi trong `delay` ms.
// Dùng cho ô search: input vẫn cập nhật tức thì (UX mượt), nhưng giá trị đưa vào
// queryKey chỉ đổi sau khi user ngừng gõ → tránh bắn 1 request mỗi keystroke.
export function useDebouncedValue<T>(value: T, delay = 400): T {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delay);
    return () => clearTimeout(timer);
  }, [value, delay]);

  return debounced;
}
