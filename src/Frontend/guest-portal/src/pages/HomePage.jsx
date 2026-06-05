import { Link } from 'react-router-dom';

export default function HomePage() {
  return (
    <div>
      <section className="hero">
        <h1>Welcome to GrandStay Hotel</h1>
        <p>A four-star experience across six floors. Book your stay, order room service,
           and let us take care of everything in real time.</p>
        <Link to="/rooms" className="btn btn-primary">Book a Room</Link>
      </section>

      <section className="cards">
        <div className="card">
          <div className="card-icon">🛏️</div>
          <h3>Comfortable Rooms</h3>
          <p>Single, double, suite and accessible rooms, all kept spotless by our
             housekeeping team.</p>
          <Link to="/rooms" className="card-link">Check availability →</Link>
        </div>
        <div className="card">
          <div className="card-icon">🍽️</div>
          <h3>Room Service</h3>
          <p>Order food and drinks straight to your room and track your order live.</p>
          <Link to="/room-service" className="card-link">View menu →</Link>
        </div>
        <div className="card">
          <div className="card-icon">🔧</div>
          <h3>Report an Issue</h3>
          <p>Something not working? Report it and our maintenance crew responds by
             priority.</p>
          <Link to="/maintenance" className="card-link">Report now →</Link>
        </div>
      </section>
    </div>
  );
}