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
using TreeExtensions;
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
    Version: 250410
    */
    #endregion

    private void RunScript(
		DataTree<Curve> InnerCurves,
		DataTree<Curve> OuterCurves,
		int FloorLayerCount,
		int CapLayerCount,
		bool FloorEnabled,
		double Threshold,
		bool Debug,
		ref object DebugMessage,
		ref object OuterFloorLayers,
		ref object InnerFloorLayers,
		ref object OuterRegularLayers,
		ref object InnerRegularLayers,
		ref object OuterOverhangLayers,
		ref object InnerOverhangLayers,
		ref object OuterCapLayers,
		ref object InnerCapLayers)
    {
        // set component params
        this.Component.Name = "ClassifyClayPrintLayers";
        this.Component.NickName = "ClassifyClayPrintLayers";
        this.Component.Category = "DDUClayPrintingSlicer";
        this.Component.SubCategory = "2 Slice Processing";

        // Output DataTrees (OuterCurves)
        DataTree<Curve> OuterFloors = new DataTree<Curve>();
        DataTree<Curve> OuterRegulars = new DataTree<Curve>();
        DataTree<Curve> OuterOverhangs = new DataTree<Curve>();
        DataTree<Curve> OuterCaps = new DataTree<Curve>();
        // Output DataTrees (InnerCurves)
        DataTree<Curve> InnerFloors = new DataTree<Curve>();
        DataTree<Curve> InnerRegulars = new DataTree<Curve>();
        DataTree<Curve> InnerOverhangs = new DataTree<Curve>();
        DataTree<Curve> InnerCaps = new DataTree<Curve>();
        // Debugging Message(s)
        DataTree<string> ReportMessages = new DataTree<string>();
        // DataTree Paths
        GH_Path lastPath;
        GH_Path thisPath;
        GH_Path nextPath;
        // Outer Layers
        List<Curve> lastLayer;
        List<Curve> thisLayer;
        List<Curve> nextLayer;
        // Inner Layers
        List<Curve> lastInnerLayer;
        List<Curve> thisInnerLayer;
        List<Curve> nextInnerLayer;

        // control flow int for looping and floor definition....
        int subfactor = 1;
        if (OuterCurves.BranchCount == FloorLayerCount)
        {
            subfactor = 0;
        }
        
        for (int i = 0; i < OuterCurves.BranchCount - subfactor; i++)
        {
            // THIS LAYER'S DATA ------------------------------------------------------
            thisPath = new GH_Path(OuterCurves.Paths[i]);
            thisLayer = OuterCurves.Branches[i];
            
            // FIRST LAYER(S) FLOOR DEFINITION -----------------------------------------
            if (i == 0 | i < FloorLayerCount)
            {
                if (FloorEnabled && FloorLayerCount > 0)
                {
                    // ADD INITIAL N LAYERS TO FLOORS IF FLOOR IS ACTIVE
                    OuterFloors.AddRange(thisLayer, thisPath);
                    InnerFloors.AddRangeGracefully(InnerCurves.Branch(thisPath), thisPath);
                    continue;
                }
                else
                {
                    // THISLAYER IS REGULAR/SPARSE LAYER
                    OuterRegulars.AddRange(thisLayer, thisPath);
                    InnerRegulars.AddRangeGracefully(InnerCurves.Branch(thisPath), thisPath);
                    continue;
                }
            }

            // GET NEXT AND LAST LAYER DATA ------------------------------------------
            // Paths
            lastPath = new GH_Path(OuterCurves.Paths[i - 1]);
            nextPath = new GH_Path(OuterCurves.Paths[i + 1]);
            // Layers
            lastLayer = OuterCurves.Branches[i - 1];
            nextLayer = OuterCurves.Branches[i + 1];

            // AREA COMPUTATION AND DIFFERENCE -----------------------------------------
            // Get cumulative area of outer layer curves
            double lastArea = this.GetLayerArea(lastLayer, lastPath);
            double thisArea = this.GetLayerArea(thisLayer, thisPath);
            double nextArea = this.GetLayerArea(nextLayer, nextPath);
            // Compute area difference between outer layers
            double diffLast = thisArea - lastArea;
            double diffNext = thisArea - nextArea;
            // Compute percentage difference
            double percLast = (diffLast / thisArea) * 100;
            double percNext = (diffNext / thisArea) * 100;

            // COMPILE REPORT MESSAGE
            if (Debug)
            {
                ReportMessages.Add(
                    string.Format(
                        "LAST {2} <- {0}% <--| {3} THIS {3} |--> {1}% -> {4} NEXT",
                        Math.Round(percLast, 2).ToString(),
                        Math.Round(percNext, 2).ToString(),
                        lastPath.ToString(),
                        thisPath.ToString(),
                        nextPath.ToString()
                    ), new GH_Path(0, 1)
                );
            }
            
            // CAP CLASSIFICATION --------------------------------------------------------
            if ((percNext > 0) && (Math.Abs(percNext) > Threshold) && CapLayerCount > 0)
            {
                // THISLAYER IS LARGER THAN NEXT LAYER --> THISLAYER IS CAP LAYER
                OuterCaps.AddRange(thisLayer, thisPath);
                InnerCaps.AddRangeGracefully(InnerCurves.Branch(thisPath), thisPath);
                // RE-CLASSIFY CAPS BACKWARDS
                for (int j = 1; j < CapLayerCount && (i - j) >= 0; j++) 
                {
                    GH_Path prevPath = new GH_Path(OuterCurves.Paths[i - j]);
                    List<Curve> prevLayer = OuterCurves.Branches[i - j];
                    // Add to CAP and remove REGULAR - Leave as is if it is already a FLOOR
                    if (!OuterFloors.Paths.Contains(prevPath) & !InnerFloors.Paths.Contains(prevPath)) 
                    {
                        OuterCaps.AddRange(prevLayer, prevPath);
                        InnerCaps.AddRangeGracefully(InnerCurves.Branch(prevPath), prevPath);
                        OuterRegulars.RemovePath(prevPath);
                        InnerRegulars.RemovePath(prevPath);
                        OuterOverhangs.RemovePath(prevPath);
                        InnerOverhangs.RemovePath(prevPath);
                    }
                }
                if (i >= OuterCurves.BranchCount - 2 && CapLayerCount > 0)
                {
                    OuterCaps.AddRange(nextLayer, nextPath);
                    InnerCaps.AddRangeGracefully(InnerCurves.Branch(nextPath), nextPath);
                }
                else
                {
                    // THISLAYER IS REGULAR/SPARSE LAYER
                    OuterRegulars.AddRange(nextLayer, nextPath);
                    InnerRegulars.AddRangeGracefully(InnerCurves.Branch(nextPath), nextPath);
                    continue;
                }
            }

            // LAST LAYER(S) CAP CLASSIFICATION --------------------------------------------
            if (i >= OuterCurves.BranchCount - 2)
            {
                if (CapLayerCount > 0)
                {
                    OuterCaps.AddRange(nextLayer, nextPath);
                    InnerCaps.AddRangeGracefully(InnerCurves.Branch(nextPath), nextPath);
                    // RE-CLASSIFY CAPS BACKWARDS
                    for (int j = 1; j < CapLayerCount && (i + 1 - j) >= 0; j++) 
                    {
                        GH_Path prevPath = new GH_Path(OuterCurves.Paths[i + 1 - j]);
                        List<Curve> prevLayer = OuterCurves.Branches[i + 1 - j];
                        // Add to CAP and remove REGULAR - Leave as is if it is already a FLOOR
                        if (!OuterFloors.Paths.Contains(prevPath) & !InnerFloors.Paths.Contains(prevPath)) 
                        {
                            OuterCaps.AddRange(prevLayer, prevPath);
                            InnerCaps.AddRangeGracefully(InnerCurves.Branch(prevPath), prevPath);
                            OuterRegulars.RemovePath(prevPath);
                            InnerRegulars.RemovePath(prevPath);
                            OuterOverhangs.RemovePath(prevPath);
                            InnerOverhangs.RemovePath(prevPath);
                        }
                    }
                }
                else
                {
                    // THISLAYER IS REGULAR/SPARSE LAYER
                    OuterRegulars.AddRange(thisLayer, thisPath);
                    InnerRegulars.AddRangeGracefully(InnerCurves.Branch(thisPath), thisPath);
                    OuterRegulars.AddRange(nextLayer, nextPath);
                    InnerRegulars.AddRangeGracefully(InnerCurves.Branch(nextPath), nextPath);
                }
                continue;
            }

            // OVERHANG CLASSIFICATION ---------------------------------------------------
            else if ((percLast > 0) && (Math.Abs(percLast) > Threshold))
            {
                // THISLAYER IS LARGER THAN LAST LAYER --> THISLAYER IS OVERHANG LAYER
                OuterOverhangs.AddRange(thisLayer, thisPath);
                InnerOverhangs.AddRangeGracefully(InnerCurves.Branch(thisPath), thisPath);
            }

            // REGULAR / SPARSE CLASSIFICATION ------------------------------------------
            else
            {
                // THISLAYER IS REGULAR/SPARSE LAYER
                OuterRegulars.AddRange(thisLayer, thisPath);
                InnerRegulars.AddRangeGracefully(InnerCurves.Branch(thisPath), thisPath);
            }

        }
        // Set outputs
        DebugMessage = ReportMessages;
        OuterFloorLayers = OuterFloors;
        OuterRegularLayers = OuterRegulars;
        OuterOverhangLayers = OuterOverhangs;
        OuterCapLayers = OuterCaps;
        InnerFloorLayers = InnerFloors;
        InnerRegularLayers = InnerRegulars;
        InnerOverhangLayers = InnerOverhangs;
        InnerCapLayers = InnerCaps;
    }

    private double GetLayerArea(List<Curve> Layer, GH_Path path = null)
    {
        double LayerArea = 0.0;
        for (int i = 0; i < Layer.Count; i++)
        {
            AreaMassProperties amp = AreaMassProperties.Compute(Layer[i], 1.0);
            if (amp == null)
            {
                this.Component.AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Warning,
                    $"Area Computation failed for Layer {path}[{i}]!");
                continue;
            }
            LayerArea += amp.Area;
        }
        return LayerArea;
    }
}

    namespace TreeExtensions
    {
        public static class DataTreeExtensions
        {
            public static void AddRangeGracefully(this DataTree<Curve> tree, IEnumerable<Curve> data, GH_Path path)
            {
                try
                {
                    tree.AddRange(data, path);
                }
                catch (ArgumentNullException)
                {
                    tree.RemovePath(path);
                }
            }
        }
    }

