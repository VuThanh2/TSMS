import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Input, Spin, Empty } from 'antd';
import { SearchOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';

import { useAuth } from '@/shared/lib/auth-context';
import StatusTag from '@/shared/components/StatusTag';
import { getCoursesApi, getMyCourseApi } from '@/modules/course-management/course-grid/course-grid.api';

// Ghi chú: API Contract Mapping (mục "Course Report Grid Screen") quy định tái dùng
// GET /api/courses (Admin) hoặc GET /api/courses/my-courses (Lecturer) — không có endpoint
// riêng trả kèm điểm trung bình cho màn hình này. Vì vậy card chỉ hiện "Students"
// (enrolledCount/maxCapacity), KHÔNG hiện "Avg score" như mock tĩnh vì dữ liệu đó
// không có sẵn ở API được map cho màn hình này (tránh suy đoán field).
export default function CourseReportGridPage() {
  const navigate = useNavigate();
  const { state: authState } = useAuth();
  const isAdmin = authState.status === 'authenticated' && authState.user.role === 'Admin';

  const [keyword, setKeyword] = useState('');

  const params = { keyword: keyword || undefined, page: 1, pageSize: 60 };

  const { data, isLoading } = useQuery({
    queryKey: ['report-courses', isAdmin ? 'admin' : 'lecturer', params],
    queryFn: () => (isAdmin ? getCoursesApi(params) : getMyCourseApi(params)),
    select: (res) => res.data,
  });

  const courses = data?.items ?? [];

  function handleOpen(courseId: string) {
    const basePath = isAdmin ? '/admin/reports/courses' : '/lecturer/reports/courses';
    navigate(`${basePath}/${courseId}`);
  }

  return (
    <div className="p-10 px-12">
      <h1 className="m-0 mb-1.5 text-[32px] font-bold tracking-tight">Reports</h1>
      <p className="m-0 mb-7 text-[15px] text-text-secondary">
        Select a course to view its grade, attendance and score reports.
      </p>

      <div className="mb-5">
        <Input
          placeholder="Search by course name…"
          prefix={<SearchOutlined />}
          value={keyword}
          onChange={(e) => setKeyword(e.target.value)}
          allowClear
          size="large"
          className="max-w-[360px]"
        />
      </div>

      {isLoading ? (
        <div className="flex justify-center pt-16">
          <Spin size="large" />
        </div>
      ) : courses.length === 0 ? (
        <Empty description="No courses to report on yet." className="py-16" />
      ) : (
        <div className="grid grid-cols-3 gap-4">
          {courses.map((c) => (
            <button
              key={c.courseId}
              onClick={() => handleOpen(c.courseId)}
              className="cursor-pointer rounded-[20px] border border-border bg-white p-[22px] text-left shadow-sm transition-all duration-[220ms] ease-[cubic-bezier(0.34,1.56,0.64,1)] hover:-translate-y-0.5 hover:shadow-lg"
            >
              <div className="mb-4 flex items-start justify-between">
                <StatusTag status={c.status} />
                <span className="text-[18px] text-text-muted">→</span>
              </div>
              <div className="mb-3.5 text-[17px] font-semibold leading-[1.3] tracking-tight">{c.name}</div>
              <div className="flex gap-5">
                <div>
                  <div className="mb-0.5 text-[12px] text-text-muted">Students</div>
                  <div className="font-mono text-[16px] font-semibold">
                    {c.enrolledCount}/{c.maxCapacity}
                  </div>
                </div>
              </div>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
