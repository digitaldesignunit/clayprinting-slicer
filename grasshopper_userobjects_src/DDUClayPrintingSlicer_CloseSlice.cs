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
        Version: 250505
    */
    #endregion

    private void RunScript(Polyline P, double t, ref object CP, ref object R)
    {
        // GHENV COMPONENT SETTINGS
        this.Component.Name = "CloseSlice";
        this.Component.NickName = "CloseSlice";
        this.Component.Category = "DDUClayPrintingSlicer";
        this.Component.SubCategory = "1 Slicing";

        Polyline Slice;
        if (P == null)
        {
            CP = new DataTree<object>();
            R = new DataTree<object>();
            return;
        }
        if (t == null)
        {
            t = 0;
        }

        Slice = P.Duplicate();

        if (Slice.IsClosed)
        {
            CP = Slice;
            R = true;
        }
        else
        {
            if (t == 0)
            {
                Slice.Add(new Point3d(Slice[0]));
                CP = Slice;
                R = true;
            }
            else if (t > 0.0 && Slice[0].DistanceTo(Slice.Last) < t)
            {
                Slice.Add(new Point3d(Slice[0]));
                CP = Slice;
                R = true;
            }
            else if (t > 0.0 && Slice[0].DistanceTo(Slice.Last) > t)
            {
                CP = Slice;
                R = false;
            }
        }
    }
}
