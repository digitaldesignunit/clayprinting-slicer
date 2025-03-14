#region Usings
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
#endregion

public class Script_Instance : GH_ScriptInstance
{
    #region Notes
    /* 
      Members:
        RhinoDoc RhinoDocument
        GH_Document GrasshopperDocument
        IGH_Component Component
        int Iteration

      Methods (Virtual & overridable):
        Print(string text)
        Print(string format, params object[] args)
        Reflect(object obj)
        Reflect(object obj, string method_name)

    Author: Max Benjamin Eschenbach
    License: MIT License
    Version: 250314
    */
    #endregion

    private void RunScript(
		DataTree<Brep> SliceSurfaces,
		ref object InnerCurves,
		ref object OuterCurves)
    {
        // set component params
        this.Component.Name = "InnerOuterCurves";
        this.Component.NickName = "InnerOuterCurves";
        this.Component.Category = "DDUClayPrintingSlicer";
        this.Component.SubCategory = "2 Slice Processing";

        DataTree<Curve> iLoops = new DataTree<Curve>();
        DataTree<Curve> oLoops = new DataTree<Curve>();
        List<Brep> Breps;
        for (int i = 0; i < SliceSurfaces.BranchCount; i++)
        {
            Breps = SliceSurfaces.Branches[i];
            for (int j = 0; j < Breps.Count; j++)
            {
                int fc = Breps[j].Faces.Count;
                for (int k = 0; k < fc; k++)
                {
                    BrepFace bface = Breps[j].Faces[k];
                    int iCount = 0;
                    foreach (BrepLoop floop in bface.Loops)
                    {
                        if (floop.LoopType == BrepLoopType.Inner)
                        {
                            iLoops.Add(floop.To3dCurve(), SliceSurfaces.Paths[i]);
                        }
                        else if (floop.LoopType == BrepLoopType.Outer)
                        {
                            oLoops.Add(floop.To3dCurve(), SliceSurfaces.Paths[i]);
                        }
                    }
                }
            }
        }
        InnerCurves = iLoops;
        OuterCurves = oLoops;
    }
}
