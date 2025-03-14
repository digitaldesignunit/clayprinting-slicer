// Grasshopper Script Instance
#region Usings
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Rhino;
using Rhino.Geometry;
using Rhino.Collections;

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

    private void RunScript(DataTree<Curve> Slices, ref object OrderedSlices)
    {
        // set component params
        this.Component.Name = "OrderBranchingSlices";
        this.Component.NickName = "OrderBranchingSlices";
        this.Component.Category = "DDUClayPrintingSlicer";
        this.Component.SubCategory = "2 Slice Processing";

        double tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
        DataTree<Brep> SliceSurfaces = new DataTree<Brep>();
        for (int i = 0; i < Slices.BranchCount; i++)
        {
            GH_Path ghp = new GH_Path(Slices.Paths[i]);
            Brep[] planarBreps = Brep.CreatePlanarBreps(Slices.Branches[i], tol);
            if (planarBreps != null && planarBreps.Length > 0)
            {
                SliceSurfaces.AddRange(planarBreps, ghp);
            }
            
        }
        OrderedSlices = OrderSliceSurfaces(SliceSurfaces);
    }

    public Point3d GetSurfaceCentroid(Brep srf)
    {
        Curve[] borders = srf.DuplicateEdgeCurves(false);
        Point3d centroid_sum = new Point3d(0, 0, 0);
        for (int i = 0; i < borders.Length; i++)
        {

        centroid_sum += borders[i].ToPolyline(0.1, 0, 0, 0).ToPolyline().CenterPoint();
        }
        return centroid_sum / borders.Length;
    }

    public DataTree<Brep> OrderSliceSurfaces(DataTree<Brep> SliceSurfaces)
    {
        // loop over datatree and ensure correct order
        for (int i = 0; i < SliceSurfaces.BranchCount - 1; i++)
        {
        // get branches
        var this_branch = SliceSurfaces.Branches[i];
        var next_branch = SliceSurfaces.Branches[i + 1];

        // case 1: this branch one curve and next branch one curve
        if (this_branch.Count == 1 && next_branch.Count == 1)
        {
            Print("Case 1, 1 -> 1 (nothing done)");
            // nothing needs to be done
        }
            // case 2: this branch one curve and next branch multiple curves
        else if (this_branch.Count == 1 && next_branch.Count > 1)
        {
            Print("Case 2, 1 -> N");
            // loop over next branch and find all centerpoints to compare against
            Point3dList ptL = new Point3dList();
            for (int k = 0; k < next_branch.Count; k++)
            {
            //Point3d cpB = AreaMassProperties.Compute(next_branch[k]).Centroid;
            Point3d cpB = GetSurfaceCentroid(next_branch[k]);
            ptL.Add(cpB);
            }
            // create new list for storage
            List<Brep> OrderedNextBranch = new List<Brep>();
            // get curve centerpoint
            // Point3d cpA = AreaMassProperties.Compute(this_branch[0]).Centroid;
            Point3d cpA = GetSurfaceCentroid(this_branch[0]);
            // compare against point3dlist of other centerpoints
            int idx = ptL.ClosestIndex(cpA);
            OrderedNextBranch.Add((Brep) next_branch[idx].Duplicate());
            next_branch.RemoveAt(idx);
            OrderedNextBranch.AddRange(next_branch);
            // clear data
            ptL.Clear();
            // restructure next branch of contours tree
            SliceSurfaces.Branches[i + 1].Clear();
            SliceSurfaces.Branches[i + 1].AddRange(OrderedNextBranch);
        }
            // case 3: this branch and next branch have multiple curves but the same count
        else if (this_branch.Count > 1 && this_branch.Count == next_branch.Count)
        {
            Print("Case 3, N -> N");
            // create new list for storage
            List<Brep> OrderedNextBranch = new List<Brep>();
            // loop over all curves in this branch
            for (int j = 0; j < this_branch.Count; j++)
            {
            if (next_branch.Count < 1) break;
            // loop over next branch and find all centerpoints to compare against
            Point3dList ptL = new Point3dList();
            for (int k = 0; k < next_branch.Count; k++)
            {
                //Point3d cpB = AreaMassProperties.Compute(next_branch[k]).Centroid;
                Point3d cpB = GetSurfaceCentroid(next_branch[k]);
                ptL.Add(cpB);
            }
            // get curve centerpoint
            //Point3d cpA = AreaMassProperties.Compute(this_branch[j]).Centroid;
            Point3d cpA = GetSurfaceCentroid(this_branch[j]);
            // compare against point3dlist of other centerpoints
            int idx = ptL.ClosestIndex(cpA);
            // add closest item to ordered list
            OrderedNextBranch.Add((Brep) next_branch[idx].Duplicate());
            // remove closest item from candidates
            next_branch.RemoveAt(idx);
            // clear data
            ptL.Clear();
            }
            // restructure next branch of contours tree
            SliceSurfaces.Branches[i + 1].Clear();
            SliceSurfaces.Branches[i + 1].AddRange(OrderedNextBranch);
        }
            // case 4: this branch has more curves than next branch
        else if (this_branch.Count > 1 && next_branch.Count > 1 && this_branch.Count > next_branch.Count)
        {
            Print("Case 4, N -> M>N (VERIFY!)");
            // create new list for storage
            List<Brep> OrderedNextBranch = new List<Brep>();
            // loop over all curves in this branch
            for (int j = 0; j < this_branch.Count; j++)
            {
            if (next_branch.Count < 1) break;
            // loop over next branch and find all centerpoints to compare against
            Point3dList ptL = new Point3dList();
            for (int k = 0; k < next_branch.Count; k++)
            {
                //Point3d cpB = AreaMassProperties.Compute(next_branch[k]).Centroid;
                Point3d cpB = GetSurfaceCentroid(next_branch[k]);
                ptL.Add(cpB);
            }
            // get curve centerpoint
            //Point3d cpA = AreaMassProperties.Compute(this_branch[j]).Centroid;
            Point3d cpA = GetSurfaceCentroid(this_branch[j]);
            // compare against point3dlist of other centerpoints
            int idx = ptL.ClosestIndex(cpA);
            // add closest item to ordered list
            OrderedNextBranch.Add((Brep) next_branch[idx].Duplicate());
            // remove closest item from candidates
            next_branch.RemoveAt(idx);
            // clear data
            ptL.Clear();
            }
            // restructure next branch of contours tree
            SliceSurfaces.Branches[i + 1].Clear();
            SliceSurfaces.Branches[i + 1].AddRange(OrderedNextBranch);
        }
            // case 5: this branch has less curves than next branch
        else if (this_branch.Count > 1 && next_branch.Count > 1 && this_branch.Count < next_branch.Count)
        {
            Print("Case 5, N -> M<N (VERIFY)");
            // loop over next branch and find all centerpoints to compare against
            Point3dList ptL = new Point3dList();
            for (int k = 0; k < next_branch.Count; k++)
            {
            //Point3d cpB = AreaMassProperties.Compute(next_branch[k]).Centroid;
            Point3d cpB = GetSurfaceCentroid(next_branch[k]);
            ptL.Add(cpB);
            }
            // create new list for storage
            List<Brep> OrderedNextBranch = new List<Brep>();
            // create duplicate list for looping
            List<Brep> next_branch_dup = new List<Brep>(next_branch);
            // loop over all curves in this branch
            for (int j = 0; j < this_branch.Count; j++)
            {
            // get curve centerpoint
            //Point3d cpA = AreaMassProperties.Compute(this_branch[j]).Centroid;
            Point3d cpA = GetSurfaceCentroid(this_branch[j]);
            // compare against point3dlist of other centerpoints
            int idx = ptL.ClosestIndex(cpA);
            // add closest curve based on comparison to new list
            OrderedNextBranch.Add((Brep) next_branch[idx].Duplicate());
            next_branch_dup.Remove(next_branch[idx]);
            }
            // add remaining curves from next branch to list
            OrderedNextBranch.AddRange(next_branch_dup);
            // clear data
            ptL.Clear();
            // restructure next branch of contours tree
            SliceSurfaces.Branches[i + 1].Clear();
            SliceSurfaces.Branches[i + 1].AddRange(OrderedNextBranch);
        }
            // case 6: this branch has multiple curves but next branch only has one
        else if (this_branch.Count > 1 && next_branch.Count == 1)
        {
            Print("Case 6, N -> 1 (nothing done)");
        }
        }

        return SliceSurfaces;
    }
}
