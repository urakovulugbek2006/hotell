# HotelOS — Real-Time Hotel Management System
**BTEC Unit 4: Programming — Higher National**

A microservices-based hotel management platform for the fictional *GrandStay Hotel*. Six independent
.NET 8 services communicate **only** through a Redis message broker, and a real-time operations
dashboard receives live updates over **WebSocket (SignalR)**. Two React single-page apps provide a
guest portal and a staff/operations dashboard.

---

## 1. System Overview

HotelOS connects every department through a single platform:

| Department / Area | Responsibility |
|-------------------|----------------|
| **Reception**     | Guest check-in/out, multi-criteria room assignment, billing |
| **Housekeeping**  | Room cleaning workflow (Dirty → Being cleaned → Clean), status publishing |
| **Room Service**  | Food & beverage orders (Received → Preparing → Out for delivery → Delivered) |
| **Maintenance**   | Issue reporting, **priority queue**, technician dispatch |
| **Guest Portal**  | Customer-facing booking, room service ordering, issue reporting |
| **Dashboard**     | Live operations monitoring via WebSocket |

---

## 2. Architecture

### Microservices (C# .NET 8 Web API)

| Service | Port | Purpose |
|---------|------|---------|
| ReceptionService    | 5001 | Room assignment algorithm + billing |
| HousekeepingService | 5002 | Cleaning workflow + room status |
| RoomService         | 5003 | Food & beverage orders |
| MaintenanceService  | 5004 | Priority queue + technician dispatch |
| DashboardService    | 5005 | Real-time dashboard (SignalR hub `/dashboardHub`) |
| FrontendService     | 5006 | Guest-portal API gateway |

### Frontends (React + Vite)

| App | Port | Audience |
|-----|------|----------|
| guest-portal    | 3000 | Customers — booking, room service, issue reporting |
| staff-dashboard | 3001 | Staff/management — live operations dashboard |

### Supporting infrastructure
- **Message broker:** Redis Pub/Sub (`redis:6379`)
- **Database:** Entity Framework Core + SQLite (shared volume in Docker)
- **Real-time:** SignalR WebSockets
- **Containerisation:** Docker + Docker Compose

> Services never call each other directly for domain events. Reception publishes a
> `RoomVacated` event; Housekeeping and the Dashboard subscribe and react — neither publisher nor
> subscriber knows about the other.

---

## 3. Quick Start (Docker — one command)

**Prerequisites:** Docker Desktop (includes Docker Compose).

```bash
git clone https://github.com/urakovulugbek2006/hotell.git
cd hotell
docker compose up --build
```

Then open:

| URL | What |
|-----|------|
| http://localhost:3001 | **Staff Operations Dashboard** (token: `hotelos-admin-2024`) |
| http://localhost:3000 | **Guest Portal** |
| http://localhost:5001 | Reception API + Swagger |
| http://localhost:5002 | Housekeeping API + Swagger |
| http://localhost:5003 | Room Service API + Swagger |
| http://localhost:5004 | Maintenance API + Swagger |
| http://localhost:5005 | Dashboard API + Swagger |
| http://localhost:5006 | Guest Portal API + Swagger |

Stop everything with `docker compose down` (add `-v` to also clear the database volume).

---

## 4. Running Without Docker (local dev)

**Prerequisites:** .NET 8 SDK, Node.js 18+, and a local Redis (`redis-server` or `docker run -p 6379:6379 redis:7-alpine`).

### Backend — run each service in its own terminal
```bash
dotnet restore HotelOS.sln

dotnet run --project src/Services/ReceptionService      # :5001
dotnet run --project src/Services/HousekeepingService   # :5002
dotnet run --project src/Services/RoomService           # :5003
dotnet run --project src/Services/MaintenanceService    # :5004
dotnet run --project src/Services/DashboardService      # :5005
dotnet run --project src/Services/FrontendService       # :5006
```

