using System.Collections.Generic;
using UnityEngine;

namespace LiquidUtilities
{
    public class LiquidMap
    {
        public string BodyName;
        public string TexturePath;
        public Texture2D Texture;
        public List<LiquidDefinition> Liquids = new List<LiquidDefinition>();
    }
}
