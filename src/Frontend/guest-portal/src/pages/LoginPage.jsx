import { useState } from 'react';
import { guestApi } from '../api/client.js';

export default function LoginPage() {
  const [mode, setMode] = useState('login'); // 'login' | 'register'
  const [message, setMessage] = useState(null);
  const [loginForm, setLoginForm] = useState({ email: '', password: '' });
  const [registerForm, setRegisterForm] = useState({
    firstName: '', lastName: '', email: '', password: '', phoneNumber: '', dateOfBirth: '2000-01-01'
  });

  // Pull the most useful message out of an axios error so the user sees the
  // real reason (validation error, server message, or a network problem).
  const extractError = (err, fallback) => {
    if (err?.response) {
      const data = err.response.data;
      if (typeof data === 'string') return data;
      if (data?.errorMessage) return data.errorMessage;
      if (data?.error) return data.error;
      if (data?.errors) {
        const first = Object.values(data.errors)[0];
        return Array.isArray(first) ? first[0] : String(first);
      }
      return `Server returned ${err.response.status}.`;
    }
    // No response = network/CORS problem (API not reachable on :5006)
    return `${fallback} Could not reach the API at :5006 - check the frontend-service container is running.`;
  };

  const doLogin = async (e) => {
    e.preventDefault();
    setMessage(null);
    try {
      const res = await guestApi.login(loginForm);
      if (res.data?.isSuccess) {
        localStorage.setItem('guest_token', res.data.token);
        localStorage.setItem('guest_id', res.data.guest.id);
        setMessage({ type: 'success', text: `Welcome back, ${res.data.guest.firstName}!` });
      } else {
        setMessage({ type: 'error', text: res.data?.errorMessage || 'Login failed.' });
      }
    } catch (err) {
      setMessage({ type: 'error', text: extractError(err, 'Login request failed.') });
    }
  };

  const doRegister = async (e) => {
    e.preventDefault();
    setMessage(null);
    try {
      const res = await guestApi.register(registerForm);
      if (res.data?.isSuccess) {
        localStorage.setItem('guest_id', res.data.data.id);
        setMessage({ type: 'success', text: 'Account created! You can now book rooms.' });
        setMode('login');
      } else {
        setMessage({ type: 'error', text: res.data?.errorMessage || 'Registration failed.' });
      }
    } catch (err) {
      setMessage({ type: 'error', text: extractError(err, 'Registration request failed.') });
    }
  };

  return (
    <div style={{ maxWidth: 480, margin: '0 auto' }}>
      <h2 className="page-title">{mode === 'login' ? 'Sign In' : 'Create Account'}</h2>
      {message && <div className={`alert alert-${message.type}`}>{message.text}</div>}

      {mode === 'login' ? (
        <form className="panel" onSubmit={doLogin}>
          <div className="form-group" style={{ marginBottom: '1rem' }}>
            <label>Email</label>
            <input type="email" value={loginForm.email}
              onChange={(e) => setLoginForm({ ...loginForm, email: e.target.value })} />
          </div>
          <div className="form-group" style={{ marginBottom: '1rem' }}>
            <label>Password</label>
            <input type="password" value={loginForm.password}
              onChange={(e) => setLoginForm({ ...loginForm, password: e.target.value })} />
          </div>
          <button className="btn btn-primary btn-block">Sign in</button>
          <p className="muted" style={{ marginTop: '1rem', textAlign: 'center' }}>
            No account? <a href="#" onClick={(e) => { e.preventDefault(); setMode('register'); }}>Create one</a>
          </p>
        </form>
      ) : (
        <form className="panel" onSubmit={doRegister}>
          <div className="form-row">
            <div className="form-group">
              <label>First name</label>
              <input value={registerForm.firstName}
                onChange={(e) => setRegisterForm({ ...registerForm, firstName: e.target.value })} />
            </div>
            <div className="form-group">
              <label>Last name</label>
              <input value={registerForm.lastName}
                onChange={(e) => setRegisterForm({ ...registerForm, lastName: e.target.value })} />
            </div>
          </div>
          <div className="form-group" style={{ marginBottom: '1rem' }}>
            <label>Email</label>
            <input type="email" value={registerForm.email}
              onChange={(e) => setRegisterForm({ ...registerForm, email: e.target.value })} />
          </div>
          <div className="form-group" style={{ marginBottom: '1rem' }}>
            <label>Password (min 6 characters)</label>
            <input type="password" value={registerForm.password}
              onChange={(e) => setRegisterForm({ ...registerForm, password: e.target.value })} />
          </div>
          <div className="form-row">
            <div className="form-group">
              <label>Phone</label>
              <input value={registerForm.phoneNumber}
                onChange={(e) => setRegisterForm({ ...registerForm, phoneNumber: e.target.value })} />
            </div>
            <div className="form-group">
              <label>Date of birth</label>
              <input type="date" value={registerForm.dateOfBirth}
                onChange={(e) => setRegisterForm({ ...registerForm, dateOfBirth: e.target.value })} />
            </div>
          </div>
          <button className="btn btn-primary btn-block">Create account</button>
          <p className="muted" style={{ marginTop: '1rem', textAlign: 'center' }}>
            Already registered? <a href="#" onClick={(e) => { e.preventDefault(); setMode('login'); }}>Sign in</a>
          </p>
        </form>
      )}
    </div>
  );
}