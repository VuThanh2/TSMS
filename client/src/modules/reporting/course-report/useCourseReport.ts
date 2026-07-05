import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';

import {
  getStudentGradesApi,
  getAttendanceReportApi,
  getScoreDistributionApi,
} from './course-report.api';

export type ReportTab = 'grades' | 'attendance' | 'distribution';

export function useCourseReport(courseId: string) {
  const [activeTab, setActiveTab] = useState<ReportTab>('grades');

  const grades = useQuery({
    queryKey: ['student-grades', courseId],
    queryFn: () => getStudentGradesApi(courseId),
    select: (res) => res.data,
    enabled: activeTab === 'grades',
  });

  const attendance = useQuery({
    queryKey: ['attendance-report', courseId],
    queryFn: () => getAttendanceReportApi(courseId),
    select: (res) => res.data,
    enabled: activeTab === 'attendance',
  });

  const distribution = useQuery({
    queryKey: ['score-distribution', courseId],
    queryFn: () => getScoreDistributionApi(courseId),
    select: (res) => res.data,
    enabled: activeTab === 'distribution',
  });

  return { activeTab, setActiveTab, grades, attendance, distribution };
}