### Frontends
```bash
# Guest portal
cd src/Frontend/guest-portal
cp .env.example .env        # VITE_API_URL=http://localhost:5006
npm install && npm run dev  # http://localhost:3000

# Staff dashboard
cd src/Frontend/staff-dashboard
cp .env.example .env        # VITE_DASHBOARD_URL=http://localhost:5005
npm install && npm run dev  # http://localhost:3001
```

---

## 5. Message Broker — Event Catalogue

All inter-service communication flows through these Redis topics.

| Event | Topic | Publisher | Subscriber(s) | Payload (key fields) |
|-------|-------|-----------|---------------|----------------------|
| GuestCheckedIn   | `reception.guest.checkedin`        | Reception   | Dashboard | bookingId, guestId, roomId, roomNumber, checkInTime |
| GuestCheckedOut  | `reception.guest.checkedout`       | Reception   | Dashboard | bookingId, roomNumber, checkOutTime, totalBill |
| RoomAssigned     | `reception.room.assigned`          | Reception   | Dashboard | bookingId, roomNumber, guestName |
| RoomVacated      | `reception.room.vacated`           | Reception   | Housekeeping, Dashboard | roomId, roomNumber, vacatedTime, needsCleaning |
| RoomCleaningStarted | `housekeeping.room.cleaning.started` | Housekeeping | Dashboard | roomId, roomNumber, housekeeperName |
| RoomCleaned      | `housekeeping.room.cleaned`        | Housekeeping | Dashboard | roomId, roomNumber, completedTime, passedInspection |
| RoomNeedsCleaning| `housekeeping.room.needs.cleaning` | Housekeeping | Dashboard | roomId, roomNumber, priority |
| RoomStatusChanged| `housekeeping.room.status.changed` | Housekeeping | Dashboard | roomId, previousStatus, newStatus |
| OrderReceived    | `roomservice.order.received`       | Room Service| Dashboard | orderId, roomNumber, totalAmount, itemCount |
| OrderPreparing   | `roomservice.order.preparing`      | Room Service| Dashboard | orderId, roomNumber, chefName |
| OrderOutForDelivery | `roomservice.order.outfordelivery` | Room Service | Dashboard | orderId, roomNumber, deliveryStaffName |
| OrderDelivered   | `roomservice.order.delivered`      | Room Service| Dashboard | orderId, roomNumber, deliveredTime, totalAmount |
| OrderCancelled   | `roomservice.order.cancelled`      | Room Service| Dashboard | orderId, roomNumber, reason, refundAmount |
| MaintenanceRequested | `maintenance.request.created`  | Maintenance | Dashboard | requestId, roomNumber, priority, description |
| MaintenanceAssigned  | `maintenance.request.assigned` | Maintenance | Dashboard | requestId, roomNumber, technicianName |
| MaintenanceStarted   | `maintenance.work.started`     | Maintenance | Dashboard | requestId, roomNumber, technicianName |
| MaintenanceCompleted | `maintenance.work.completed`   | Maintenance | Dashboard | requestId, roomNumber, resolutionNotes, roomBackInService |

---

## 6. Core Algorithms

### Room Assignment (Reception) — `src/Shared/Infrastructure/RoomAssignmentAlgorithm.cs`
Multi-criteria selection, evaluated in strict order:
1. **Room type match** — only rooms of the exact requested type.
2. **Cleanliness** — only `Clean` rooms are eligible (falls back to `Available` if none clean).
3. **Longest clean first** — orders eligible rooms by `LastCleaned` ascending (even rotation).
4. **Floor preference** — secondary filter; falls back to any floor if none on the preferred floor.
5. **Proximity** — final tiebreaker for elevator/stairs preference.

### Maintenance Priority Queue — `src/Services/MaintenanceService/Services/PriorityQueueService.cs`
Each request gets a priority score: `(priority × 1000) + waiting-time urgency + emergency bonus`.
Critical > High > Normal > Low, and ties are broken by submission time (oldest first). A background
service auto-assigns queued requests to the least-loaded available technician.

