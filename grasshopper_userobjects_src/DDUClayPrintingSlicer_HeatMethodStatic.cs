#region Usings
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

#r "KangarooSolver.dll"
using KPlankton;
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
    
        Author: Original Script by Daniel Piker 03/07/2020, Modified by Max Benjamin Eschenbach May 2021
        License: MIT License
        Version: 250314
    */
    #endregion

    private void RunScript(
		Mesh RefMesh,
		List<Point3d> HeatPoints,
		List<Point3d> ColdPoints,
		List<Polyline> IsoFixCurves,
		double TextureScale,
		double Threshold,
		int MaxIterations,
		ref object HeatMesh,
		ref object Values,
		ref object Gradient)
    {
        // set component params
        this.Component.Name = "HeatMethodStatic";
        this.Component.NickName = "HeatMethodStatic";
        this.Component.Category = "DDUClayPrintingSlicer";
        this.Component.SubCategory = "1 Slicing";

        // set defaults
        bool abort = false;
        if (RefMesh == null)
        {
        this.Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
            "Input Parameter RefMesh failed to collect data!");
        abort = true;
        }

        if (MaxIterations == 0) MaxIterations = 200;
        if (abort)
        {
        HeatMesh = new DataTree<Object>();
        Values = new DataTree<Object>();
        Gradient = new DataTree<Object>();
        return;
        }

        // init variables
        double[] values;
        double[][] storedweights;
        int[][] storedneighbours;
        Vector3d[] gradient;
        Vector3d[] edgeGradient;
        int[] fix;
        int[][] equalize;
        Mesh ResultMesh;

        // init vars for threshold tracking
        double heat_avg = 0;
        double poisson_avg = 0;
        bool converged = false;

        // INITIALIZE HEAT METHOD -----------------------------------------------

        // convert all quads to triangles
        RefMesh.Faces.ConvertQuadsToTriangles();

        // get kplankton mesh
        KPlanktonMesh ReferencePMesh = KPlankton.RhinoSupport.ToKPlanktonMesh(RefMesh);

        int vc = ReferencePMesh.Vertices.Count;
        storedweights = new double[vc][];
        storedneighbours = new int[vc][];
        values = new double[vc];
        fix = new int[vc];
        equalize = new int[IsoFixCurves.Count][];
        for(int i = 0;i < vc;i++)
        {
            storedweights[i] = ComputeCotanWeights(ReferencePMesh, i);
            storedneighbours[i] = ReferencePMesh.Vertices.GetVertexNeighbours(i);
            for(int j = 0;j < HeatPoints.Count;j++)
            {
                if(ReferencePMesh.Vertices[i].ToPoint3d().DistanceToSquared(HeatPoints[j]) < Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                {
                    values[i] = 1;
                    fix[i] = 1;
                }
            }
            for(int j = 0;j < ColdPoints.Count;j++)
            {
                if(ReferencePMesh.Vertices[i].ToPoint3d().DistanceToSquared(ColdPoints[j]) < Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                {
                    values[i] = 0;
                    fix[i] = 2;
                }
            }
        }

        for(int i = 0;i < IsoFixCurves.Count;i++)
        {
            Polyline thisPoly = IsoFixCurves[i];
            int polyCount = thisPoly.Count;
            if(thisPoly[thisPoly.Count - 1].Equals(thisPoly[0])) polyCount--;//don't count first point twice for closed curves
            equalize[i] = new int[polyCount];
            for(int j = 0;j < polyCount;j++)
            {
                for(int k = 0;k < vc;k++)
                {
                    if(ReferencePMesh.Vertices[k].ToPoint3d().DistanceToSquared(thisPoly[j]) < Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                    {
                        equalize[i][j] = k;
                        break;
                    }
                }
            }
        }

        gradient = new Vector3d[vc];
        ResultMesh = RefMesh.DuplicateMesh();

        edgeGradient = null;

        // HEAT DIFFUSION -----------------------------------------------------

        int heat_iters = 0;
        for(int k = 0; k < MaxIterations; k++)
        {
        heat_iters += 1;
        double[] newValues = new double[values.Length];
        Parallel.For(0, ReferencePMesh.Vertices.Count,
            i => {

            if(fix[i] == 0)
            {
            int[] neighbours = storedneighbours[i];
            double[] weights = storedweights[i];
            double newValue = 0;
            for(int j = 0;j < neighbours.Length;j++)
            {
                newValue += weights[j] * values[neighbours[j]];
            }
            newValues[i] = newValue;
            }
            else newValues[i] = values[i];
            });

        for(int i = 0; i < equalize.Length; i++)
        {
            double avg = 0;
            for(int j = 0;j < equalize[i].Length;j++)avg += newValues[equalize[i][j]];
            avg /= equalize[i].Length;
            for(int j = 0;j < equalize[i].Length;j++) newValues[equalize[i][j]] = avg;
        }
        values = newValues;

        // track average heat value change
        double this_heat_avg = System.Linq.Enumerable.Average(values);
        double heat_avg_change = Math.Abs(this_heat_avg - heat_avg);
        heat_avg = this_heat_avg;

        // break if threshold is reached
        if (heat_avg <= Threshold)
        {
            converged = true;
            break;
        }
        }

        double min = values.Min();
        double max = values.Max();
        double mult = 1.0 / (max - min);
        for(int i = 0;i < values.Length;i++) values[i] = mult * (values[i] - min);

        edgeGradient = null;

        // POISSON AVERAGING --------------------------------------------------

        if(edgeGradient == null)
        {
        gradient = ComputeGradients(ReferencePMesh, values);
        double avgGradientLength = 0;
        for(int i = 0;i < gradient.Length;i++)
        {
            avgGradientLength += gradient[i].Length;
        }
        avgGradientLength /= gradient.Length;

        edgeGradient = new Vector3d[ReferencePMesh.Halfedges.Count / 2];
        for(int i = 0;i < ReferencePMesh.Halfedges.Count / 2;i++)
        {
            int start = ReferencePMesh.Halfedges[2 * i].StartVertex;
            int end = ReferencePMesh.Halfedges[(2 * i) + 1].StartVertex;
            Vector3d sv = gradient[start];
            Vector3d ev = gradient[end];
            Vector3d thisEdgeGradient = sv + ev;
            thisEdgeGradient.Unitize();
            thisEdgeGradient *= avgGradientLength;
            edgeGradient[i] = thisEdgeGradient;
        }
        }

        int ps_iters = 0;
        for(int k = 0; k < MaxIterations; k++)
        {
        ps_iters++;
        double[] heChange = new double[ReferencePMesh.Halfedges.Count];
        Parallel.For(0, ReferencePMesh.Halfedges.Count / 2,
            i => {
            int start = ReferencePMesh.Halfedges[2 * i].StartVertex;
            int end = ReferencePMesh.Halfedges[(2 * i) + 1].StartVertex;
            Vector3d edgeVector = ReferencePMesh.Vertices[end].ToPoint3d() - ReferencePMesh.Vertices[start].ToPoint3d();
            double projected = edgeVector * edgeGradient[i];
            double currentDifference = values[end] - values[start];
            double change = currentDifference - projected;
            heChange[2 * i] = 1 * change;
            heChange[(2 * i) + 1] = -1 * change;
            });

        Parallel.For(0, ReferencePMesh.Vertices.Count,
            i => {
            {
            {
                double[] weights = storedweights[i];
                int[] halfedges = ReferencePMesh.Vertices.GetHalfedges(i);
                for(int j = 0;j < halfedges.Length;j++)
                {
                values[i] += heChange[halfedges[j]] * weights[j];
                }
            }
            }
            });

        double this_poisson_avg = 0;
        for(int i = 0;i < equalize.Length;i++)
        {
            double avg = 0;
            for(int j = 0;j < equalize[i].Length;j++)avg += values[equalize[i][j]];
            avg /= equalize[i].Length;
            for(int j = 0;j < equalize[i].Length;j++) values[equalize[i][j]] = avg;
            this_poisson_avg = avg;
        }

        // track average value change for poisson step
        double poisson_avg_change = Math.Abs(this_poisson_avg - poisson_avg);
        poisson_avg = this_poisson_avg;

        /*
        if (poisson_avg_change <= Threshold)
        {
            converged = true;
            break;
        }
        */
        }

        if (converged == true)
        {
        this.Component.Message = String.Format("Converged.\n{0} Heat Diffusion iterations", heat_iters);
        }
        else
        {
        this.Component.Message = "MaxIterations reached!";
        }


        // SET TEXTURE COORDINATES ----------------------------------------------

        for(int i = 0;i < ResultMesh.Vertices.Count;i++)
        {
        ResultMesh.TextureCoordinates.SetTextureCoordinate(i, new Point2f(0, (float) (values[i] * TextureScale)));
        }

        // OUTPUT ----------------------------------------------------------------

        // input mesh with adjusted texture coordinates
        HeatMesh = ResultMesh;
        // the scalar values and gradient vectors per vertex
        Values = values;
        Gradient = gradient;
    }

    private static Vector3d[] ComputeGradients(KPlanktonMesh KPM, double[] values)
    {
        // compute halfedge gradients
        Vector3d[] halfedgeGradients = new Vector3d[KPM.Halfedges.Count];
        for(int i = 0;i < KPM.Halfedges.Count / 2;i++)
        {
        int start = KPM.Halfedges[2 * i].StartVertex;
        int end = KPM.Halfedges[2 * i + 1].StartVertex;
        Vector3d v = KPM.Vertices[end].ToPoint3d() - KPM.Vertices[start].ToPoint3d();
        double lengthSquared = v.SquareLength;
        double diff = values[end] - values[start];
        Vector3d edgeGradient = v * (diff / lengthSquared);
        halfedgeGradients[2 * i] = halfedgeGradients[2 * i + 1] = edgeGradient;
        }

        // compute vertex gradients from halfedge gradients
        Vector3d[] vertexGradients = new Vector3d[KPM.Vertices.Count];
        for(int i = 0;i < KPM.Vertices.Count;i++)
        {
        double[] weights = ComputeCotanWeights(KPM, i);
        Vector3d gradient = new Vector3d();
        int[] halfedges = KPM.Vertices.GetHalfedges(i);
        for(int j = 0;j < halfedges.Length;j++)
        {
            gradient += halfedgeGradients[halfedges[j]] * weights[j];
        }
        vertexGradients[i] = gradient;
        }
        return vertexGradients;
    }

    private static double[] ComputeCotanWeights(KPlanktonMesh KPM, int i)
    {
        int[] Neighbours = KPM.Vertices.GetVertexNeighbours(i);
        Point3d Vertex = KPM.Vertices[i].ToPoint3d();
        int valence = KPM.Vertices.GetValence(i);
        Point3d[] NeighbourPts = new Point3d[valence];
        Vector3d[] Radial = new Vector3d[valence];
        Vector3d[] Around = new Vector3d[valence];
        double[] CotWeight = new double[valence];
        double WeightSum = 0;
        for (int j = 0; j < valence; j++)
        {
        NeighbourPts[j] = KPM.Vertices[Neighbours[j]].ToPoint3d();
        Radial[j] = NeighbourPts[j] - Vertex;
        }
        for (int j = 0; j < valence; j++)
        {
        Around[j] = NeighbourPts[(j + 1) % valence] - NeighbourPts[j];
        }
        int[] halfEdges = KPM.Vertices.GetHalfedges(i);
        for (int j = 0; j < Neighbours.Length; j++)
        {
        int previous = (j + valence - 1) % valence;
        int next = (j + 1) % valence;

        Vector3d Cross1 = Vector3d.CrossProduct(Radial[previous], Around[previous]);
        double Dot1 = Radial[previous] * Around[previous];
        double cwa = Math.Abs(Dot1 / Cross1.Length);

        Vector3d Cross2 = Vector3d.CrossProduct(Radial[next], Around[j]);
        double Dot2 = Radial[next] * Around[j];
        double cwb = Math.Abs(Dot2 / Cross2.Length);

        if(KPM.Halfedges[halfEdges[j]].AdjacentFace == -1){cwa = 0;}
        if(KPM.Halfedges[KPM.Halfedges.GetPairHalfedge(halfEdges[j])].AdjacentFace == -1){cwb = 0;}
        CotWeight[j] = cwa + cwb;
        WeightSum += CotWeight[j];
        }
        for (int j = 0; j < CotWeight.Length; j++) CotWeight[j] /= WeightSum;
        return CotWeight;
    }

}
