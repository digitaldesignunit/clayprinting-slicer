#region Usings
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Drawing;
using System.Threading.Tasks;

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
		List<Curve> Curves,
		double Radius,
		int SubSegments,
		int Accuracy,
		int CapStyle,
		ref object MeshPipes)
  {
    // set component params
    this.Component.Name = "MeshPipe";
    this.Component.NickName = "MeshPipe";
    this.Component.Category = "DDUClayPrintingSlicer";
    this.Component.SubCategory = "8 Visualisation";

    ConcurrentBag<Mesh> MeshPipesBag = new ConcurrentBag<Mesh>();
    MeshPipeCapStyle mpcs = (MeshPipeCapStyle) CapStyle;

    if (Curves == null)
    {
        MeshPipes = new DataTree<object>();
        return;
    }

    if (Curves.Count > 20)
    {
      // create pipes in parallel
      Parallel.For(0, Curves.Count,
        i => {
        // create mesh pipe
        Mesh MeshPipe = Mesh.CreateFromCurvePipe(Curves[i], Radius, SubSegments, Accuracy, mpcs, true);
        // add to tree
        MeshPipesBag.Add(MeshPipe);
        });
    }
    else
    {
      // create pipes sequential
      for (int i = 0; i < Curves.Count; i++)
      {
        // create mesh pipe
        Mesh mp = Mesh.CreateFromCurvePipe(Curves[i], Radius, SubSegments, Accuracy, mpcs, true);
        // add to tree
        MeshPipesBag.Add(mp);
      }
    }
    MeshPipes = MeshPipesBag.ToArray();
  }

}