### Billing (Reception) — `src/Services/ReceptionService/Services/BillingService.cs`
`Total = (nightly rate × nights) + delivered room-service charges + additional charges + tax`.
Handles edge cases: minimum 1 night charged, zero charges, partial payments.

---

## 7. Data Structures (and why)

| Structure | Where | Why |
|-----------|-------|-----|
| `List<Room>` | Room inventory queries | Simple ordered iteration & LINQ filtering |
| `SortedList<int, PriorityQueueItem>` | Maintenance queue | Keeps requests ordered by priority score automatically |
| Order state queue | Room service workflow | Orders progress through ordered states |
| `Dictionary`/map (EF entities keyed by Id) | Guest & room records | O(1) lookup by primary key |

---

## 8. Security Considerations

- **Input validation** — DTOs use `System.ComponentModel.DataAnnotations` (`[Required]`, `[EmailAddress]`,
  `[Range]`, `[StringLength]`). Room numbers, guest names and order details are validated before processing.
- **Authentication** — the staff dashboard requires an access token before any sensitive data is shown
  (see `LoginGate.jsx`; token configured via `VITE_DASHBOARD_TOKEN`).
- **Limited data exposure** — dashboard DTOs deliberately omit passport numbers and full payment details;
  only the data needed for operations is sent over the wire.
- **Error handling** — every controller action wraps logic in try/catch, logs internally, and returns a
  safe message — raw stack traces are never returned to clients.

---

## 9. Test Scenarios

The system is designed to satisfy the brief's scenarios TS-01 → TS-08:

| ID | Scenario | Where handled |
|----|----------|---------------|
| TS-01 | Check-in requesting a double on floor 3 | `RoomAssignmentAlgorithm` (type + clean + floor) |
| TS-02 | Check-out of a room → bill + `RoomVacated` → Housekeeping queues it | `ReceptionService` + `ReceptionEventHandler` |
| TS-03 | Housekeeper marks room clean → dashboard updates live | `HousekeepingService` + SignalR |
| TS-04 | Guest orders coffees + sandwich, order progresses | `RoomService` order workflow |
| TS-05 | Critical broken-shower report enters queue at front | `PriorityQueueService` |
| TS-06 | Two simultaneous check-ins, no double-booking | EF transactions in `ReceptionService` |
| TS-07 | All rooms of type occupied → clear "no rooms" message | `RoomAssignmentAlgorithm` returns null → handled |
| TS-08 | Invalid room number → validation error, no crash | DTO validation + controller error handling |

---

## 10. Project Structure

```
HotelOS/
├── src/
│   ├── Services/
│   │   ├── ReceptionService/      # check-in/out, room assignment, billing
│   │   ├── HousekeepingService/   # cleaning workflow
│   │   ├── RoomService/           # food & beverage orders
│   │   ├── MaintenanceService/    # priority queue + technicians
│   │   ├── FrontendService/       # guest-portal API gateway
│   │   └── DashboardService/      # SignalR real-time dashboard
│   ├── Shared/
│   │   ├── Models/                # Room, Guest, Booking, Staff, Bill, etc.
│   │   ├── Events/                # broker event contracts
│   │   └── Infrastructure/        # DbContext, Redis broker, algorithms
│   └── Frontend/
│       ├── guest-portal/          # React customer app
│       └── staff-dashboard/       # React operations dashboard (SignalR)
├── docker-compose.yml
└── README.md
```

---

## 11. Notes & Limitations

- The SQLite database is shared across services via a Docker volume for demo simplicity. In a
  production microservice deployment each service would own its own database; here a shared store keeps
  the "runs with one command" requirement realistic.
- Built with **10 rooms across 2 floors** (seeded in `HotelDbContext`) as permitted by the brief.
- The dashboard authentication is a simplified token gate to demonstrate the concept, not production auth.

---

## 12. Git Log

Run `git log --oneline` to see the full commit history (the brief asks for ≥10 meaningful commits).
