import { useState } from 'react';
import LoginGate from './components/LoginGate.jsx';
import Dashboard from './components/Dashboard.jsx';

// The dashboard requires a token before any sensitive data is displayed
// (Task 3.2 - Authentication). The expected token is configured via
// VITE_DASHBOARD_TOKEN and defaults to a known demo value.
const EXPECTED_TOKEN = import.meta.env.VITE_DASHBOARD_TOKEN || 'hotelos-admin-2024';

export default function App() {
  const [authed, setAuthed] = useState(
    () => sessionStorage.getItem('dashboard_authed') === 'true'
  );

  const handleLogin = (token) => {
    if (token === EXPECTED_TOKEN) {
      sessionStorage.setItem('dashboard_authed', 'true');
      setAuthed(true);
      return true;
    }
    return false;
  };

  const handleLogout = () => {
    sessionStorage.removeItem('dashboard_authed');
    setAuthed(false);
  };

  if (!authed) {
    return <LoginGate onLogin={handleLogin} />;
  }

  return <Dashboard onLogout={handleLogout} />;
}