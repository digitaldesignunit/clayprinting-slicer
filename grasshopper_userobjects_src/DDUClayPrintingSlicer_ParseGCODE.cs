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
    Version: 250314
    */
    #endregion

    private void RunScript(
		List<string> GCODE,
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

        List<string> _G = new List<string>();
        List<double> _X = new List<double>();
        List<double> _Y = new List<double>();
        List<double> _Z = new List<double>();
        List<double> _E = new List<double>();
        List<double> _F = new List<double>();

        double Ztemp = 0.0;
        double Etemp = 0.0;
        double Ftemp = 0.0;

        Regex re_colon_a = new Regex(" ;");
        Regex re_colon_b = new Regex(" |;");

        var cult = System.Globalization.CultureInfo.InvariantCulture;

        for (int i = 0; i < GCODE.Count; i++)
        {
        string ln = GCODE[i];

        if (ln.StartsWith(";"))
        {
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
            Print(ln);
        }
        }

        G = _G;
        X = _X;
        Y = _Y;
        Z = _Z;
        E = _E;
        F = _F;
    }
}
