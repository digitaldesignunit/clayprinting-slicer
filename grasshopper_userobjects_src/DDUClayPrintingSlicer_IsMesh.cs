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

    private void RunScript(GeometryBase G, ref object M)
    {
        // set component params
        this.Component.Name = "IsMesh";
        this.Component.NickName = "IsMesh";
        this.Component.Category = "DDUClayPrintingSlicer";
        this.Component.SubCategory = "9 Utilities";

        if (G != null)
        {
            if (G is Mesh)
            {
                M = true;
            }
            else if (G is Brep)
            {
                M = false;
            }
        }
        else
        {
            M = new DataTree<object>();
        }
    }
}
