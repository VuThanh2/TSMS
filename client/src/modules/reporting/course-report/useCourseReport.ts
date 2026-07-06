import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';

import {
  getStudentGradesApi,
  getAttendanceReportApi,
  getScoreDistributionApi,
} from './course-report.api';

export type ReportTab = 'grades' | 'attendance' | 'distribution';

export function useCourseReport(courseId: string, initialTab: ReportTab = 'grades') {
  const [activeTab, setActiveTab] = useState<ReportTab>(initialTab);

  const grades = useQuery({
    queryKey: ['student-grades', courseId],
    queryFn: () => getStudentGradesApi(courseId),
    select: (res) => res.data,
    enabled: activeTab === 'grades',
    retry: false,
    staleTime: 5 * 60 * 1000,
  });

  const attendance = useQuery({
    queryKey: ['attendance-report', courseId],
    queryFn: () => getAttendanceReportApi(courseId),
    select: (res) => res.data,
    enabled: activeTab === 'attendance',
    retry: false,
    staleTime: 5 * 60 * 1000,
  });

  const distribution = useQuery({
    queryKey: ['score-distribution', courseId],
    queryFn: () => getScoreDistributionApi(courseId),
    select: (res) => res.data,
    enabled: activeTab === 'distribution',
    retry: false,
    staleTime: 5 * 60 * 1000,
  });

  return { activeTab, setActiveTab, grades, attendance, distribution };
}
