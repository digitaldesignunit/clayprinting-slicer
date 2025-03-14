// Grasshopper Script Instance
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

    private void RunScript(DataTree<object> T, List<object> P, ref object C)
    {
        // set component params
        this.Component.Name = "CullTreePaths";
        this.Component.NickName = "CullTreePaths";
        this.Component.Category = "DDUClayPrintingSlicer";
        this.Component.SubCategory = "9 Utilities";
        
        DataTree<object> NewTree = new DataTree<object>(T);
        if (P != null)
        {
            for (int i = 0; i < P.Count; i++)
            {
                // cast gh path to object
                GH_Path ghp = (GH_Path) P[i];
                // remove path from tree
                NewTree.RemovePath(ghp);
            }
        }
        C = NewTree;
    }
}
