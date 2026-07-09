import api from '@/shared/lib/axios';
import type { PersonalSummaryResponse } from '@/modules/reporting/shared/reporting.types';

export function getPersonalSummaryApi() {
  return api.get<PersonalSummaryResponse>('/reports/my-summary');
}
