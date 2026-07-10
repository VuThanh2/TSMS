import { useState } from 'react';
import { Button, Spin } from 'antd';

import StatusTag from '@/shared/components/StatusTag';
import { useAvailableCourses } from './useAvailableCourses';
import EnrollModal from './EnrollModal';
import type { AvailableCourse } from './enrollment.types';

function capacityColor(pct: number) {
  if (pct >= 1) return '#D7372C';
  if (pct >= 0.75) return '#E5A20B';
  return '#1E875F';
}

function CourseCard({ course, onEnroll }: { course: AvailableCourse; onEnroll: () => void }) {
  const remaining = course.maxCapacity - course.enrolledCount;
  const pct = course.maxCapacity > 0 ? course.enrolledCount / course.maxCapacity : 0;
  const isFull = remaining <= 0;

  return (
    <div className="flex flex-col rounded-[20px] border border-border bg-white p-6 shadow-sm">
      <div className="mb-2.5 flex items-start justify-between gap-3">
        <div className="text-[18px] font-semibold leading-tight tracking-tight">
          {course.name}
        </div>
        <StatusTag status="Upcoming" />
      </div>
      <div className="mb-4 flex gap-6">
        <div>
          <div className="mb-0.5 text-[12px] text-text-muted">Lecturer</div>
          <div className="text-[14px] font-semibold">{course.lecturerName}</div>
        </div>
        <div>
          <div className="mb-0.5 text-[12px] text-text-muted">Dates</div>
          <div className="font-mono text-[14px] font-semibold">
            {course.startDate} – {course.endDate}
          </div>
        </div>
      </div>
      <div className="mb-[18px]">
        <div className="mb-1.5 flex justify-between text-[13px]">
          <span className="text-text-muted">Capacity</span>
          <span className="font-mono font-semibold">
            {course.enrolledCount}/{course.maxCapacity}
          </span>
        </div>
        <div className="h-2 overflow-hidden rounded-full" style={{ background: '#F1E7DD' }}>
          <div
            className="h-full rounded-full"
            style={{ width: `${Math.min(pct, 1) * 100}%`, background: capacityColor(pct) }}
          />
        </div>
      </div>
      <Button type="primary" disabled={isFull} onClick={onEnroll} className="mt-auto self-start">
        {isFull ? 'Full' : 'Enroll'}
      </Button>
    </div>
  );
}

export default function AvailableCoursesPage() {
  const available = useAvailableCourses();
  const [enrollTarget, setEnrollTarget] = useState<AvailableCourse | null>(null);

  return (
    <div className="p-5 sm:p-8 md:p-10 md:px-12">
      <div className="mb-7">
        <h1 className="m-0 mb-1.5 text-[32px] font-bold tracking-tight">Available courses</h1>
        <p className="m-0 text-[15px] text-text-secondary">
          Upcoming courses open for enrollment. Pick 2 sessions per week when you enroll.
        </p>
      </div>

      {available.isLoading ? (
        <div className="flex justify-center pt-20">
          <Spin size="large" />
        </div>
      ) : available.courses.length === 0 ? (
        <div className="rounded-xl border border-border bg-white p-14 text-center text-[15px] text-text-muted">
          No upcoming courses available right now. Check back soon.
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          {available.courses.map((course) => (
            <CourseCard
              key={course.courseId}
              course={course}
              onEnroll={() => setEnrollTarget(course)}
            />
          ))}
        </div>
      )}

      <EnrollModal
        course={enrollTarget}
        onClose={() => setEnrollTarget(null)}
        isLoading={available.enrollMutation.isPending}
        onConfirm={(courseId, weeklySlotIds) => {
          available.enrollMutation.mutate(
            { courseId, weeklySlotIds },
            { onSuccess: () => setEnrollTarget(null) },
          );
        }}
      />
    </div>
  );
}
