# Event Reference – EDForceFeedbackXinput

Quick reference for every event type used and where it is read from. All sources go through EliteAPI (Journal + Status.json).

---

## Event flow overview

| Source | What EliteAPI provides | Our handler |
|--------|------------------------|-------------|
| Journal | Discrete events (Docked, FSDJump, etc.) | Typed handlers, OnAll, OnAllJson |
| Status.json | Full state file, event "Status" with Flags bitfield | OnAllJson → EmitStatusEventsFromJson |

---

## Events by source

### 1. Typed handlers (Journal only)

These come from Journal events. We use EliteAPI typed handlers that fire immediately when the event occurs.

| Event key (use in settings.json) | EliteAPI event | When it fires |
|----------------------------------|----------------|---------------|
| `Status.Docked:True` | DockedEvent | Docked at station |
| `Status.Docked:False` | UndockedEvent | Left station |
| `Status.Landed:True` | TouchdownEvent | Landed on planet surface |
| `Status.Landed:False` | LiftoffEvent | Lifted off planet |

---

### 2. Status.json (Flags bitfield)

Read from Status.json via `OnAllJson` when `eventName == "Status"`. We parse the `Flags` integer and emit on bit change. Status.json is updated every few seconds by Elite.

**Format:** `Status.{Field}:True` and `Status.{Field}:False` for each field below.

| Field | Bit | Value | Meaning |
|-------|-----|-------|---------|
| Gear | 2 | 4 | Landing gear deployed |
| Shields | 3 | 8 | Shields up |
| FlightAssist | 5 | 32 | Flight assist off |
| Hardpoints | 6 | 64 | Hardpoints deployed |
| Winging | 7 | 128 | In wing |
| Lights | 8 | 256 | Lights on |
| CargoScoop | 9 | 512 | Cargo scoop deployed |
| SilentRunning | 10 | 1024 | Silent running |
| Scooping | 11 | 2048 | Fuel scooping |
| SrvHandbreak | 12 | 4096 | SRV handbrake |
| SrvTurrent | 13 | 8192 | SRV turret view |
| SrvNearShip | 14 | 16384 | SRV near ship |
| SrvDriveAssist | 15 | 32768 | SRV drive assist |
| MassLocked | 16 | 65536 | FSD mass locked |
| FsdCharging | 17 | 131072 | FSD charging |
| FsdCooldown | 18 | 262144 | FSD cooldown |
| LowFuel | 19 | 524288 | Low fuel (<25%) |
| Overheating | 20 | 1048576 | Overheating (>100%) |

**Note:** Gear may also come from the `Gear` token in Status.json if present. When both exist, we use the token.

---

### 3. Status.json (skipped – handled elsewhere)

These flags are parsed from Status.json but **not** emitted by us, to avoid duplicates:

| Flag | Skipped because |
|------|-----------------|
| Docked (bit 0) | Handled by DockedEvent / UndockedEvent |
| Landed (bit 1) | Handled by TouchdownEvent / LiftoffEvent |
| Supercruise (bit 4) | Handled by SupercruiseEntry / SupercruiseExit |

---

### 4. Journal events (OnAll / OnAllJson)

Fired when EliteAPI receives a Journal event. The event name is used as the key.

#### Travel

| Event key | When it fires |
|-----------|---------------|
| `SupercruiseEntry` | Entered supercruise |
| `SupercruiseExit` | Exited supercruise |
| `FSDJump` | Completed hyperspace jump |
| `StartJump` | Started jump (hyperspace or supercruise) |

#### Combat

| Event key | When it fires |
|-----------|---------------|
| `HullDamage` | Hull took damage |
| `UnderAttack` | Under attack |
| `Interdicted` | Interdiction complete (victim) |
| `Interdiction` | Interdiction started (aggressor) |
| `EscapeInterdiction` | Escaped interdiction |
| `Died` | Ship destroyed |
| `CockpitBreached` | Cockpit breached |

#### Deployment

| Event key | When it fires |
|-----------|---------------|
| `LaunchSRV` | Launched SRV |
| `DockSRV` | Docked SRV |
| `LaunchFighter` | Launched fighter |
| `DockFighter` | Docked fighter |

#### Navigation & docking

| Event key | When it fires |
|-----------|---------------|
| `FuelScoop` | Scooping fuel |
| `ApproachSettlement` | Approaching settlement |
| `LeaveBody` | Left planet/body |
| `ApproachBody` | Approaching planet/body |
| `DockingRequested` | Requested docking |
| `DockingGranted` | Docking granted |
| `DockingDenied` | Docking denied |
| `DockingCancelled` | Docking cancelled |
| `DockingTimeout` | Docking timed out |

---

### 5. Suppressed events (do not use in settings)

These Journal events are **ignored** to avoid duplicate vibrations.

| Event name | Use instead |
|------------|-------------|
| `Docked` | `Status.Docked:True` |
| `Undocked` | `Status.Docked:False` |
| `Touchdown` | `Status.Landed:True` |
| `Liftoff` | `Status.Landed:False` |
| `Status` | Individual `Status.X:True/False` |
| `ShieldState` | `Status.Shields:True` / `Status.Shields:False` |
| `HeatWarning` | `Status.Overheating:True` |
| `HeatDamage` | `Status.Overheating:True` |

---

## Event key format

- **Status events:** `Status.{Field}:{True|False}` (e.g. `Status.Gear:True`)
- **Journal events:** Exact Journal event name (e.g. `SupercruiseEntry`, `FSDJump`)

The `Event` field in each StatusEvents entry must match one of these keys exactly (case-sensitive for `True`/`False`).

---

## Summary table

| Category | Source | Example keys |
|----------|--------|--------------|
| Typed (Journal) | DockedEvent, UndockedEvent, TouchdownEvent, LiftoffEvent | `Status.Docked:True`, `Status.Landed:False` |
| Status.json Flags | Status.json → EmitChangedStatusFlags | `Status.Gear:True`, `Status.Shields:False`, `Status.Overheating:True` |
| Status.json Gear token | Status.json → gearToken block | `Status.Gear:True` (when Gear token present) |
| Journal (OnAll) | Journal → Events_AllEvent | `SupercruiseEntry`, `FSDJump`, `HullDamage`, etc. |
