using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiquidUtilities
{
    public static class LiquidManager
    {
        public static Dictionary<string, List<LiquidDefinition>> Definitions = new Dictionary<string, List<LiquidDefinition>>();

        public static void Load()
        {
            Definitions.Clear();
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("LiquidUtilities");
            if (nodes == null)
                return;
            for (int i = 0; i < nodes.Length; i++)
            {
                ConfigNode root = nodes[i];
                ConfigNode[] bodies = root.GetNodes("Body");
                if (bodies == null)
                    continue;
                for (int b = 0; b < bodies.Length; b++)
                {
                    ConfigNode bodyNode = bodies[b];
                    string bodyName = bodyNode.GetValue("name");
                    if (string.IsNullOrEmpty(bodyName))
                        continue;
                    ConfigNode[] liquids = bodyNode.GetNodes("Liquid");
                    if (liquids == null || liquids.Length == 0)
                    {
                        string type = bodyNode.GetValue("type");
                        string densityStr = bodyNode.GetValue("density");
                        string dragStr = bodyNode.GetValue("dragMultiplier");
                        double density = 1.0;
                        double drag = 1.0;
                        if (!string.IsNullOrEmpty(densityStr))
                        {
                            double parsed;
                            if (double.TryParse(densityStr, out parsed))
                                density = parsed;
                        }
                        if (!string.IsNullOrEmpty(dragStr))
                        {
                            double parsed;
                            if (double.TryParse(dragStr, out parsed))
                                drag = parsed;
                        }
                        if (string.IsNullOrEmpty(type))
                        {
                            if (density > 1.01)
                                type = "SaltWater";
                            else
                                type = "FreshWater";
                        }
                        LiquidDefinition def = new LiquidDefinition();
                        def.BodyName = bodyName;
                        def.Name = bodyName + "Ocean";
                        def.Type = type;
                        def.Density = density;
                        def.DragMultiplier = drag;
                        def.HasLocation = false;
                        Add(bodyName, def);
                    }
                    else
                    {
                        for (int l = 0; l < liquids.Length; l++)
                        {
                            ConfigNode liquidNode = liquids[l];
                            string name = liquidNode.GetValue("name");
                            if (string.IsNullOrEmpty(name))
                                name = "Liquid";
                            string type = liquidNode.GetValue("type");
                            string densityStr = liquidNode.GetValue("density");
                            string dragStr = liquidNode.GetValue("dragMultiplier");
                            double density = 1.0;
                            double drag = 1.0;
                            if (!string.IsNullOrEmpty(densityStr))
                            {
                                double parsed;
                                if (double.TryParse(densityStr, out parsed))
                                    density = parsed;
                            }
                            if (!string.IsNullOrEmpty(dragStr))
                            {
                                double parsed;
                                if (double.TryParse(dragStr, out parsed))
                                    drag = parsed;
                            }
                            if (string.IsNullOrEmpty(type))
                            {
                                if (density > 1.01)
                                    type = "SaltWater";
                                else
                                    type = "FreshWater";
                            }
                            string latStr = liquidNode.GetValue("lat");
                            string lonStr = liquidNode.GetValue("lon");
                            string radiusStr = liquidNode.GetValue("radius");
                            double lat = 0;
                            double lon = 0;
                            double radius = 0;
                            bool hasLat = false;
                            bool hasLon = false;
                            bool hasRadius = false;
                            if (!string.IsNullOrEmpty(latStr))
                                hasLat = double.TryParse(latStr, out lat);
                            if (!string.IsNullOrEmpty(lonStr))
                                hasLon = double.TryParse(lonStr, out lon);
                            if (!string.IsNullOrEmpty(radiusStr))
                                hasRadius = double.TryParse(radiusStr, out radius);
                            bool hasLocation = hasLat && hasLon && hasRadius;
                            LiquidDefinition def = new LiquidDefinition();
                            def.BodyName = bodyName;
                            def.Name = name;
                            def.Type = type;
                            def.Density = density;
                            def.DragMultiplier = drag;
                            def.HasLocation = hasLocation;
                            if (hasLocation)
                            {
                                def.Lat = lat;
                                def.Lon = lon;
                                def.Radius = radius;
                            }
                            Add(bodyName, def);
                        }
                    }
                }
            }
            int total = 0;
            foreach (var kv in Definitions)
                total += kv.Value.Count;
            Debug.Log("[LiquidUtilities] Loaded " + total + " liquids on " + Definitions.Count + " bodies");
        }

        private static void Add(string bodyName, LiquidDefinition def)
        {
            List<LiquidDefinition> list;
            if (!Definitions.TryGetValue(bodyName, out list))
            {
                list = new List<LiquidDefinition>();
                Definitions[bodyName] = list;
            }
            list.Add(def);
        }

        public static bool TryGetList(string bodyName, out List<LiquidDefinition> list)
        {
            return Definitions.TryGetValue(bodyName, out list);
        }
    }
}
