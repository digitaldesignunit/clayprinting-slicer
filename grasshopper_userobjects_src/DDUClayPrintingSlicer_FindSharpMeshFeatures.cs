#region Usings
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Collections;

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

    private void RunScript(Mesh M, double A, ref object E)
    {
        // set component params
        this.Component.Name = "FindSharpMeshFeatures";
        this.Component.NickName = "FindSharpMeshFeatures";
        this.Component.Category = "DDUClayPrintingSlicer";
        this.Component.SubCategory = "6 Analysis";

        if (M != null)
        {
            // get topology edges
            MeshTopologyEdgeList Edges = M.TopologyEdges;
            MeshFaceNormalList FaceNormals = M.FaceNormals;
            // create list for storing sharp edges
            List<Line> SharpEdges = new List<Line>();
            // loop over all edges
            for (int i = 0; i < Edges.Count; i++)
            {
                // for every edge, get adjacent faces
                int[] fids = Edges.GetConnectedFaces(i);
                // only continue if there are exactly two faces adjacent to this edge
                if (fids.Length == 2)
                {
                    // compute angle between face normals
                    if (RhinoMath.ToDegrees(Vector3d.VectorAngle(FaceNormals[fids[0]], FaceNormals[fids[1]])) > A)
                    {
                    SharpEdges.Add(Edges.EdgeLine(i));
                    }
                }
            }
            E = SharpEdges;
        }
        else
        {
            E = new DataTree<object>();
        }
    }
}
