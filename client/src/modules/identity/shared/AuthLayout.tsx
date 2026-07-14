import type { ReactNode } from 'react';

// Layout chia đôi màn hình: bên trái form, bên phải hero gradient
// Dùng chung cho Login và Reset Password
export default function AuthLayout({ children }: { children: ReactNode }) {
  return (
    <div className="grid min-h-screen grid-cols-1 md:grid-cols-[1.05fr_0.95fr]">
      <div className="flex items-center justify-center p-6 sm:p-12">
        <div className="w-full max-w-[400px]">
          {children}
        </div>
      </div>

      {/* Cột phải — hero gradient: logo phóng to ở trên cùng để user chú ý ngay,
          câu giới thiệu neo ở dưới cùng (justify-between). Ẩn trên màn nhỏ để form
          chiếm trọn chiều ngang (tránh cột hẹp bị bóp). */}
      <div
        className="relative hidden flex-col justify-between overflow-hidden p-14 md:flex"
        style={{ background: 'linear-gradient(155deg, #F45D48 0%, #E04A36 100%)' }}
      >
        {/* Decorative circles */}
        <div
          className="absolute rounded-full"
          style={{
            top: -80, right: -80,
            width: 360, height: 360,
            background: 'rgba(255,255,255,0.08)',
          }}
        />
        <div
          className="absolute rounded-full"
          style={{
            top: 120, right: 180,
            width: 180, height: 180,
            background: 'rgba(255,255,255,0.06)',
          }}
        />

        <div className="relative flex items-center gap-4">
          <span className="text-[70px] font-bold tracking-tight text-white">TSMS</span>
        </div>

        <div className="relative max-w-[400px] text-white">
          <div className="mb-4 text-[28px] font-bold leading-tight tracking-tight">
            Schedules, Enrollment and Grades — calm and legible.
          </div>
          <div className="text-[15px] leading-relaxed" style={{ color: 'rgba(255,255,255,0.85)' }}>
            Class schedules and grades, updated the moment they happen — no refresh, no guesswork, the way academic tools should've worked from day one
          </div>
        </div>
      </div>
    </div>
  );
}
