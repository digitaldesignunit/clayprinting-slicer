// Grasshopper Script Instance
#region Usings
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Rhino;
using Rhino.Geometry;

using System.Threading.Tasks;
using System.Collections.Concurrent;
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

    Original Script by Daniel Piker 03/07/2020
    Modified by Max Benjamin Eschenbach May 2024
    Version: 250314
    */
    #endregion

    private void RunScript(
		Mesh InMesh,
		int Count,
		ref object Contours,
		ref object DebuggingOut)
    {
    // set component params
    this.Component.Name = "IsoContourMeshCount";
    this.Component.NickName = "IsoContourMeshCount";
    this.Component.Category = "DDUClayPrintingSlicer";
    this.Component.SubCategory = "1 Slicing";

    // sanitize data input
    if (InMesh == null)
    {
      this.Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
        "Input Parameter InMesh failed to collect data!");
      Contours = new Grasshopper.DataTree<PolylineCurve>();
    }
    // only do something if mesh is present
    if (InMesh != null && Count > 0)
    {
      // Contours = OrderSliceCurves(CreateContours(InMesh, Count));
      var UnorderedContours = CreateContours(InMesh, Count);
      Contours = OrderSliceCurves(UnorderedContours);
    }
    else
    {
      this.Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input Parameters failed to collect data!");
    }
    }

  public List<Curve> FindBoundaries(Mesh M)
  {
    // get naked mesh edges
    Polyline[] NakedEdges = M.GetNakedEdges();

    // join all the resulting naked edges
    Curve[] Boundaries = Curve.JoinCurves(NakedEdges.Select(pl => pl.ToPolylineCurve()));

    return Boundaries.ToList();
  }

  public DataTree<PolylineCurve> CreateContours(Mesh M, int Count)
  {
    // subtract from the count to make count match the outcome
    Count -= 2;

    // init concurrent bag for storing contours
    List<PolylineCurve>[] contours = new List<PolylineCurve>[Count];

    // extract gradient from texture coordinates
    double[] values = new double[M.Vertices.Count];
    for(int i = 0; i < M.Vertices.Count; i++) values[i] = M.TextureCoordinates[i].Y;

    // get bounds of gradient value domain
    double target_min = values.Min();
    double target_max = values.Max();

    // compute stepsize for iso values
    // increment count to avoid catching boundary extremes
    // (they will be found later)
    double stepsize = (target_max - target_min) / (Count + 1.0);

    // create contours in parallel
    Parallel.For(0, Count,
      j => {
      // compute current iso value
      double iso = target_min + ((j + 1) * stepsize);

      // init list for geometry storage
      List<Curve> curves = new List<Curve>();

      // loop over all faces
      for(int i = 0; i < M.Faces.Count; i++)
      {
        int va = M.Faces[i].A;
        int vb = M.Faces[i].B;
        int vc = M.Faces[i].C;

        double a = M.TextureCoordinates[va].Y - iso;
        double b = M.TextureCoordinates[vb].Y - iso;
        double c = M.TextureCoordinates[vc].Y - iso;

        Point3d pa = M.Vertices.Point3dAt(va);
        Point3d pb = M.Vertices.Point3dAt(vb);
        Point3d pc = M.Vertices.Point3dAt(vc);

        List<Point3d> ends = new List<Point3d>();

        // find line segment
        if(a < 0 != b < 0)
        {
          double wa = b / (b - a);
          double wb = 1 - wa;
          ends.Add(wa * pa + wb * pb);
        }
        if(a < 0 != c < 0)
        {
          double wa = c / (c - a);
          double wc = 1 - wa;
          ends.Add(wa * pa + wc * pc);
        }
        if(c < 0 != b < 0)
        {
          double wb = c / (c - b);
          double wc = 1 - wb;
          ends.Add(wb * pb + wc * pc);
        }

        // if line segment is complete add it to list
        if(ends.Count == 2)
        {
          curves.Add(new LineCurve(ends[0], ends[1]));
        }
      }

      // join contour lines
      Curve[] jcrvs = Curve.JoinCurves(curves);

      // init list for storing closed curves
      List<PolylineCurve> clsdcrvs = new List<PolylineCurve>();

      // only add closed curves
      for (int i = 0; i < jcrvs.Length; i++)
      {
        if (jcrvs[i].IsClosed)
        {
          clsdcrvs.Add(jcrvs[i] as PolylineCurve);
        }
      }
      // add list of closed curves to array
      contours[j] = clsdcrvs;

      // end parallel for
      });

    // convert array results to datatree for output
    DataTree<PolylineCurve> ContourTree = new DataTree<PolylineCurve>();
    for (int i = 0; i < contours.Length; i++)
    {
      // create path in datatre
      // increment i because of intentionally missing extremes
      GH_Path path = new GH_Path(0, i + 1);
      ContourTree.AddRange(contours[i], path);
    }

    // find extreme boundaries seperately
    // this is necessary because of stability issues
    // with the iso routine near the boundaries
    List<Curve> Boundaries = FindBoundaries(M);

    // loop through boundaries and for every boundary
    // find the corresponding tree path
    for (int i = 0; i < Boundaries.Count; i++)
    {
      // init vars for closest branch and minimum distance
      int closestBranch = 0;
      double minDist = double.MaxValue;
      // get closest point on every contour curve
      for (int j = 1; j < ContourTree.BranchCount; j++)
      {
        Point3d onCrv;
        Point3d onObj;
        int wG;
        // get cp pair for every branch
        bool ret = Boundaries[i].ClosestPoints(ContourTree.Branches[j], out onCrv, out onObj, out wG);
        // measure distance
        double dist = onCrv.DistanceTo(onObj);
        // set minimum distance
        if (dist < minDist)
        {
          minDist = dist;
          closestBranch = j;
        }
      }
      // correct closest branch
      if (closestBranch == 1)
      {
        closestBranch = 0;
      }
      else if (closestBranch == ContourTree.BranchCount - 1)
      {
        closestBranch = Count + 1;
      }
      else
      {
        continue;
      }

      //Print(closestBranch.ToString());

      // insert into contour tree
      GH_Path ghp = new GH_Path(0, closestBranch);
      ContourTree.EnsurePath(ghp);
      ContourTree.Add(Boundaries[i] as PolylineCurve, ghp);
    }

    // return tree with closed contour curves
    return ContourTree;
  }

  public DataTree<PolylineCurve> OrderSliceCurves(DataTree<PolylineCurve> ContourCurves)
  {
    // create copy of datatree to avoid messing up things
    DataTree<PolylineCurve> SliceCurves = new DataTree<PolylineCurve>(ContourCurves);

    // loop over datatree and ensure correct order
    for (int i = 0; i < SliceCurves.BranchCount - 1; i++)
    {
      // get branches
      var this_branch = SliceCurves.Branches[i];
      var next_branch = SliceCurves.Branches[i + 1];

      // case 1: this branch one curve and next branch one curve
      if (this_branch.Count == 1 && next_branch.Count == 1)
      {
        //Print("Case 1, 1 -> 1 (nothing done)");
        // nothing needs to be done
      }
        // case 2: this branch one curve and next branch multiple curves
      else if (this_branch.Count == 1 && next_branch.Count > 1)
      {
        //Print("Case 2, 1 -> N");
        // loop over next branch and find all centerpoints to compare against

        Point3dList ptL = new Point3dList();
        for (int k = 0; k < next_branch.Count; k++)
        {
          Point3d cpB = next_branch[k].ToPolyline().CenterPoint();
          ptL.Add(cpB);
        }
        // create new list for storage
        List<PolylineCurve> OrderedNextBranch = new List<PolylineCurve>();
        // get curve centerpoint
        Point3d cpA = this_branch[0].ToPolyline().CenterPoint();
        // compare against point3dlist of other centerpoints
        int idx = ptL.ClosestIndex(cpA);
        OrderedNextBranch.Add(next_branch[idx].Duplicate() as PolylineCurve);
        next_branch.RemoveAt(idx);
        OrderedNextBranch.AddRange(next_branch);
        // clear data
        ptL.Clear();
        // restructure next branch of contours tree
        SliceCurves.Branches[i + 1].Clear();
        SliceCurves.Branches[i + 1].AddRange(OrderedNextBranch);
      }
        // case 3: this branch and next branch have multiple curves but the same count
      else if (this_branch.Count > 1 && this_branch.Count == next_branch.Count)
      {
        //Print("Case 3, N -> N");
        // create new list for storage
        List<PolylineCurve> OrderedNextBranch = new List<PolylineCurve>();
        // loop over all curves in this branch
        for (int j = 0; j < this_branch.Count; j++)
        {
          if (next_branch.Count < 1) break;
          // loop over next branch and find all centerpoints to compare against
          Point3dList ptL = new Point3dList();
          for (int k = 0; k < next_branch.Count; k++)
          {
            Point3d cpB = next_branch[k].ToPolyline().CenterPoint();
            ptL.Add(cpB);
          }
          // get curve centerpoint
          Point3d cpA = this_branch[j].ToPolyline().CenterPoint();
          // compare against point3dlist of other centerpoints
          int idx = ptL.ClosestIndex(cpA);
          // add closest item to ordered list
          OrderedNextBranch.Add(next_branch[idx].Duplicate() as PolylineCurve);
          // remove closest item from candidates
          next_branch.RemoveAt(idx);
          // clear data
          ptL.Clear();
        }
        // restructure next branch of contours tree
        SliceCurves.Branches[i + 1].Clear();
        SliceCurves.Branches[i + 1].AddRange(OrderedNextBranch);
      }
        // case 4: this branch has more curves than next branch
      else if (this_branch.Count > 1 && next_branch.Count > 1 && this_branch.Count > next_branch.Count)
      {
        //Print("Case 4, N -> M>N (VERIFY!)");
        // create new list for storage
        List<PolylineCurve> OrderedNextBranch = new List<PolylineCurve>();
        // loop over all curves in this branch
        for (int j = 0; j < this_branch.Count; j++)
        {
          if (next_branch.Count < 1) break;
          // loop over next branch and find all centerpoints to compare against
          Point3dList ptL = new Point3dList();
          for (int k = 0; k < next_branch.Count; k++)
          {
            
            // Point3d cpB = AreaMassProperties.Compute(next_branch[k]).Centroid;
            Point3d cpB = next_branch[k].ToPolyline().CenterPoint();
            ptL.Add(cpB);
          }
          // get curve centerpoint
          //  Point3d cpA = AreaMassProperties.Compute(this_branch[j]).Centroid;
          Point3d cpA = this_branch[j].ToPolyline().CenterPoint();
          // compare against point3dlist of other centerpoints
          int idx = ptL.ClosestIndex(cpA);
          // add closest item to ordered list
          OrderedNextBranch.Add(next_branch[idx].Duplicate() as PolylineCurve);
          // remove closest item from candidates
          next_branch.RemoveAt(idx);
          // clear data
          ptL.Clear();
        }
        // restructure next branch of contours tree
        SliceCurves.Branches[i + 1].Clear();
        SliceCurves.Branches[i + 1].AddRange(OrderedNextBranch);
      }
        // case 5: this branch has less curves than next branch
      else if (this_branch.Count > 1 && next_branch.Count > 1 && this_branch.Count < next_branch.Count)
      {
        //Print("Case 5, N -> M<N (VERIFY)");
        // loop over next branch and find all centerpoints to compare against
        Point3dList ptL = new Point3dList();
        for (int k = 0; k < next_branch.Count; k++)
        {
          Point3d cpB = next_branch[k].ToPolyline().CenterPoint();
          ptL.Add(cpB);
        }
        // create new list for storage
        List<PolylineCurve> OrderedNextBranch = new List<PolylineCurve>();
        // create duplicate list for looping
        List<PolylineCurve> next_branch_dup = new List<PolylineCurve>(next_branch);
        // loop over all curves in this branch
        for (int j = 0; j < this_branch.Count; j++)
        {
          // get curve centerpoint
          Point3d cpA = this_branch[j].ToPolyline().CenterPoint();
          // compare against point3dlist of other centerpoints
          int idx = ptL.ClosestIndex(cpA);
          // add closest curve based on comparison to new list
          OrderedNextBranch.Add(next_branch[idx].Duplicate() as PolylineCurve);
          next_branch_dup.Remove(next_branch[idx]);
        }
        // add remaining curves from next branch to list
        OrderedNextBranch.AddRange(next_branch_dup);
        // clear data
        ptL.Clear();
        // restructure next branch of contours tree
        SliceCurves.Branches[i + 1].Clear();
        SliceCurves.Branches[i + 1].AddRange(OrderedNextBranch);
      }
        // case 6: this branch has multiple curves but next branch only has one
      else if (this_branch.Count > 1 && next_branch.Count == 1)
      {
        //Print("Case 6, N -> 1 (nothing done)");
      }
    }

    return SliceCurves;
  }

}
