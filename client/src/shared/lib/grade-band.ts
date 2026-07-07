// Màu theo dải điểm (grade band) — khớp bảng màu status của design system
// (xanh lá = Excellent, xanh dương = Good, vàng = Average, đỏ = Weak).
// Dùng chung cho Course Statistics, Course Report (Grades tab)... — Rule of Three: >=2 module đã cần.
export interface GradeBand {
  label: 'Excellent' | 'Good' | 'Average' | 'Weak';
  color: string;
}

export function getGradeBand(score: number): GradeBand {
  if (score >= 8.5) return { label: 'Excellent', color: '#1E875F' };
  if (score >= 7) return { label: 'Good', color: '#2E73C4' };
  if (score >= 5) return { label: 'Average', color: '#E5A20B' };
  return { label: 'Weak', color: '#D7372C' };
}
