using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;

namespace LiquidUtilities
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class LiquidDisplay : MonoBehaviour
    {
        private ApplicationLauncherButton button;
        private bool visible;
        private bool showMap;
        private Rect windowRect = new Rect(100, 100, 300, 180);
        private Texture2D icon;

        public void Start()
        {
            icon = GameDatabase.Instance.GetTexture("001_LiquidUtilities/Assets/Logos/LiquidUtilities_Logo", false);
            if (ApplicationLauncher.Ready)
                AddButton();
            else
                GameEvents.onGUIApplicationLauncherReady.Add(AddButton);
        }

        private void AddButton()
        {
            if (button != null)
                return;
            button = ApplicationLauncher.Instance.AddModApplication(OnToggleOn, OnToggleOff, null, null, null, null, ApplicationLauncher.AppScenes.FLIGHT, icon);
        }

        private void OnToggleOn()
        {
            visible = true;
        }

        private void OnToggleOff()
        {
            visible = false;
        }

        public void OnGUI()
        {
            if (!visible)
                return;
            GUI.skin = HighLogic.Skin;
            windowRect = GUILayout.Window(GetInstanceID(), windowRect, Window, "Liquid Utilities");
        }

        private void Window(int id)
        {
            LiquidDefinition current = GetCurrent();
            string bodyName = "N/A";
            if (FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.mainBody != null)
                bodyName = FlightGlobals.ActiveVessel.mainBody.name;
            string type = current != null ? current.Type : "None";
            string density = current != null ? current.Density.ToString("F3") : "0.000";
            string drag = current != null ? current.DragMultiplier.ToString("F2") : "1.00";
            string name = current != null ? current.Name : "None";
            GUILayout.Label("Body: " + bodyName);
            GUILayout.Label("Water: " + name);
            GUILayout.Label("Type: " + type);
            GUILayout.Label("Density: " + density);
            GUILayout.Label("Drag: " + drag);
            if (FlightGlobals.ActiveVessel != null)
            {
                GUILayout.Label("Lat: " + FlightGlobals.ActiveVessel.latitude.ToString("F3"));
                GUILayout.Label("Lon: " + FlightGlobals.ActiveVessel.longitude.ToString("F3"));
            }
            LiquidMap map = null;
            if (FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.mainBody != null)
                MapManager.TryGet(FlightGlobals.ActiveVessel.mainBody.name, out map);
            bool hasMap = map != null && map.Texture != null;
            if (hasMap)
            {
                if (GUILayout.Button(showMap ? "Hide Map" : "Show Map"))
                    showMap = !showMap;
            }
            if (showMap && hasMap)
            {
                GUILayout.Label("Map: " + map.TexturePath);
                GUILayout.Box(map.Texture, GUILayout.Width(280), GUILayout.Height(140));
            }
            GUI.DragWindow();
        }

        private LiquidDefinition GetCurrent()
        {
            Vessel vessel = FlightGlobals.ActiveVessel;
            if (vessel == null)
                return null;
            if (vessel.mainBody == null)
                return null;
            LiquidDefinition mapDef = MapManager.Sample(vessel.mainBody.name, vessel.latitude, vessel.longitude);
            if (mapDef != null)
                return mapDef;
            List<LiquidDefinition> list;
            if (!LiquidManager.TryGetList(vessel.mainBody.name, out list))
                return null;
            LiquidDefinition global = null;
            for (int i = 0; i < list.Count; i++)
            {
                LiquidDefinition def = list[i];
                if (!def.HasLocation)
                {
                    if (global == null)
                        global = def;
                }
                else
                {
                    double dist = Distance(vessel.mainBody, vessel.latitude, vessel.longitude, def.Lat, def.Lon);
                    if (dist <= def.Radius)
                        return def;
                }
            }
            return global;
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

        public void OnDestroy()
        {
            if (button != null)
                ApplicationLauncher.Instance.RemoveModApplication(button);
            GameEvents.onGUIApplicationLauncherReady.Remove(AddButton);
        }
    }
}
