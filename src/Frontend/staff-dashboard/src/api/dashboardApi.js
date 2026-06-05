import axios from 'axios';

const DASHBOARD_URL = import.meta.env.VITE_DASHBOARD_URL || 'http://localhost:5005';

const client = axios.create({
  baseURL: `${DASHBOARD_URL}/api/dashboard`,
  headers: { 'Content-Type': 'application/json' }
});

export const dashboardApi = {
  overview: () => client.get('/overview'),
  roomStatus: () => client.get('/rooms/status'),
  activeBookings: () => client.get('/bookings/active'),
  activeOrders: () => client.get('/orders/active'),
  activeMaintenance: () => client.get('/maintenance/active'),
  staffWorkloads: () => client.get('/staff/workloads'),
  occupancyMetrics: () => client.get('/metrics/occupancy'),
  revenueMetrics: () => client.get('/metrics/revenue'),
  alerts: () => client.get('/alerts')
};

export const DASHBOARD_BASE = DASHBOARD_URL;
export default client;