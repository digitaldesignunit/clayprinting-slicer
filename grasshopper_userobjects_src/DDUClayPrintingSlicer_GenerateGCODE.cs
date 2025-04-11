using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System.Linq;

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

    Author: Max Benjamin Eschenbach (Original Base Script by Tom Svilans)
    License: MIT License
    Version: 250411
    */
    #endregion

  private void RunScript(
		DataTree<Polyline> Paths,
		double PrintSpeed,
		double TravelSpeed,
		double RetractionSpeed,
		double ExtrusionRate,
		double InitExtrusionConstant,
		double RetractionConstant,
		double LayerHeight,
		double LineWidth,
		double RetractExtension,
		bool CurveExtensionRetraction,
		double ZHopDistance,
		int FloorLayerCount,
		bool PauseAfterFloor,
		int PauseTime,
		DataTree<double> ExtrusionRateMod,
		ref object GCODE)
  {
    // set component params
    this.Component.Name = "GenerateGCODE";
    this.Component.NickName = "GenerateGCODE";
    this.Component.Category = "DDUClayPrintingSlicer";
    this.Component.SubCategory = "7 GCODE";
    
    // catch empty paths ttree and return empty tree
    if (Paths.DataCount < 1)
    {
      GCODE = new Grasshopper.DataTree<object>();
      return;
    }

    // check if extrusion rate modified values correspond
    bool e_mod = true;
    int ptsum = 0;
    for (int i = 0; i < Paths.BranchCount; i++)
    {
      foreach (Polyline pl in Paths.Branches[i])
      {
        ptsum += pl.Count;
      }
    }
    if (ptsum != ExtrusionRateMod.DataCount)
    {
      e_mod = false;
      Print("ExtrusionRateMod values do not correspond with supplied Paths. " +
        "Constant ExtrusionRate will be used!");
    }

    // init list for storing GCODE strings
    List<string> gcode = new List<string>();

    // get all data from Paths input
    List<Polyline> paths = Paths.AllData();

    // compute union bbx with each layer
    BoundingBox bbx = BoundingBox.Empty;
    foreach (Polyline path in paths)
    {
      bbx.Union(path.BoundingBox);
    }

    // compute offset value from bbx
    Vector3d offset = new Vector3d(-bbx.Center.X, -bbx.Center.Y, 0);
    Print("Offset: {0}", offset);

    // SET CONSTANTS -----------------------------------------------
    // value for safe approach before first layer
    double z_safety = 5.0;

    // initial extrusion rate constant and retraction constant
    double extrusion_rate_extra = InitExtrusionConstant;
    double retraction_amount = RetractionConstant;

    // CREATE HEADER -----------------------------------------------
    Point3d p0 = paths[0][0] + offset;
    gcode.AddRange(new string[]
      {
        "; GCODE Generation Script by: Arkitekturens Teknologi (AT) + SuperFormLab",
        "; Modified, adapted and extended by: Max Eschenbach, DDU - Digital Design Unit, TU Darmstadt",
        string.Format("; Created by : {0}", System.Environment.UserName),
        string.Format("; File name  : {0}", GrasshopperDocument.DisplayName),
        string.Format("; Date time  : {0}", System.DateTime.Now.ToString("yyyy-MM-ddTHH\\:mm\\:ss")),
        ";",
        "; --- BEGIN_DDU_3DCLAYPRINTING_HEADER ---",
        $"; PRINTSPEED={PrintSpeed}",
        $"; TRAVELSPEED={TravelSpeed}",
        $"; RETRACTIONSPEED={RetractionSpeed}",
        $"; EXTRUSIONRATE={ExtrusionRate}",
        $"; LAYERHEIGHT={LayerHeight}",
        $"; LINEWIDTH={LineWidth}",
        "; --- END_DDU_3DCLAYPRINTING_HEADER ---",
        ";",
        "M105 ; get extruder temperature",
        "M109 S0 ; set extruder temp to 0 (deactivate)",
        "M82 ; use absolute distances for extrusion",
        "G90 ; use absolute coordintes",
        "M106 S0 ; set fan speed to 0 (deactivate)",
        "M107 ; fan off",
        "M104 S0 T0 ; set hotend temperature to 0 (deactivate)",
        "G28 ; home all axes",
        "T0 ; set extruder to extruder 0 (first and only one)",
        "G21 ; set units to millimetres",
        // "M204 S200 ; set acceleration",
        "G92 E0 ; reset E distance",
        "; approach the first vertex using z safety height",
        string.Format("G0 X0.0 Y0.0 Z{0:0.000} F{1}", p0.Z + z_safety, 3000),
        "; approach first point with x and y coordinates",
        string.Format("G1 X{0:0.000} Y{1:0.000} Z{2:0.000}", p0.X, p0.Y, p0.Z + z_safety),
        "; --- END HEADER ---",
      });


    // CREATE PRINT PATHS GCODE  ------------------------------------
    Point3d temp = Paths.Branches[0][0][0];
    double emod_value = 1.0;

    // LOOP OVER ALL LAYERS (BRANCHES)
    for (int i = 0; i < Paths.BranchCount; i++)
    {
      // pause after floor printing on request
      if (PauseAfterFloor && FloorLayerCount > 0 && i == FloorLayerCount)
      {
        // add marker to GCODE out
        gcode.Add(string.Format("; ///// PAUSE AFTER FLOOR FOR {0} ms /////", PauseTime));
        // reset extrusion distance
        gcode.Add(string.Format("G92 E0 ; Reset Extrusion Distance"));
        // move 5mm upwards from last point
        gcode.Add(string.Format("G0 X{0:0.000} Y{1:0.000} Z{2:0.000} E{3:0.000} F{4}",
          p0.X, p0.Y, p0.Z + 5.0, 0.000, PrintSpeed));
        // move to pause position 100mm upwards from last point
        gcode.Add(string.Format("G0 X{0:0.000} Y{1:0.000} Z{2:0.000} E{3:0.000} F{4}",
          0.000, 0.000, p0.Z + 100.0, 0.000, PrintSpeed));
        // execute pause command
        gcode.Add(string.Format("G04 P{0:0} ; Dwell / Pause", PauseTime));
      }

      // add layer marker to GCODE out
      gcode.Add(string.Format("; ///// LAYER {0} /////", i + 1));

      // LOOP OVER ALL CURVES IN LAYER (BRANCH)
      for (int j = 0; j < Paths.Branches[i].Count; j++)
      {
        // set current path
        Polyline path = Paths.Branches[i][j];
        PolylineCurve pathcrv = path.ToPolylineCurve();
        // reset extrusion distance
        double ExtrusionDist = 0.0;
        // translate path according to offset
        path.Transform(Transform.Translation(offset));
        pathcrv.Transform(Transform.Translation(offset));
        // get first vertex
        p0 = path.First();
        // format first vertex to GCODE
        gcode.Add(string.Format("G1 X{0:0.000} Y{1:0.000} Z{2:0.000} E{3:0.000} F{4}",
          p0.X, p0.Y, p0.Z + ZHopDistance, ExtrusionDist, TravelSpeed));
        // set extrusion on first vertex (no distance here!)
        ExtrusionDist += extrusion_rate_extra;
        // set temporary vertex
        temp = p0;
        // loop over all vertices in path
        for (int k = 0; k < path.Count; k++)
        {

          // get point distance to previous point
          double ptDist = temp.DistanceTo(path[k]);
          if (k > 0 && ptDist < Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
          {
            Print("Point Distance below tolerance, point is skipped!");
            continue;
          }

          // compute extrusion rate modified (if applicable)
          if (e_mod == true)
          {
            emod_value = ExtrusionRateMod.Branches[i][k];
          }

          // set extrusion distance based on pt distance and emod
          ExtrusionDist += ptDist * (ExtrusionRate * emod_value);

          // compile GCODE string
          // for first vertex use half print speed
          gcode.Add(string.Format("G1 X{0:0.000} Y{1:0.000} Z{2:0.000} E{3:0.000} F{4}",
            path[k].X, path[k].Y, path[k].Z, ExtrusionDist, (k == 0) ? PrintSpeed * 0.5 :  PrintSpeed));

          // set new temp vertex
          temp = path[k];
        }

        // retract on last vertex or extend during retraction move
        if (RetractExtension > 0.0)
        {
          if (CurveExtensionRetraction && path.IsClosedWithinTolerance(0.01) && path.Length > RetractExtension)
          {
            // move retractextension amount along the curve

            // find point on curve that is RetractionExtension mm along crv
            double ret_end_t;
            bool _suc = pathcrv.LengthParameter(RetractExtension, out ret_end_t);
            PolylineCurve subcrv = pathcrv.Split(new List<double>{0.0, ret_end_t})[0] as PolylineCurve;
            Polyline subpl = subcrv.ToPolyline();

            temp = subpl.First;
            for (int k = 1; k < subpl.Count; k++)
            {
              double ptDist = temp.DistanceTo(subpl[k]);
              if (k > 0 && ptDist < Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
              {
                Print("Point Distance below tolerance, point is skipped!");
                continue;
              }

              // compute retraction amount for point
              ExtrusionDist -= (ptDist / RetractExtension) * retraction_amount;

              // create retraction gcode
              gcode.Add(string.Format("G1 X{0:0.000} Y{1:0.000} Z{2:0.000} E{3:0.000} F{4} ; RetractExtensionClosedCurve",
                subpl[k].X, subpl[k].Y, subpl[k].Z, ExtrusionDist, RetractionSpeed));

              // set new temp vertex
              temp = subpl[k];
            }

            // create z-hop gcode
            gcode.Add(string.Format("G1 X{0:0.000} Y{1:0.000} Z{2:0.000} E{3:0.000} F{4} ; Z-Hop",
              subpl.Last.X, subpl.Last.Y, subpl.Last.Z + ZHopDistance, ExtrusionDist, RetractionSpeed));
          }
          else
          {
            // move retract extension amount in direction of last move and retract
            Vector3d rvec = new Vector3d(path.Last - path[path.Count - 2]);
            rvec.Unitize();
            p0 = path.Last + (rvec * RetractExtension);
            ExtrusionDist -= retraction_amount;
            gcode.Add(string.Format("G1 X{0:0.000} Y{1:0.000} Z{2:0.000} E{3:0.000} F{4} ; RetractExtension",
              p0.X, p0.Y, p0.Z, ExtrusionDist, RetractionSpeed));

            gcode.Add(string.Format("G1 X{0:0.000} Y{1:0.000} Z{2:0.000} E{3:0.000} F{4} ; Z-Hop",
              p0.X, p0.Y, p0.Z + ZHopDistance, ExtrusionDist, RetractionSpeed));
          }
        }
        else
        {
          // use last point for retraction move and move upwards
          p0 = path.Last();
          ExtrusionDist -= retraction_amount;
          gcode.Add(string.Format("G1 X{0:0.000} Y{1:0.000} Z{2:0.000} E{3:0.000} F{4} ; Z-Hop with retraction",
            p0.X, p0.Y, p0.Z + ZHopDistance, ExtrusionDist, RetractionSpeed));
        }

        // reset extrusion distance
        gcode.Add(string.Format("G92 E0 ; Reset Extrusion Distance"));
      }
    }

    // CREATE FOOTER -----------------------------------------------------
    gcode.AddRange(new string[]
      {
      "G1 E-1 ; retract",
      "G92 E0 ; reset E value",
      "M104 S0 ; turn off extruder",
      "M140 S0 ; turn off bed",
      "G28 X0 Y0 ; home all axes",
      "M84; disable motors",
      "M82 ; absolute extrusion mode",
      "; --- END OF GCODE ---",
      });

    // set gcode to output
    GCODE = gcode;
  }

}
