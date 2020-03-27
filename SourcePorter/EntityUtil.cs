using System;
using System.Collections.Generic;
using VMFParser;

namespace SourcePorter
{
    public class EntityUtil
    {
        public List<string> GetPropModelList(List<VBlock> entities)
        {
            var props = new List<string>();
            foreach (var entity in entities)
            {
                foreach (var entdata in entity.Body)
                {
                    if (entdata.Name == "classname")
                    {
                        var thisentdata = (VProperty)entdata;
                        if (thisentdata.Value.StartsWith("prop_"))
                        {
                            foreach(var newentdata in entity.Body)
                            {
                                if (newentdata.Name == "model")
                                {
                                    var newentcast = (VProperty)newentdata;
                                    if(!props.Contains(newentcast.Value))
                                    {
                                        props.Add(newentcast.Value);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return props;
        }

        public List<string> GetBrushEntityMaterialList(List<VBlock> entities)
        {
            var brushents = new List<string>();
            foreach (var entity in entities)
            {
                foreach (var entdata in entity.Body)
                {
                    if (entdata.Name == "classname")
                    {
                        var thisentdata = (VProperty)entdata;
                        if (thisentdata.Value.StartsWith("func_"))
                        {
                            foreach (var newentdata in entity.Body)
                            {
                                if (newentdata.Name == "solid")
                                {
                                    var solid = (VBlock)newentdata;
                                    foreach (var side in solid.Body)
                                    {
                                        if (side.Name == "side")
                                        {
                                            var thisside = (VBlock)side;
                                            foreach (var sideprop in thisside.Body)
                                            {
                                                if(sideprop.Name == "material")
                                                {
                                                    var thismat = (VProperty)sideprop;
                                                    if(!brushents.Contains(thismat.Value))
                                                    {
                                                        brushents.Add(thismat.Value);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return brushents;
        }


        public List<string> GetDecalMaterialList(List<VBlock> entities)
        {
            var decals = new List<string>();
            foreach (var entity in entities)
            {
                foreach (var entdata in entity.Body)
                {
                    if (entdata.Name == "classname")
                    {
                        var thisentdata = (VProperty)entdata;
                        if (thisentdata.Value == ("infodecal"))
                        {
                            foreach (var newentdata in entity.Body)
                            {
                                if (newentdata.Name == "texture")
                                {
                                    var newentcast = (VProperty)newentdata;
                                    if (!decals.Contains(newentcast.Value))
                                    {
                                        decals.Add(newentcast.Value);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return decals;
        }

        public List<string> GetOverlayMaterialList(List<VBlock> entities)
        {
            var overlays = new List<string>();
            foreach (var entity in entities)
            {
                foreach (var entdata in entity.Body)
                {
                    if (entdata.Name == "classname")
                    {
                        var thisentdata = (VProperty)entdata;
                        if (thisentdata.Value == ("info_overlay"))
                        {
                            foreach (var newentdata in entity.Body)
                            {
                                if (newentdata.Name == "material")
                                {
                                    var newentcast = (VProperty)newentdata;
                                    if (!overlays.Contains(newentcast.Value))
                                    {
                                        overlays.Add(newentcast.Value);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return overlays;
        }


        public List<VBlock> GetEntityList(VMF myVMF)
        {
            var entities = new List<VBlock>();
            foreach (var entity in myVMF.Body)
            {
                if (entity.Name == "entity")
                {
                    var thisentity = (VBlock)entity;
                    entities.Add(thisentity);
                }
            }
            return entities;
        }
    }
}
