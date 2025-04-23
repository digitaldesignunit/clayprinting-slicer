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
    
    Author: Max Eschenbach
    License: MIT License
    Version: 250423
    */
    #endregion

    List<string> INFO;
    List<string> LHA;
    List<string> OHA;
    List<string> OHO;

    int hmargin;
    int vmargin;
    string font;
    int txtsize;
    Color txtcolor;
    Color errcolor;
    Color warncolor;
    Color ignocolor;
    bool throwerror;

    private void RunScript(
		List<string> INFO,
		List<string> LHA,
		List<string> OHA,
		List<string> OHO,
		int MarginH,
		int MarginV,
		string Font,
		int TextSize,
		Color TextColor,
		Color ErrorColor,
		Color WarningColor,
		Color IgnoColor,
		bool ThrowDebugError)
    {
        // set component params
        this.Component.Name = "ClayPrintingHUD";
        this.Component.NickName = "ClayPrintingHUD";
        this.Component.Category = "DDUClayPrintingSlicer";
        this.Component.SubCategory = "8 Visualisation";

        // data
        this.INFO = INFO;
        this.LHA = LHA;
        this.OHA = OHA;
        this.OHO = OHO;
        
        // settings
        this.hmargin = MarginH;
        this.vmargin = MarginV;
        this.font = Font;
        this.txtsize = TextSize;
        this.txtcolor = TextColor;
        this.errcolor = ErrorColor;
        this.warncolor = WarningColor;
        this.ignocolor = IgnoColor;
        this.throwerror = ThrowDebugError;

    }

    override public void DrawViewportWires(IGH_PreviewArgs args)
    {
        if (args.Display.Viewport.Id == args.Document.RhinoDocument.Views.ActiveView.ActiveViewportID)
        {
            Point2d anchor_pt = new Point2d(this.hmargin, this.vmargin);
            

            // INFO, WARNING & ERROR STREAM
            if (this.INFO != null)
            {
                // Headline
                args.Display.Draw2dText(
                    "INFO & ERROR STREAM",
                    this.txtcolor,
                    anchor_pt,
                    false,
                    this.txtsize,
                    this.font + " Bold"
                );
                anchor_pt += new Point2d(0, this.txtsize * 1.25);
                // Stream
                foreach (string infostring in this.INFO)
                {
                    args.Display.Draw2dText(
                        infostring,
                        this.GetTextColor(infostring),
                        anchor_pt,
                        false,
                        this.txtsize,
                        this.font
                    );
                    anchor_pt += new Point2d(0, this.txtsize * 1.25);
                }


            }

            // LAYER HEIGHT ANALYSIS
            if (this.LHA != null)
            {
                anchor_pt += new Point2d(0, this.txtsize * 1.5);
                // Headline
                args.Display.Draw2dText(
                    "LAYER HEIGHT ANALYSIS",
                    this.txtcolor,
                    anchor_pt,
                    false,
                    this.txtsize,
                    this.font + " Bold"
                );
                anchor_pt += new Point2d(0, this.txtsize * 1.25);
                // Stream
                foreach (string lhastring in this.LHA)
                {
                    args.Display.Draw2dText(
                        lhastring,
                        this.txtcolor,
                        anchor_pt,
                        false,
                        this.txtsize,
                        this.font
                    );
                    anchor_pt += new Point2d(0, this.txtsize * 1.25);
                }
            }

            // OVERHANG ANGLE ANALYSIS
            if (this.OHA != null)
            {
                if (this.INFO != null && this.LHA != null) { anchor_pt += new Point2d(0, this.txtsize * 4.5); }
                else if (this.INFO != null && this.LHA == null) { anchor_pt += new Point2d(0, this.txtsize * 1.5); }
                // Headline
                args.Display.Draw2dText(
                    "OVERHANG ANGLE ANALYSIS",
                    this.txtcolor,
                    anchor_pt,
                    false,
                    this.txtsize,
                    this.font + " Bold"
                );
                anchor_pt += new Point2d(0, this.txtsize * 1.25);
                // Stream
                foreach (string ohastring in this.OHA)
                {
                    args.Display.Draw2dText(
                        ohastring,
                        this.txtcolor,
                        anchor_pt,
                        false,
                        this.txtsize,
                        this.font
                    );
                    anchor_pt += new Point2d(0, this.txtsize * 1.25);
                }
            }

            // OVERHANG OFFSET ANALYSIS
            if (this.OHO != null)
            {
                if (this.INFO != null && this.LHA != null) { anchor_pt += new Point2d(0, this.txtsize * 4.5); }
                else if (this.INFO != null && this.LHA == null) { anchor_pt += new Point2d(0, this.txtsize * 1.5); }
                // Headline
                args.Display.Draw2dText(
                    "OVERHANG OFFSET ANALYSIS",
                    this.txtcolor,
                    anchor_pt,
                    false,
                    this.txtsize,
                    this.font + " Bold"
                );
                anchor_pt += new Point2d(0, this.txtsize * 1.25);
                // Stream
                foreach (string ohostring in this.OHO)
                {
                    args.Display.Draw2dText(
                        ohostring,
                        this.txtcolor,
                        anchor_pt,
                        false,
                        this.txtsize,
                        this.font
                    );
                    anchor_pt += new Point2d(0, this.txtsize * 1.25);
                }
            }

        }
    }

    private Color GetTextColor(string infostring)
    {
        Color col = Color.Black;
        // options
        if (infostring.StartsWith("[INFO]")) { return this.txtcolor; }
        else if (infostring.StartsWith("[ERROR]")) { return this.errcolor; }
        else if (infostring.StartsWith("[WARNING]")) { return this.warncolor; }
        else if (infostring.StartsWith("[SETTING IGNORED]")) { return this.ignocolor; }
        else { return this.txtcolor; }
    }
}
