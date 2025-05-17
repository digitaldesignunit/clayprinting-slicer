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
    Version: 250517
    */
    #endregion

    private void RunScript(
		DataTree<Curve> Curves,
		double SeamAdjust,
		bool Randomize,
		int Seed,
		ref object OutCurves)
    {
        // set component params
        this.Component.Name = "AdjustSliceSeams";
        this.Component.NickName = "AdjustSliceSeams";
        this.Component.Category = "DDUClayPrintingSlicer";
        this.Component.SubCategory = "2 Slice Processing";

        if (Curves != null && Curves.DataCount > 0)
        {
            // reparametrize domain of first input curve
            Curves.Branches[0][0].Domain = new Rhino.Geometry.Interval(0.0, 1.0);
            // sanitize negative seam adjust values
            if (SeamAdjust < 0) SeamAdjust = 1.0 + SeamAdjust;
            // get normalized length parameter based on adjust value
            double CrvLP;
            Curves.Branches[0][0].NormalizedLengthParameter(SeamAdjust, out CrvLP);
            // use adjust value for first seam adjustment
            Curves.Branches[0][0].ChangeClosedCurveSeam(CrvLP);
            // get initial sample point after seam adjustment (is now point at start)
            Point3d samplePt = Curves.Branches[0][0].PointAtStart;
            // create random instance with seed for randomization
            Random RandSeam = new Random(Seed);

            // loop over all branches of curves
            for (int i = 0; i < Curves.BranchCount; i++)
            {
                for (int j = 0; j < Curves.Branches[i].Count; j++)
                {
                    // skip the first curve in the first branch
                    // because it is already preprocessed before the loop
                    if (i == 0 && j == 0) continue;
                    // skip open curves
                    if (!Curves.Branches[i][j].IsClosed) continue;
                    // init parameter var
                    double t;
                    // reparametrize domain
                    Curves.Branches[i][j].Domain = new Rhino.Geometry.Interval(0.0, 1.0);
                    if (Randomize)
                    {
                        Curves.Branches[i][j].ChangeClosedCurveSeam(RandSeam.NextDouble());
                    }
                    else
                    {
                        // get cp based on sample point
                        Curves.Branches[i][j].ClosestPoint(samplePt, out t);
                        // set seam
                        Curves.Branches[i][j].ChangeClosedCurveSeam(t);
                    }
                    // reparam again
                    Curves.Branches[i][j].Domain = new Rhino.Geometry.Interval(0.0, 1.0);
                }
                // set sample point for next branch
                samplePt = Curves.Branches[i][0].PointAtStart;
            }
        }
        else
        {
            Curves = new DataTree<Curve>();
        }
        OutCurves = Curves;
    }
}
