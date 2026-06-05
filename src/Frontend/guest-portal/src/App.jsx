import { Routes, Route, Link, useLocation } from 'react-router-dom';
import HomePage from './pages/HomePage.jsx';
import RoomsPage from './pages/RoomsPage.jsx';
import RoomServicePage from './pages/RoomServicePage.jsx';
import MaintenancePage from './pages/MaintenancePage.jsx';
import LoginPage from './pages/LoginPage.jsx';

function NavBar() {
  const location = useLocation();
  const isActive = (path) => (location.pathname === path ? 'nav-link active' : 'nav-link');

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
        <Link className={isActive('/login')} to="/login">Sign In</Link>
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