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
		DataTree<Curve> OuterCurves,
		DataTree<Curve> InfillCurves,
		ref object ConnectedCurves)
    {
        // set component params
        this.Component.Name = "ConnectInfillCurves_WIP";
        this.Component.NickName = "ConnectInfillCurves_WIP";
        this.Component.Category = "DDUClayPrintingSlicer";
        this.Component.SubCategory = "3 Infill";

        List<Curve> infill;
        Curve bounds;
        Curve inline_A;
        Curve inline_B;
        Curve subcrv;
        Curve[] splitcurves;
        double tS;
        double tA;
        double tB;

        List<Curve> joinedCurves = new List<Curve>();

        DataTree<Curve> OutputCurves = new DataTree<Curve>();

        // LOOP OVER INFILLCURVES
        for (int i = 0; i < InfillCurves.BranchCount; i++)
        {
            infill = InfillCurves.Branches[i];
            bounds = OuterCurves.Branches[i][0];
            // Loop over infillcurves
            for (int j = 0; j < infill.Count - 1; j++)
            {
                inline_A = infill[j];
                inline_B = infill[j + 1];
                if (j == 0)
                {
                    // Get CP of first InfillCurve START-Point
                    bounds.ClosestPoint(inline_A.PointAtStart, out tS);
                    bounds.ChangeClosedCurveSeam(tS);
                    bounds.Domain = new Rhino.Geometry.Interval(0.0, 1.0);
                }

                // get subcurve from THIS endpoint to NEXT startpoint
                // build consecutive curve from that (in script / out script?)
                bounds.ClosestPoint(inline_A.PointAtEnd, out tA);
                bounds.ClosestPoint(inline_B.PointAtStart, out tB);
                if (tA > tB)
                {
                    splitcurves = bounds.Split(new double[2]{tB, tA});
                    Array.Sort(splitcurves, new CurveComparer());
                    subcrv = splitcurves[0];
                    subcrv.Reverse();
                }
                else
                {
                    splitcurves = bounds.Split(new double[2]{tA, tB});
                    Array.Sort(splitcurves, new CurveComparer());
                    subcrv = splitcurves[0];
                }

                // join inline A with subcrv and continue
                if (j == 0)
                {
                    joinedCurves = Curve.JoinCurves(new Curve[2]{inline_A, subcrv}).ToList();
                }
                else
                {
                    if (joinedCurves != null)
                    {
                        if (j == infill.Count - 2)
                        {
                            joinedCurves = Curve.JoinCurves(new Curve[4]{joinedCurves[0], inline_A, subcrv, inline_B}).ToList();
                        }
                        else
                        {
                            joinedCurves = Curve.JoinCurves(new Curve[3]{joinedCurves[0], inline_A, subcrv}).ToList();
                        }
                    }
                }
            }
            OutputCurves.AddRange(joinedCurves, InfillCurves.Paths[i]);
        }
        ConnectedCurves = OutputCurves;
    }
    
}

public class CurveComparer : IComparer<Curve>
    {
        // Call CaseInsensitiveComparer.Compare with the parameters reversed.
        public int Compare(Curve x, Curve y)
        {
            double a = x.GetLength();
            double b = y.GetLength();
            if (a - b < 0) { return -1; }
            else if (a - b > 0) { return 1; }
            else { return 0; }
        }
    }
