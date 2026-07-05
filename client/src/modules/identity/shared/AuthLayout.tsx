import type { ReactNode } from 'react';

// Layout chia đôi màn hình: bên trái form, bên phải hero gradient
// Dùng chung cho Login và Reset Password
export default function AuthLayout({ children }: { children: ReactNode }) {
  return (
    <div className="grid min-h-screen" style={{ gridTemplateColumns: '1.05fr 0.95fr' }}>
      {/* Cột trái — form */}
      <div className="flex items-center justify-center p-12">
        <div className="w-full max-w-[400px]">
          {/* Logo */}
          <div className="mb-10 flex items-center gap-2.5">
            <div className="flex h-9 w-9 items-center justify-center rounded-[10px] bg-primary text-[17px] font-bold text-white">
              T
            </div>
            <span className="text-[19px] font-bold tracking-tight">TSMS</span>
          </div>
          {children}
        </div>
      </div>

      {/* Cột phải — hero gradient */}
      <div
        className="relative flex items-end overflow-hidden p-14"
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

        <div className="relative max-w-[400px] text-white">
          <div className="mb-4 text-[28px] font-bold leading-tight tracking-tight">
            Schedules, enrollment and grades — calm and legible.
          </div>
          <div className="text-[15px] leading-relaxed" style={{ color: 'rgba(255,255,255,0.85)' }}>
            A warm, human-centered academic scheduling system that feels like a helpful advisor,
            not a bureaucratic portal.
          </div>
        </div>
      </div>
    </div>
  );
}
