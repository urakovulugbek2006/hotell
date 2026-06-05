import axios from 'axios';

// Central axios instance for all guest-portal API calls.
const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5006';

const client = axios.create({
  baseURL: `${API_URL}/api`,
  headers: { 'Content-Type': 'application/json' }
});

// Attach auth token (if present) to every request.
client.interceptors.request.use((config) => {
  const token = localStorage.getItem('guest_token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export const guestApi = {
  register: (data) => client.post('/guest/register', data),
  login: (data) => client.post('/guest/login', data),
  get: (id) => client.get(`/guest/${id}`)
};

export const roomApi = {
  availability: (query) => client.post('/room/availability', query),
  get: (id) => client.get(`/room/${id}`)
};

export const bookingApi = {
  create: (data) => client.post('/booking', data),
  get: (id) => client.get(`/booking/${id}`),
  byGuest: (guestId) => client.get(`/booking/guest/${guestId}`),
  cancel: (id, reason) => client.post(`/booking/${id}/cancel`, { reason })
};

export const orderApi = {
  menu: (category) => client.get('/order/menu', { params: { category } }),
  create: (data) => client.post('/order', data),
  byRoom: (roomId) => client.get(`/order/room/${roomId}`)
};

export const maintenanceApi = {
  create: (data) => client.post('/maintenance/requests', data)
};

export default client;