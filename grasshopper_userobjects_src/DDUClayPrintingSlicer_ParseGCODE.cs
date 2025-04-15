// Grasshopper Script Instance
#region Usings
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;

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
    Version: 2504152
    */
    #endregion

    private void RunScript(
		List<string> GCODE,
		ref object PrintSpeed,
		ref object TravelSpeed,
		ref object RetractionSpeed,
		ref object ExtrusionRate,
		ref object LayerHeight,
		ref object LineWidth,
		ref object G,
		ref object X,
		ref object Y,
		ref object Z,
		ref object E,
		ref object F)
    {
        // set component params
        this.Component.Name = "ParseGCODE";
        this.Component.NickName = "ParseGCODE";
        this.Component.Category = "DDUClayPrintingSlicer";
        this.Component.SubCategory = "7 GCODE";

        DataTree<double> _PrintSpeed = new DataTree<double>();
        DataTree<double> _TravelSpeed = new DataTree<double>();
        DataTree<double> _RetractionSpeed = new DataTree<double>();
        DataTree<double> _ExtrusionRate = new DataTree<double>();
        DataTree<double> _LayerHeight = new DataTree<double>();
        DataTree<double> _LineWidth = new DataTree<double>();

        List<string> _G = new List<string>();
        List<double> _X = new List<double>();
        List<double> _Y = new List<double>();
        List<double> _Z = new List<double>();
        List<double> _E = new List<double>();
        List<double> _F = new List<double>();

        if (GCODE == null)
        {
            this.AddRuntimeMessage(
                GH_RuntimeMessageLevel.Warning,
                "Input Parameter GCODE failed to collect Data!"
            );
            PrintSpeed = _PrintSpeed;
            TravelSpeed = _TravelSpeed;
            RetractionSpeed = _RetractionSpeed;
            ExtrusionRate = _ExtrusionRate;
            LayerHeight = _LayerHeight;
            LineWidth = _LineWidth;
            G = _G;
            X = _X;
            Y = _Y;
            Z = _Z;
            E = _E;
            F = _F;
            return;
        }

        // Temporary Variables
        double Ztemp = 0.0;
        double Etemp = 0.0;
        double Ftemp = 0.0;

        // Regex Definitions
        Regex re_colon_a = new Regex(" ;");
        Regex re_colon_b = new Regex(" |;");
        Regex re_colon_c = new Regex("=");

        // Define Invariant Culture
        var cult = System.Globalization.CultureInfo.InvariantCulture;

        for (int i = 0; i < GCODE.Count; i++)
        {
            // Define the current GCODE line
            string ln = GCODE[i];
            if (ln.StartsWith(";"))
            {
                // Detect Header
                if (ln.Contains("BEGIN_DDU_3DCLAYPRINTING_HEADER"))
                {
                    // READ HEADER INFORMATION, HEADER LOOKS LIKE THIS:
                    // ; --- BEGIN_DDU_3DCLAYPRINTING_HEADER ---
                    // ; PRINTSPEED=3000
                    // ; TRAVELSPEED=6000
                    // ; RETRACTIONSPEED=1500
                    // ; EXTRUSIONRATE=0.16
                    // ; LAYERHEIGHT=1.9
                    // ; LINEWIDTH=4
                    // ; --- END_DDU_3DCLAYPRINTING_HEADER ---
                    
                    // Process using RegEx patterns
                    _PrintSpeed.Add(double.Parse(re_colon_c.Split(re_colon_b.Split(re_colon_a.Split(GCODE[i + 1])[0])[2])[1], cult));
                    _TravelSpeed.Add(double.Parse(re_colon_c.Split(re_colon_b.Split(re_colon_a.Split(GCODE[i + 2])[0])[2])[1], cult));
                    _RetractionSpeed.Add(double.Parse(re_colon_c.Split(re_colon_b.Split(re_colon_a.Split(GCODE[i + 3])[0])[2])[1], cult));
                    _ExtrusionRate.Add(double.Parse(re_colon_c.Split(re_colon_b.Split(re_colon_a.Split(GCODE[i + 4])[0])[2])[1], cult));
                    _LayerHeight.Add(double.Parse(re_colon_c.Split(re_colon_b.Split(re_colon_a.Split(GCODE[i + 5])[0])[2])[1], cult));
                    _LineWidth.Add(double.Parse(re_colon_c.Split(re_colon_b.Split(re_colon_a.Split(GCODE[i + 6])[0])[2])[1], cult));
                }
                continue;
            }
            else if (ln.StartsWith("G0") || ln.StartsWith("G1"))
            {
                if (ln.Contains("X") && ln.Contains("Y"))
                {
                    _G.Add(ln.Substring(1, 1));
                }
                else
                {
                    continue;
                }
                // Split Line into parts using RegEx template
                string[] preparts = re_colon_a.Split(ln);
                string[] parts = re_colon_b.Split(preparts[0]);
                // loop over all string parts
                for (int j = 0; j < parts.Length; j++)
                {
                    string part = parts[j];

                    // catch X values
                    if (part.StartsWith("X")) _X.Add(double.Parse(part.Substring(1), cult));

                    // catch Y values
                    if (part.StartsWith("Y")) _Y.Add(double.Parse(part.Substring(1), cult));

                    // catch Z values
                    if (part.StartsWith("Z"))
                    {
                        double Zval = double.Parse(part.Substring(1), cult);
                        _Z.Add(Zval);
                        Ztemp = Zval;
                    }

                    // catch E values
                    if (part.StartsWith("E"))
                    {
                        double Eval = double.Parse(part.Substring(1), cult);
                        _E.Add(Eval);
                        Etemp = Eval;
                    }

                    // catch F values
                    if (part.StartsWith("F"))
                    {
                        double Fval = double.Parse(part.Substring(1), cult);
                        _F.Add(Fval);
                        Ftemp = Fval;
                    }
                }
                // catch missing Z values
                if (ln.Contains("X") && ln.Contains("Y") && !ln.Contains("Z")) _Z.Add(Ztemp);

                // catch missing E values
                if (!ln.Contains("E")) _E.Add(Etemp);

                // catch missing F values
                if (!ln.Contains("F")) _F.Add(Ftemp);
            }
            else
            {
                // Print(ln);
            }
        }
        PrintSpeed = _PrintSpeed;
        TravelSpeed = _TravelSpeed;
        RetractionSpeed = _RetractionSpeed;
        ExtrusionRate = _ExtrusionRate;
        LayerHeight = _LayerHeight;
        LineWidth = _LineWidth;
        G = _G;
        X = _X;
        Y = _Y;
        Z = _Z;
        E = _E;
        F = _F;
    }
}
