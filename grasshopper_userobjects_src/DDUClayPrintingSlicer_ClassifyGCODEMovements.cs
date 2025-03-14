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

    private void RunScript(
		double ProgressPercentage,
		List<Point3d> Points,
		List<double> E,
		Color ExtrusionColor,
		Color TravelColor,
		Color RetractionColor,
		ref object MoveLines,
		ref object MoveLinesProg,
		ref object Colors,
		ref object ColorsProg,
		ref object PrintLines,
		ref object PrintPolyLines)
    {
        // set component params
        this.Component.Name = "ClassifyGCODEMovements";
        this.Component.NickName = "ClassifyGCODEMovements";
        this.Component.Category = "DDUClayPrintingSlicer";
        this.Component.SubCategory = "7 GCODE";

        List<LineCurve> _MoveLines = new List<LineCurve>();
        List<LineCurve> _MoveLinesProg = new List<LineCurve>();
        List<LineCurve> _PrintLines = new List<LineCurve>();
        List<System.Drawing.Color> _Colors = new List<System.Drawing.Color>();
        List<System.Drawing.Color> _ColorsProg = new List<System.Drawing.Color>();
        List<Polyline> _PrintPolyLines = new List<Polyline>();

        Polyline pl_temp = new Polyline();

        int SplitIndex = (int) Math.Round((ProgressPercentage / 100.0) * Points.Count, 0);

        for (int i = 0; i < Points.Count - 1; i++)
        {
        // construct line curve
        LineCurve ln = new LineCurve(Points[i], Points[i + 1]);
        _MoveLines.Add(ln);

        if (i < SplitIndex) { _MoveLinesProg.Add(ln); }

        // set flags
        bool travel = E[i + 1] == 0 || E[i] == E[i + 1];
        bool retract = E[i + 1] < E[i];
        bool approach = E[i] == 0 && E[i + 1] > 0;

        // evaluate flags
        if (travel || approach)
        {
            _Colors.Add(TravelColor);
            if (i < SplitIndex) { _ColorsProg.Add(TravelColor); }

            // finish print polyline if it has at least two vertices
            if (pl_temp.Count > 1 && i <= SplitIndex)
            {
            _PrintPolyLines.Add(pl_temp.Duplicate());
            pl_temp = new Polyline();
            }
        }
        else if (retract)
        {
            _Colors.Add(RetractionColor);
            if (i < SplitIndex) { _ColorsProg.Add(RetractionColor); }

            // finish print polyline if it has at least two vertices
            if (pl_temp.Count > 1 && i <= SplitIndex)
            {
            _PrintPolyLines.Add(pl_temp.Duplicate());
            pl_temp = new Polyline();
            }
        }
        else
        {
            _Colors.Add(ExtrusionColor);
            if (i < SplitIndex) { _ColorsProg.Add(ExtrusionColor); }

            _PrintLines.Add(ln);

            // add points to print polyline
            if (i < SplitIndex)
            {
            if (pl_temp.Count == 0)
            {
                // add A point if this is the start of the PPL
                pl_temp.Add(Points[i]);
            }
            // always add B point
            pl_temp.Add(Points[i + 1]);
            }
        }

        // catch unfinished PPL and finalize it
        if (pl_temp.Count > 0 && i == Points.Count - 2)
        {
            _PrintPolyLines.Add(pl_temp.Duplicate());
            pl_temp = new Polyline();
        }

        MoveLines = _MoveLines;
        MoveLinesProg = _MoveLinesProg;
        Colors = _Colors;
        ColorsProg = _ColorsProg;
        PrintLines = _PrintLines;
        PrintPolyLines = _PrintPolyLines;
        }
    }

}
