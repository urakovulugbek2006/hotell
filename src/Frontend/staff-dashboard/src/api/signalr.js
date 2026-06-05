import * as signalR from '@microsoft/signalr';
import { DASHBOARD_BASE } from './dashboardApi.js';

// Creates and starts a SignalR connection to the Dashboard hub. The supplied
// handlers map server-pushed event names to callbacks so the UI can update
// in real time without polling.
export function createDashboardConnection(handlers = {}) {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${DASHBOARD_BASE}/dashboardHub`)
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

  // Wire up all server -> client events
  connection.on('InitialData', (data) => handlers.onInitialData?.(data));
  connection.on('RoomStatusUpdate', (update) => handlers.onRoomStatus?.(update));
  connection.on('RoomUpdate', (update) => handlers.onRoomStatus?.(update));
  connection.on('OrderUpdate', (update) => handlers.onOrder?.(update));
  connection.on('MaintenanceUpdate', (update) => handlers.onMaintenance?.(update));
  connection.on('CheckInOutUpdate', (update) => handlers.onCheckInOut?.(update));
  connection.on('SystemAlert', (update) => handlers.onAlert?.(update));
  connection.on('Error', (msg) => handlers.onError?.(msg));

  connection.onreconnecting(() => handlers.onConnectionChange?.('reconnecting'));
  connection.onreconnected(() => handlers.onConnectionChange?.('connected'));
  connection.onclose(() => handlers.onConnectionChange?.('disconnected'));

  connection
    .start()
    .then(() => handlers.onConnectionChange?.('connected'))
    .catch(() => handlers.onConnectionChange?.('disconnected'));

  return connection;
}