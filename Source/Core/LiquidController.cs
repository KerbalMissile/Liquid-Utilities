using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiquidUtilities
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class LiquidController : MonoBehaviour
    {
        private Dictionary<Part, float> original = new Dictionary<Part, float>();
        private Dictionary<Part, double> originalSubmerged = new Dictionary<Part, double>();
        private Dictionary<Part, float> originalWaterAngular = new Dictionary<Part, float>();

        public void Awake()
        {
            LiquidManager.Load();
            MapManager.Load();
        }

        public void FixedUpdate()
        {
            if (FlightGlobals.Vessels == null)
                return;
            if (!FlightGlobals.ready)
                return;
            for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
            {
                Vessel vessel = FlightGlobals.Vessels[i];
                if (vessel == null)
                    continue;
                if (vessel.mainBody == null)
                    continue;
                if (vessel.packed && !vessel.isActiveVessel)
                    continue;
                if (vessel.parts == null)
                    continue;
                if (!vessel.mainBody.ocean)
                {
                    RestoreVessel(vessel);
                    continue;
                }
                LiquidDefinition chosen = null;
                LiquidDefinition mapDef = MapManager.Sample(vessel.mainBody.name, vessel.latitude, vessel.longitude);
                if (mapDef != null)
                    chosen = mapDef;
                if (chosen == null)
                {
                    List<LiquidDefinition> list;
                    if (LiquidManager.TryGetList(vessel.mainBody.name, out list))
                    {
                        LiquidDefinition global = null;
                        for (int l = 0; l < list.Count; l++)
                        {
                            LiquidDefinition def = list[l];
                            if (!def.HasLocation)
                            {
                                if (global == null)
                                    global = def;
                            }
                            else
                            {
                                double dist = Distance(vessel.mainBody, vessel.latitude, vessel.longitude, def.Lat, def.Lon);
                                if (dist <= def.Radius)
                                {
                                    chosen = def;
                                    break;
                                }
                            }
                        }
                        if (chosen == null)
                            chosen = global;
                    }
                }
                if (chosen == null)
                {
                    RestoreVessel(vessel);
                    continue;
                }
                float factor = (float)chosen.Density;
                float dragFactor = (float)chosen.DragMultiplier;
                if (dragFactor <= 0)
                    dragFactor = 1.0f;
                for (int p = 0; p < vessel.parts.Count; p++)
                {
                    Part part = vessel.parts[p];
                    if (part == null)
                        continue;
                    float baseBuoy;
                    if (!original.TryGetValue(part, out baseBuoy))
                    {
                        original[part] = part.buoyancy;
                        baseBuoy = part.buoyancy;
                    }
                    part.buoyancy = baseBuoy * factor;
                    double baseSub;
                    if (!originalSubmerged.TryGetValue(part, out baseSub))
                    {
                        originalSubmerged[part] = part.submergedDragScalar;
                        baseSub = part.submergedDragScalar;
                    }
                    part.submergedDragScalar = baseSub * dragFactor;
                    float baseAng;
                    if (!originalWaterAngular.TryGetValue(part, out baseAng))
                    {
                        originalWaterAngular[part] = part.waterAngularDragMultiplier;
                        baseAng = part.waterAngularDragMultiplier;
                    }
                    part.waterAngularDragMultiplier = baseAng * dragFactor;
                }
            }
            Cleanup();
        }

        private double Distance(CelestialBody body, double lat1, double lon1, double lat2, double lon2)
        {
            double rad1 = lat1 * Mathf.Deg2Rad;
            double rad2 = lat2 * Mathf.Deg2Rad;
            double dLat = (lat2 - lat1) * Mathf.Deg2Rad;
            double dLon = (lon2 - lon1) * Mathf.Deg2Rad;
            double a = Math.Sin(dLat * 0.5) * Math.Sin(dLat * 0.5) + Math.Cos(rad1) * Math.Cos(rad2) * Math.Sin(dLon * 0.5) * Math.Sin(dLon * 0.5);
            double c = 2 * Math.Asin(Math.Sqrt(a));
            return body.Radius * c;
        }

        private void RestoreVessel(Vessel vessel)
        {
            for (int p = 0; p < vessel.parts.Count; p++)
            {
                Part part = vessel.parts[p];
                if (part == null)
                    continue;
                float baseBuoy;
                if (original.TryGetValue(part, out baseBuoy))
                    part.buoyancy = baseBuoy;
                double baseSub;
                if (originalSubmerged.TryGetValue(part, out baseSub))
                    part.submergedDragScalar = baseSub;
                float baseAng;
                if (originalWaterAngular.TryGetValue(part, out baseAng))
                    part.waterAngularDragMultiplier = baseAng;
            }
        }

        private void Cleanup()
        {
            List<Part> toRemove = null;
            foreach (var kv in original)
            {
                if (kv.Key == null)
                {
                    if (toRemove == null)
                        toRemove = new List<Part>();
                    toRemove.Add(kv.Key);
                }
            }
            if (toRemove != null)
            {
                for (int i = 0; i < toRemove.Count; i++)
                {
                    original.Remove(toRemove[i]);
                    originalSubmerged.Remove(toRemove[i]);
                    originalWaterAngular.Remove(toRemove[i]);
                }
            }
        }

        public void OnDestroy()
        {
            foreach (var kv in original)
            {
                if (kv.Key != null)
                    kv.Key.buoyancy = kv.Value;
            }
            foreach (var kv in originalSubmerged)
            {
                if (kv.Key != null)
                    kv.Key.submergedDragScalar = kv.Value;
            }
            foreach (var kv in originalWaterAngular)
            {
                if (kv.Key != null)
                    kv.Key.waterAngularDragMultiplier = kv.Value;
            }
            original.Clear();
            originalSubmerged.Clear();
            originalWaterAngular.Clear();
        }
    }
}
