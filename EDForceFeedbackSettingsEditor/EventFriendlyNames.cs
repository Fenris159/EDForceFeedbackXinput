using System.Collections.Generic;

namespace EDForceFeedbackSettingsEditor
{
    /// <summary>Maps event keys to friendly display labels.</summary>
    static class EventFriendlyNames
    {
        private static readonly Dictionary<string, string> Map = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "Status.Docked:True", "Docked (at station)" },
            { "Status.Docked:False", "Undocked (left station)" },
            { "Status.Landed:True", "Landed / Touchdown" },
            { "Status.Landed:False", "Liftoff" },
            { "Status.Gear:True", "Landing gear deployed" },
            { "Status.Gear:False", "Landing gear retracted" },
            { "Status.Shields:True", "Shields on" },
            { "Status.Shields:False", "Shields off" },
            { "Status.FlightAssist:True", "Flight assist on" },
            { "Status.FlightAssist:False", "Flight assist off" },
            { "Status.Hardpoints:True", "Hardpoints deployed" },
            { "Status.Hardpoints:False", "Hardpoints retracted" },
            { "Status.Winging:True", "Wing beacon on" },
            { "Status.Winging:False", "Wing beacon off" },
            { "Status.Lights:True", "Lights on" },
            { "Status.Lights:False", "Lights off" },
            { "Status.CargoScoop:True", "Cargo scoop deployed" },
            { "Status.CargoScoop:False", "Cargo scoop retracted" },
            { "Status.SilentRunning:True", "Silent running on" },
            { "Status.SilentRunning:False", "Silent running off" },
            { "Status.Scooping:True", "Fuel scooping" },
            { "Status.Scooping:False", "Fuel scoop retracted" },
            { "Status.SrvHandbreak:True", "SRV handbrake on" },
            { "Status.SrvHandbreak:False", "SRV handbrake off" },
            { "Status.SrvTurrent:True", "SRV turret mode" },
            { "Status.SrvTurrent:False", "SRV drive mode" },
            { "Status.SrvNearShip:True", "SRV near ship" },
            { "Status.SrvNearShip:False", "SRV away from ship" },
            { "Status.SrvDriveAssist:True", "SRV drive assist on" },
            { "Status.SrvDriveAssist:False", "SRV drive assist off" },
            { "Status.MassLocked:True", "Mass locked" },
            { "Status.MassLocked:False", "Mass lock cleared" },
            { "Status.FsdCharging:True", "FSD charging" },
            { "Status.FsdCharging:False", "FSD charge complete" },
            { "Status.FsdCooldown:True", "FSD cooldown" },
            { "Status.FsdCooldown:False", "FSD ready" },
            { "Status.LowFuel:True", "Low fuel warning" },
            { "Status.LowFuel:False", "Fuel OK" },
            { "Status.Overheating:True", "Overheating" },
            { "Status.Overheating:False", "Temperature OK" },
            { "SupercruiseEntry", "Enter supercruise" },
            { "SupercruiseExit", "Exit supercruise" },
            { "FSDJump", "FSD jump" },
            { "StartJump", "Jump started" },
            { "HullDamage", "Hull damage" },
            { "UnderAttack", "Under attack" },
            { "Interdicted", "Interdicted" },
            { "Interdiction", "Interdiction" },
            { "EscapeInterdiction", "Escape interdiction" },
            { "Died", "Died" },
            { "CockpitBreached", "Cockpit breached" },
            { "LaunchSRV", "Launch SRV" },
            { "DockSRV", "Dock SRV" },
            { "LaunchFighter", "Launch fighter" },
            { "DockFighter", "Dock fighter" },
            { "FuelScoop", "Fuel scooping" },
            { "ApproachSettlement", "Approach settlement" },
            { "LeaveBody", "Leave body" },
            { "ApproachBody", "Approach body" },
            { "DockingRequested", "Docking requested" },
            { "DockingGranted", "Docking granted" },
            { "DockingDenied", "Docking denied" },
            { "DockingCancelled", "Docking cancelled" },
            { "DockingTimeout", "Docking timeout" },
        };

        public static string GetFriendlyName(string eventKey)
        {
            return Map.TryGetValue(eventKey ?? "", out var name) ? name : (eventKey ?? "Unknown");
        }
    }
}
