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

    private void RunScript(Mesh M, bool TT, ref object B)
    {
        this.Component.Name = "ReBrepMesh";
        this.Component.NickName = "ReBrepMesh";
        this.Component.Category = "DDUClayPrintingSlicer";
        this.Component.SubCategory = "9 Utilities";

        if (M != null)
        {
            B = Brep.CreateFromMesh(M, TT);
        }
        else
        {
            B = new DataTree<object>();
        }
        
    }
}
