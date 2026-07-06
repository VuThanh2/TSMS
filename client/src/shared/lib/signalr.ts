import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr';

const TOKEN_STORAGE_KEY = 'accessToken';

// Tạo mới một HubConnection cho GradeHub (UC-33 — Real-time Grade Notification).
// Không export một instance singleton: mỗi lần gọi trả về connection MỚI, để
// useGradeHub tự quản lý vòng đời (connect lúc mount, stop lúc unmount) — tránh
// tình trạng nhiều component share chung 1 connection rồi giẫm lên nhau khi unmount.
export function createGradeHubConnection(): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(import.meta.env.VITE_SIGNALR_HUB_URL, {

      accessTokenFactory: () => localStorage.getItem(TOKEN_STORAGE_KEY) ?? '',
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.None)
    .build();
}