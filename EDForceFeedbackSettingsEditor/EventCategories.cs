using System;
using System.Collections.Generic;
using System.Linq;

namespace EDForceFeedbackSettingsEditor
{
    /// <summary>Event categories matching EVENT_REFERENCE.md for organized display in the settings editor.</summary>
    internal static class EventCategories
    {
        /// <summary>Category display name and event keys that belong to it. Order defines display order.</summary>
        public static readonly IReadOnlyList<(string Name, HashSet<string> EventKeys)> Categories = new List<(string, HashSet<string>)>
        {
            ("Docking & Landing", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Status.Docked:True", "Status.Docked:False",
                "Status.Landed:True", "Status.Landed:False"
            }),
            ("Ship Status", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Status.Gear:True", "Status.Gear:False",
                "Status.Shields:True", "Status.Shields:False",
                "Status.FlightAssist:True", "Status.FlightAssist:False",
                "Status.Hardpoints:True", "Status.Hardpoints:False",
                "Status.Winging:True", "Status.Winging:False",
                "Status.Lights:True", "Status.Lights:False",
                "Status.CargoScoop:True", "Status.CargoScoop:False",
                "Status.SilentRunning:True", "Status.SilentRunning:False",
                "Status.Scooping:True", "Status.Scooping:False"
            }),
            ("SRV Status", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Status.SrvHandbreak:True", "Status.SrvHandbreak:False",
                "Status.SrvTurrent:True", "Status.SrvTurrent:False",
                "Status.SrvNearShip:True", "Status.SrvNearShip:False",
                "Status.SrvDriveAssist:True", "Status.SrvDriveAssist:False"
            }),
            ("FSD & Systems", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Status.MassLocked:True", "Status.MassLocked:False",
                "Status.FsdCharging:True", "Status.FsdCharging:False",
                "Status.FsdCooldown:True", "Status.FsdCooldown:False",
                "Status.LowFuel:True", "Status.LowFuel:False",
                "Status.Overheating:True", "Status.Overheating:False"
            }),
            ("Travel", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "SupercruiseEntry", "SupercruiseExit", "FSDJump", "StartJump"
            }),
            ("Combat", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "HullDamage", "UnderAttack", "Interdicted", "Interdiction",
                "EscapeInterdiction", "Died", "CockpitBreached"
            }),
            ("Deployment", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "LaunchSRV", "DockSRV", "LaunchFighter", "DockFighter"
            }),
            ("Navigation & Docking", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "FuelScoop", "ApproachSettlement", "LeaveBody", "ApproachBody",
                "DockingRequested", "DockingGranted", "DockingDenied",
                "DockingCancelled", "DockingTimeout"
            })
        };

        /// <summary>Returns the category name for an event key, or null if uncategorized.</summary>
        public static string GetCategory(string eventKey)
        {
            if (string.IsNullOrEmpty(eventKey)) return null;
            foreach (var (name, keys) in Categories)
            {
                if (keys.Contains(eventKey))
                    return name;
            }
            return null;
        }

        /// <summary>Groups events by category in display order. Uncategorized events go into "Other" at the end.</summary>
        public static IReadOnlyList<(string CategoryName, List<StatusEventModel> Events)> GroupEvents(IEnumerable<StatusEventModel> events)
        {
            var eventList = events.ToList();
            var result = new List<(string, List<StatusEventModel>)>();
            var categorized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (name, keys) in Categories)
            {
                var list = eventList.Where(e => keys.Contains(e.Event ?? "")).ToList();
                if (list.Count > 0)
                {
                    result.Add((name, list));
                    foreach (var e in list)
                        categorized.Add(e.Event ?? "");
                }
            }

            var uncategorized = eventList.Where(e => !categorized.Contains(e.Event ?? "")).ToList();
            if (uncategorized.Count > 0)
                result.Add(("Other", uncategorized));

            return result;
        }
    }
}
