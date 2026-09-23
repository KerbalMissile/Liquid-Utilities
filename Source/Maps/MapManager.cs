using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LiquidUtilities
{
    public static class MapManager
    {
        public static Dictionary<string, LiquidMap> Maps = new Dictionary<string, LiquidMap>();

        public static void Load()
        {
            foreach (var kv in Maps)
            {
                if (kv.Value.Texture != null)
                    UnityEngine.Object.Destroy(kv.Value.Texture);
            }
            Maps.Clear();
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
                    ConfigNode mapNode = bodyNode.GetNode("Map");
                    if (mapNode == null)
                        continue;
                    string texture = mapNode.GetValue("texture");
                    if (string.IsNullOrEmpty(texture))
                        continue;
                    LiquidMap map = new LiquidMap();
                    map.BodyName = bodyName;
                    map.TexturePath = texture;
                    map.Texture = LoadTexture(texture);
                    if (map.Texture == null)
                    {
                        Debug.Log("[LiquidUtilities] Failed to load map texture " + texture + " for " + bodyName);
                        continue;
                    }
                    ConfigNode[] liquids = mapNode.GetNodes("Liquid");
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
                        string colorStr = liquidNode.GetValue("color");
                        Color32 color = new Color32(0, 0, 0, 255);
                        bool hasColor = false;
                        if (!string.IsNullOrEmpty(colorStr))
                        {
                            string[] parts = colorStr.Split(',');
                            if (parts.Length >= 3)
                            {
                                byte r = 0;
                                byte g = 0;
                                byte bcol = 0;
                                byte a = 255;
                                byte.TryParse(parts[0].Trim(), out r);
                                byte.TryParse(parts[1].Trim(), out g);
                                byte.TryParse(parts[2].Trim(), out bcol);
                                if (parts.Length >= 4)
                                    byte.TryParse(parts[3].Trim(), out a);
                                color = new Color32(r, g, bcol, a);
                                hasColor = true;
                            }
                        }
                        LiquidDefinition def = new LiquidDefinition();
                        def.BodyName = bodyName;
                        def.Name = name;
                        def.Type = type;
                        def.Density = density;
                        def.DragMultiplier = drag;
                        def.HasLocation = false;
                        def.MapColor = color;
                        def.HasMapColor = hasColor;
                        map.Liquids.Add(def);
                    }
                    Maps[bodyName] = map;
                    Debug.Log("[LiquidUtilities] Loaded map for " + bodyName + " " + texture + " " + map.Texture.width + "x" + map.Texture.height + " " + map.Liquids.Count + " liquids");
                }
            }
        }

        private static Texture2D LoadTexture(string texturePath)
        {
            string root = KSPUtil.ApplicationRootPath;
            string basePath = Path.Combine(root, "GameData", texturePath);
            string full = basePath;
            string ext = Path.GetExtension(full);
            if (!string.IsNullOrEmpty(ext))
                basePath = full.Substring(0, full.Length - ext.Length);
            string[] tryExts = new string[] { ".png", ".jpg", ".jpeg" };
            for (int i = 0; i < tryExts.Length; i++)
            {
                string candidate = basePath + tryExts[i];
                if (File.Exists(candidate))
                {
                    byte[] bytes = File.ReadAllBytes(candidate);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                    tex.LoadImage(bytes);
                    tex.filterMode = FilterMode.Bilinear;
                    return tex;
                }
            }
            if (File.Exists(full))
            {
                byte[] bytes = File.ReadAllBytes(full);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                tex.LoadImage(bytes);
                tex.filterMode = FilterMode.Bilinear;
                return tex;
            }
            Texture2D dbTex = GameDatabase.Instance.GetTexture(texturePath, false);
            if (dbTex != null)
            {
                Texture2D copy = new Texture2D(dbTex.width, dbTex.height, TextureFormat.ARGB32, false);
                RenderTexture rt = RenderTexture.GetTemporary(dbTex.width, dbTex.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(dbTex, rt);
                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = rt;
                copy.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                copy.Apply();
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                return copy;
            }
            return null;
        }

        public static bool TryGet(string bodyName, out LiquidMap map)
        {
            return Maps.TryGetValue(bodyName, out map);
        }

        public static LiquidDefinition Sample(string bodyName, double lat, double lon)
        {
            LiquidMap map;
            if (!Maps.TryGetValue(bodyName, out map))
                return null;
            if (map.Texture == null)
                return null;
            double u = (lon + 180.0) / 360.0;
            double v = (lat + 90.0) / 180.0;
            u = u - Math.Floor(u);
            v = Math.Max(0, Math.Min(1, v));
            int x = (int)(u * (map.Texture.width - 1));
            int y = (int)(v * (map.Texture.height - 1));
            x = Math.Max(0, Math.Min(map.Texture.width - 1, x));
            y = Math.Max(0, Math.Min(map.Texture.height - 1, y));
            Color32 pixel = map.Texture.GetPixel(x, y);
            if (pixel.a == 0)
                return null;
            for (int i = 0; i < map.Liquids.Count; i++)
            {
                LiquidDefinition def = map.Liquids[i];
                if (!def.HasMapColor)
                    continue;
                if (def.MapColor.r == pixel.r && def.MapColor.g == pixel.g && def.MapColor.b == pixel.b)
                    return def;
            }
            return null;
        }
    }
}
