import { Routes, Route, Link, useLocation, useNavigate } from 'react-router-dom';
import { useState, useEffect } from 'react';
import HomePage from './pages/HomePage.jsx';
import RoomsPage from './pages/RoomsPage.jsx';
import RoomServicePage from './pages/RoomServicePage.jsx';
import MaintenancePage from './pages/MaintenancePage.jsx';
import LoginPage from './pages/LoginPage.jsx';

function NavBar() {
  const location = useLocation();
  const navigate = useNavigate();
  const isActive = (path) => (location.pathname === path ? 'nav-link active' : 'nav-link');

  // Track login state from localStorage so the nav reflects whether a guest is signed in.
  const [loggedIn, setLoggedIn] = useState(() => !!localStorage.getItem('guest_id'));

  // Re-check whenever the route changes (e.g. after logging in on the Sign In page).
  useEffect(() => {
    setLoggedIn(!!localStorage.getItem('guest_id'));
  }, [location]);

  const signOut = (e) => {
    e.preventDefault();
    localStorage.removeItem('guest_id');
    localStorage.removeItem('guest_token');
    setLoggedIn(false);
    navigate('/');
  };

  return (
    <nav className="navbar">
      <div className="brand">
        <span className="brand-icon">🏨</span> GrandStay Hotel
      </div>
      <div className="nav-links">
        <Link className={isActive('/')} to="/">Home</Link>
        <Link className={isActive('/rooms')} to="/rooms">Book a Room</Link>
        <Link className={isActive('/room-service')} to="/room-service">Room Service</Link>
        <Link className={isActive('/maintenance')} to="/maintenance">Report Issue</Link>
        {loggedIn
          ? <a className="nav-link" href="#" onClick={signOut}>Sign Out</a>
          : <Link className={isActive('/login')} to="/login">Sign In</Link>}
      </div>
    </nav>
  );
}

export default function App() {
  return (
    <div className="app">
      <NavBar />
      <main className="content">
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/rooms" element={<RoomsPage />} />
          <Route path="/room-service" element={<RoomServicePage />} />
          <Route path="/maintenance" element={<MaintenancePage />} />
          <Route path="/login" element={<LoginPage />} />
        </Routes>
      </main>
      <footer className="footer">
        <p>GrandStay Hotel · HotelOS Guest Portal · BTEC Unit 4 Programming</p>
      </footer>
    </div>
  );
}