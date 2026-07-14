import { useEffect, useState } from 'react';
import { Input, Modal, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useQuery, keepPreviousData } from '@tanstack/react-query';

import { useDebouncedValue } from '@/shared/hooks/useDebouncedValue';
import { getLecturersApi } from '@/modules/course-management/create-course/create-course.api';
import type { LecturerOption } from '@/modules/course-management/shared/course.types';

interface LecturerPickerProps {
  // value/onChange do antd Form inject — value là lecturerId, giữ nguyên interface cũ
  // nên các Form.Item (Create Course, Replace Lecturer) không phải sửa gì.
  value?: string;
  onChange?: (value: string) => void;
  // Ẩn 1 lecturer khỏi danh sách chọn (vd Replace Lecturer: bỏ lecturer đang gán để
  // tránh chọn trùng — backend cũng chặn bằng Course.SameLecturer, đây là chặn từ UI).
  excludeId?: string;
}

const PAGE_SIZE = 8;

// Chọn Lecturer qua Modal (danh sách + Department + phân trang + search) thay cho
// Select-search cũ. Lý do: Select dropdown bất tiện khi nhiều lecturer, và onSearch
// của Select bắn query mỗi keystroke. Ở đây search được debounce, phân trang server-side.
export default function LecturerPicker({ value, onChange, excludeId }: LecturerPickerProps) {
  const [open, setOpen] = useState(false);
  // Lưu object lecturer đã chọn để hiển thị tên ở ô trigger (value chỉ là id).
  const [selected, setSelected] = useState<LecturerOption | null>(null);
  const [searchInput, setSearchInput] = useState('');
  const [page, setPage] = useState(1);

  const debouncedSearch = useDebouncedValue(searchInput.trim(), 400);

  // Đổi từ khóa search → về trang 1 (tránh kẹt ở trang vượt quá tổng kết quả mới).
  useEffect(() => {
    setPage(1);
  }, [debouncedSearch]);

  // Form reset (value bị xóa từ ngoài) → xóa luôn nhãn đang hiển thị.
  useEffect(() => {
    if (!value) setSelected(null);
  }, [value]);

  const { data, isFetching } = useQuery({
    queryKey: ['lecturers', debouncedSearch, page],
    queryFn: () => getLecturersApi({ search: debouncedSearch || undefined, page, pageSize: PAGE_SIZE }),
    select: (res) => res.data,
    enabled: open, // chỉ fetch khi Modal mở
    placeholderData: keepPreviousData, // giữ data cũ khi lật trang → không nhấp nháy
  });

  // Lọc client-side lecturer bị loại (excludeId). Phân trang server-side nên trang chứa
  // lecturer này chỉ hiện 7 dòng thay vì 8 — chấp nhận được vì chỉ loại đúng 1 người.
  const items = (data?.items ?? []).filter((l) => l.userId !== excludeId);
  const total = data?.totalCount ?? 0;

  function pick(lecturer: LecturerOption) {
    setSelected(lecturer);
    onChange?.(lecturer.userId);
    setOpen(false);
  }

  function openModal() {
    setSearchInput('');
    setPage(1);
    setOpen(true);
  }

  const columns: ColumnsType<LecturerOption> = [
    {
      title: 'Lecturer',
      dataIndex: 'fullName',
      key: 'fullName',
      render: (v: string) => <span className="font-semibold">{v}</span>,
    },
    {
      title: 'Email',
      dataIndex: 'email',
      key: 'email',
      render: (v: string) => <span className="text-text-secondary">{v}</span>,
    },
    {
      title: 'Department',
      dataIndex: 'department',
      key: 'department',
      render: (v?: string | null) =>
        v ? <Tag>{v}</Tag> : <span className="text-text-muted">—</span>,
    },
  ];

  return (
    <>
      {/* Ô trigger: nhìn như Input nhưng readOnly, click để mở Modal chọn */}
      <Input
        readOnly
        value={selected ? `${selected.fullName} (${selected.email})` : ''}
        placeholder="Click to select a lecturer…"
        onClick={openModal}
        size="large"
        className="w-full cursor-pointer"
      />

      <Modal
        title="Select lecturer"
        open={open}
        onCancel={() => setOpen(false)}
        footer={null}
        width={640}
        destroyOnHidden
      >
        <Input.Search
          placeholder="Search by name or email…"
          allowClear
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          size="large"
          className="mb-4 mt-2"
        />

        <Table<LecturerOption>
          columns={columns}
          dataSource={items}
          rowKey="userId"
          loading={isFetching}
          size="middle"
          pagination={{
            current: page,
            pageSize: PAGE_SIZE,
            total,
            onChange: setPage,
            showSizeChanger: false,
          }}
          rowClassName={(record) =>
            record.userId === value ? 'bg-bg-card cursor-pointer' : 'cursor-pointer'
          }
          onRow={(record) => ({ onClick: () => pick(record) })}
        />
      </Modal>
    </>
  );
}
