import { Navigate, Route, Routes } from 'react-router-dom';

import ProtectedRoute from '@/app/routes/ProtectedRoute';
import AppLayout from '@/app/layouts/AppLayout';
import LoadingScreen from '@/shared/components/LoadingScreen';
import { useAuth, getDefaultRouteForRole } from '@/shared/lib/auth-context';
import LoginPage from '@/modules/identity/login/LoginPage';
import ResetPasswordPage from '@/modules/identity/reset-password/ResetPasswordPage';
import CourseGridPage from '@/modules/course-management/course-grid/CourseGridPage';
import LecturerCourseGridPage from '@/modules/course-management/course-grid/LecturerCourseGridPage';
import CourseDetailPage from '@/modules/course-management/course-detail/CourseDetailPage';
import UserListPage from '@/modules/identity/user-management/UserListPage';
import CourseStatisticsPage from '@/modules/reporting/course-statistics/CourseStatisticsPage';
import CourseReportGridPage from '@/modules/reporting/course-report/CourseReportGridPage';
import CourseReportPage from '@/modules/reporting/course-report/CourseReportPage';
import SchedulePage from '@/modules/enrollment-management/schedule/SchedulePage';
import StudentSchedulePage from '@/modules/enrollment-management/schedule/StudentSchedulePage';
import StudentCoursesPage from '@/modules/enrollment-management/enrollment/StudentCoursesPage';
import AvailableCoursesPage from '@/modules/enrollment-management/enrollment/AvailableCoursesPage';
import PersonalSummaryPage from '@/modules/reporting/personal-summary/PersonalSummaryPage';

function RootRedirect() {
  const { state } = useAuth();

  if (state.status === 'loading') {
    return <LoadingScreen />;
  }

  if (state.status === 'unauthenticated') {
    return <Navigate to="/login" replace />;
  }
  return <Navigate to={getDefaultRouteForRole(state.user.role)} replace />;
}

function App() {
  return (
    <Routes>
      <Route path="/" element={<RootRedirect />} />

      <Route path="/login" element={<LoginPage />} />
      <Route path="/reset-password" element={<ResetPasswordPage />} />

      <Route element={<ProtectedRoute allowedRoles={['Admin']} />}>
        <Route element={<AppLayout />}>
          <Route path="/admin/dashboard" element={<CourseGridPage />} />
          <Route path="/admin/users" element={<UserListPage />} />
          <Route path="/admin/courses/:courseId" element={<CourseDetailPage />} />
          <Route path="/admin/reports/statistics" element={<CourseStatisticsPage />} />
          <Route path="/admin/reports" element={<CourseReportGridPage />} />
          <Route path="/admin/reports/courses/:courseId" element={<CourseReportPage />} />
        </Route>
      </Route>

      <Route element={<ProtectedRoute allowedRoles={['Lecturer']} />}>
        <Route element={<AppLayout />}>
          <Route path="/lecturer/dashboard" element={<LecturerCourseGridPage />} />
          <Route path="/lecturer/courses/:courseId" element={<CourseDetailPage />} />
          <Route path="/lecturer/schedule" element={<SchedulePage />} />
          <Route path="/lecturer/reports" element={<CourseReportGridPage />} />
          <Route path="/lecturer/reports/courses/:courseId" element={<CourseReportPage />} />
        </Route>
      </Route>

      <Route element={<ProtectedRoute allowedRoles={['Student']} />}>
        <Route element={<AppLayout />}>
          <Route path="/student/available-courses" element={<AvailableCoursesPage />} />
          <Route path="/student/courses" element={<StudentCoursesPage />} />
          <Route path="/student/schedule" element={<StudentSchedulePage />} />
          <Route path="/student/summary" element={<PersonalSummaryPage />} />
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

export default App;
