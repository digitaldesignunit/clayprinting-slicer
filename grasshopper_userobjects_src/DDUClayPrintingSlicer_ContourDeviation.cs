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
		DataTree<Curve> Contours,
		ref object MinDist,
		ref object MaxDist,
		ref object MinA,
		ref object MaxA,
		ref object MinB,
		ref object MaxB)
  {
    // set component params
    this.Component.Name = "ContourDeviation";
    this.Component.NickName = "ContourDeviation";
    this.Component.Category = "DDUClayPrintingSlicer";
    this.Component.SubCategory = "6 Analysis";

    DataTree<object> minDistances = new DataTree<object>();
    DataTree<object> maxDistances = new DataTree<object>();
    DataTree<object> minA = new DataTree<object>();
    DataTree<object> maxA = new DataTree<object>();
    DataTree<object> minB = new DataTree<object>();
    DataTree<object> maxB = new DataTree<object>();

    if (Contours != null)
    {
      for (int i = 0; i < Contours.BranchCount - 1; i++)
      {
        // measure distance from one curve to the next branch
        // branch can have one or many curves
        var this_branch = Contours.Branches[i];
        var next_branch = Contours.Branches[i + 1];

        // create path for output
        GH_Path ghp = new GH_Path(i);

        // case 1: both branches have one contour
        if (this_branch.Count == 1 && next_branch.Count == 1)
        {
          // get results
          List<object> results = GetMinMaxDistContours(this_branch[0], next_branch[0]);

          // add to output trees
          minDistances.Add((double) results[0], ghp);
          maxDistances.Add((double) results[1], ghp);
          minA.Add((Point3d) results[2], ghp);
          minB.Add((Point3d) results[3], ghp);
          maxA.Add((Point3d) results[4], ghp);
          maxB.Add((Point3d) results[5], ghp);
        }
          // case 2: both branches have multiple, but identical number of contours
        else if (this_branch.Count > 1 && this_branch.Count == next_branch.Count)
        {
          // compute min, max distance for every pair of contours

          //loop through branch pairs
          for (int j = 0; j < this_branch.Count; j++)
          {
            // get results
            List<object> results = GetMinMaxDistContours(this_branch[j], next_branch[j]);

            // add to output trees
            minDistances.Add((double) results[0], ghp);
            maxDistances.Add((double) results[1], ghp);
            minA.Add((Point3d) results[2], ghp);
            minB.Add((Point3d) results[3], ghp);
            maxA.Add((Point3d) results[4], ghp);
            maxB.Add((Point3d) results[5], ghp);
          }
        }
        // case 3: this branch has one contour, next branch has multiple contours
        // --> generate points on this branch's contour, pull cps for every other contour

        // case 4: this branch has multiple contours, next branch has one contour
        // --> generate points on next branch, pull cps for every contour in this branch

        // case 5: both branches have multiple contours but each has different number of contours
      }
      MinDist = minDistances;
      MaxDist = maxDistances;
      MinA = minA;
      MinB = minB;
      MaxA = maxA;
      MaxB = maxB;
    }
  }

  // <Custom additional code> 
  public List<object> GetMinMaxDistContours(Curve contourA, Curve contourB)
  {
    // init vars
    double minDist;
    double maxDist;

    Point3d minPtA = Point3d.Unset;
    Point3d minPtB = Point3d.Unset;

    Point3d maxPtA = Point3d.Unset;
    Point3d maxPtB = Point3d.Unset;

    // compute min, max distance
    double len = Math.Max(contourA.GetLength(), contourB.GetLength());
    int res = (int) Math.Round(len / 1.0, 0);

    Point3d[] ptA;
    contourA.DivideByCount(res, true, out ptA);

    minDist = double.MaxValue;
    maxDist = double.MinValue;

    for (int j = 0; j < ptA.Length; j++)
    {
      double tB;
      contourB.ClosestPoint(ptA[j], out tB);
      Point3d ptB = contourB.PointAt(tB);

      double dist = ptB.Z - ptA[j].Z;

      if (dist < minDist)
      {
        minDist = dist;
        minPtA = new Point3d(ptA[j]);
        minPtB = new Point3d(ptB);
      }

      if (dist > maxDist)
      {
        maxDist = dist;
        maxPtA = new Point3d(ptA[j]);
        maxPtB = new Point3d(ptB);
      }
    }
    return new List<object>{minDist, maxDist, minPtA, minPtB, maxPtA, maxPtB};
  }
  // </Custom additional code> 
}
