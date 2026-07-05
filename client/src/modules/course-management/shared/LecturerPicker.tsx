import { useState } from 'react';
import { Select } from 'antd';
import { useQuery } from '@tanstack/react-query';

import { getLecturersApi } from '@/modules/course-management/create-course/create-course.api';

interface LecturerPickerProps {
  value?: string;
  onChange?: (value: string) => void;
}

// Select component dùng chung cho chọn Lecturer (Create Course + Replace Lecturer)
export default function LecturerPicker({ value, onChange }: LecturerPickerProps) {
  const [search, setSearch] = useState('');

  const { data, isLoading } = useQuery({
    queryKey: ['lecturers', search],
    queryFn: () => getLecturersApi({ search: search || undefined, page: 1, pageSize: 20 }),
    select: (res) => res.data.items,
  });

  return (
    <Select
      showSearch
      value={value}
      onChange={onChange}
      onSearch={setSearch}
      filterOption={false}
      loading={isLoading}
      placeholder="Search lecturer by name…"
      options={data?.map((l) => ({
        value: l.userId,
        label: `${l.fullName} (${l.email})`,
      }))}
      size="large"
      className="w-full"
    />
  );
}
