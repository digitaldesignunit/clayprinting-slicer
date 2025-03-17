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
    Version: 250317
    */
    #endregion

    private void RunScript(Curve Crv, Plane Pln, ref object CCWCrv)
    {
        // set component params
        this.Component.Name = "CrvEnsureCCW";
        this.Component.NickName = "CrvEnsureCCW";
        this.Component.Category = "DDUClayPrintingSlicer";
        this.Component.SubCategory = "2 Slice Processing";

        CCWCrv = new Grasshopper.DataTree<object>();

        if (Crv != null)
        {
            if (Crv.ClosedCurveOrientation(Pln) == CurveOrientation.Clockwise)
            {
                Crv.Reverse();
                CCWCrv = Crv;
            }
            else
            {
                CCWCrv = Crv;
            }
        }
    }
}
