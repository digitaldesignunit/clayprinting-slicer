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
		GeometryBase G,
		string T,
		bool CC,
		ref object IsMesh,
		ref object Closed,
		ref object NakedEdges,
		ref object Info)
    {
        // set component params
        this.Component.Name = "MeshBrepAnalysis";
        this.Component.NickName = "MeshBrepAnalysis";
        this.Component.Category = "DDUClayPrintingSlicer";
        this.Component.SubCategory = "9 Utilities";

        bool msh = true;
        bool clsd = false;
        int nB = 0;

        if (G is Mesh)
        {
        msh = true;
        Mesh tMesh = (Mesh) G;
        clsd = tMesh.IsClosed;
        if (clsd == false)
        {
            Curve[] nEdges = Curve.JoinCurves(Array.ConvertAll(tMesh.GetNakedEdges(), crv => crv.ToPolylineCurve()));
            NakedEdges = nEdges;
            nB = nEdges.Length;
        }
        else
        {
            NakedEdges = new Grasshopper.DataTree<object>();
        }
        IsMesh = msh;
        Closed = clsd;
        }
        else if (G is Brep)
        {
        msh = false;
        Brep tBrep = (Brep) G;
        clsd = tBrep.IsSolid;
        if (clsd == false)
        {
            Curve[] nEdges = Curve.JoinCurves(tBrep.DuplicateNakedEdgeCurves(true, true));
            NakedEdges = nEdges;
            nB = nEdges.Length;
        }
        else
        {
            NakedEdges = new Grasshopper.DataTree<object>();
        }
        IsMesh = msh;
        Closed = clsd;
        }

        string baseStr = "[INFO] {0}Geometry is a{1} {2}{3}!";
        string iclsd = (clsd) ? " closed" : "n open";
        string imsh = (msh) ? "MESH" : "BREP";
        string inb = (nB > 0) ? String.Format(" with {0} boundaries", nB) : "";
        Info = String.Format(baseStr, T, iclsd, imsh, inb);

        if (CC == true)
        {
        IsMesh = msh;
        Closed = clsd;
        NakedEdges = new Grasshopper.DataTree<object>();
        Info = new List<string> {"[INFO] Custom layer CURVES are being used!", "[INFO] Analysis tools DEACTIVATED (Custom Curves)!"};
        }
    }
}
