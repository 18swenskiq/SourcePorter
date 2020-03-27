using System;
using System.Collections.Generic;
using VMFParser;

namespace SourcePorter
{
    public class MaterialReader
    {
        public List<string> GetWorldGeoMaterials(VMF myVMF)
        {
            var materialsused = new List<string>();
            foreach (var brush in myVMF.World.Body)
            {
                if (brush.Name == "solid")   // If we find the name solid, that means that its a brush
                {
                    var thisblock = (VBlock)brush;
                    foreach (var side in thisblock.Body)
                    {
                        if (side.Name == "side")
                        {
                            var thisside = (VBlock)side;
                            foreach (var plane in thisside.Body)
                            {
                                if (plane.Name == "material")
                                {
                                    var thisname = (VProperty)plane;
                                    if (!materialsused.Contains(thisname.Value))
                                    {
                                        materialsused.Add(thisname.Value);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return materialsused;
        }

        public List<string> CombineMaterials(List<string> decals, List<string> overlays, List<string> brushentMaterials, List<string> worldgeoMaterials)
        {
            var combinedList = new List<string>();
            foreach(var decal in decals)
            {
                if(!combinedList.Contains(decal.ToLower()))
                {
                    combinedList.Add(decal.ToLower());
                }
            }
            foreach(var overlay in overlays)
            {

                if(!combinedList.Contains(overlay.ToLower()))
                {
                    combinedList.Add(overlay.ToLower());
                }
            }
            foreach(var brushentMaterial in brushentMaterials)
            {
                if(!combinedList.Contains(brushentMaterial.ToLower()))
                {
                    combinedList.Add(brushentMaterial.ToLower());
                }
            }
            foreach(var worldgeoMaterial in worldgeoMaterials)
            {
                if(!combinedList.Contains(worldgeoMaterial.ToLower()))
                {
                    combinedList.Add(worldgeoMaterial.ToLower());
                }
            }
            return combinedList;
        }
    }
}
