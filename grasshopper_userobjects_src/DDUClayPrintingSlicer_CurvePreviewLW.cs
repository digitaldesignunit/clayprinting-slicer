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

    List<Curve> drawCurves;
    List<Color> drawColors;
    List<int> drawThickness;

    private void RunScript(
		List<Curve> Curves,
		List<Color> Colors,
		List<int> Thickness)
    {
        // set component params
        this.Component.Name = "CurvePreviewLW";
        this.Component.NickName = "CurvePreviewLW";
        this.Component.Category = "DDUClayPrintingSlicer";
        this.Component.SubCategory = "8 Visualisation";

        this.drawCurves = new List<Curve>();   
        this.drawColors = new List<Color>();
        this.drawThickness = new List<int>();

        if (Curves == null) return;
        if (Colors == null) this.drawColors = new List<Color>{Color.Black};
        else this.drawColors = Colors;

        if (Thickness == null) this.drawThickness = new List<int>{1};
        else this.drawThickness = Thickness;

        for (int i = 0; i < Curves.Count; i++)
        {
            if (Curves[i] != null)
            {
                this.drawCurves.Add(Curves[i]);
            }
            else
            {
                continue;
            }
            if (Colors == null || i >= Colors.Count || Colors[i] == null)
            {
                this.drawColors.Add(drawColors.Last());
            }
            else
            {
                this.drawColors.Add(Colors[i]);
            }
            if (Thickness == null || i >= Thickness.Count || Thickness[i] == null)
            {
                this.drawThickness.Add(drawThickness.Last());
            }
            else
            {
                this.drawThickness.Add(Thickness[i]);
            }
        }
    }

    override public void DrawViewportWires(IGH_PreviewArgs args)
    {
        if (this.drawCurves != null)
        {
            try
            {
                for (int i = 0; i < this.drawCurves.Count; i++)
                {
                    args.Display.DrawCurve(
                        this.drawCurves[i],
                        this.drawColors[i],
                        this.drawThickness[i]
                    );
                }
                
            }
            catch (Exception e)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Error while drawing Preview!");
                this.Print(e.ToString());
            }
        }
        else
        {
            return;
        }
    }
}
