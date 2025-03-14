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

    Author: Cameron Newnham 2015-2016
    Modified and Updated 2025 by Max Benjamin Eschenbach
    License: GNU GPL version 3
    Version: 250314

    *      ___  _  _  ____   __   _  _   __  ____   __  ____  __  ____
    *     / __)/ )( \(  _ \ /  \ ( \/ ) /  \(    \ /  \(  _ \(  )/ ___)
    *    ( (__ ) __ ( )   /(  O )/ \/ \(  O )) D ((  O ))   / )( \___ \
    *     \___)\_)(_/(__\_) \__/ \_)(_/ \__/(____/ \__/(__\_)(__)(____/
    *
    *    Copyright Cameron Newnham 2015-2016
    *
    *    This program is free software: you can redistribute it and/or modify
    *    it under the terms of the GNU General Public License as published by
    *    the Free Software Foundation, either version 3 of the License, or
    *    (at your option) any later version.
    *
    *    This program is distributed in the hope that it will be useful,
    *    but WITHOUT ANY WARRANTY; without even the implied warranty of
    *    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    *    GNU General Public License for more details.
    *
    *    You should have received a copy of the GNU General Public License
    *    along with this program.  If not, see <http://www.gnu.org/licenses/>.
    */
    #endregion

    private void RunScript(List<Curve> P, int N, double R, bool C, ref object M)
    {
        // set component params
        this.Component.Name = "ChromoMeshPipe";
        this.Component.NickName = "ChromoMeshPipe";
        this.Component.Category = "DDUClayPrintingSlicer";
        this.Component.SubCategory = "8 Visualisation";

        List<Polyline> pls = new List<Polyline>();
        if (P == null)
        {
            M = new DataTree<object>();
            return;
        }
        foreach (var c in P)
        {
            Polyline p;
            if (c.TryGetPolyline(out p)) {
                pls.Add(p);
            }
            else
            {
                this.Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input must be polylines.");
                return;
            }
        }
        Mesh _M = GetPiped(pls, N, R, C);
        _M.UnifyNormals();
        M = new GH_Mesh(_M);
    }

    public Mesh GetPiped(List<Polyline> polylines, int numSides, double radius, bool cap)
    {
        var pt = new Point3d(radius, 0, 0);
        Polyline polygon = new Polyline();

        Point3d curr = new Point3d(pt);
        polygon.Add(curr);
        var xfm = Transform.Rotation(Math.PI * 2 / (double) numSides, Point3d.Origin);
        for (int i = 0; i < numSides; i++)
        {
            var pNew = new Point3d(curr);
            pNew.Transform(xfm);

            polygon.Add(pNew);
            curr = pNew;
        }
        var multimesh = new Mesh[polylines.Count];
        System.Threading.Tasks.Parallel.For(0, polylines.Count, pc =>
        {
            Polyline pl = polylines[pc];
            List<Plane> frames = new List<Plane>();
            for (int i = 0; i < pl.Count; i++)
            {
                Point3d o = pl[i];
                Vector3d dir = Vector3d.Unset;
                if (i == 0)
                {
                    if (!pl.IsClosed)
                    {
                        dir = pl[1] - pl[0];
                        dir.Unitize();
                    }
                    else
                    {
                        Vector3d prevDir = pl[0] - pl[pl.Count - 2];
                        Vector3d nextDir = pl[1] - pl[0];
                        prevDir.Unitize();
                        nextDir.Unitize();
                        dir = (prevDir + nextDir) / 2;
                        dir.Unitize();
                    }
                }
                else if (i == pl.Count - 1)
                {
                    if (!pl.IsClosed)
                    {
                        dir = pl[pl.Count - 1] - pl[pl.Count - 2];
                        dir.Unitize();
                    }
                }
                else
                {
                Vector3d prevDir = pl[i] - pl[i - 1];
                Vector3d nextDir = pl[i + 1] - pl[i];
                prevDir.Unitize();
                nextDir.Unitize();
                dir = (prevDir + nextDir) / 2;
                dir.Unitize();
                }
                if (frames.Count > 0)
                {
                    if (dir != Vector3d.Unset)
                    {
                        var prevFrame = frames.Last();
                        var newPlane = new Plane(o, dir);
                        double rotAng = Vector3d.VectorAngle(prevFrame.XAxis, newPlane.XAxis, newPlane);
                        newPlane.Rotate(-rotAng, newPlane.ZAxis);
                        frames.Add(newPlane);
                    }
                }
                else
                {
                    frames.Add(new Plane(o, dir));
                }
            }
            Mesh mesh = new Mesh();
            Mesh capm = null;
            var poly = new Polyline(polygon);
            poly.Transform(Transform.PlaneToPlane(Plane.WorldXY, frames.First()));
            int[] lastVerts = new int[numSides];
            for (int p = 0; p < poly.Count - 1; p++)
            {
                lastVerts[p] = mesh.Vertices.Add(poly[p]);
            }
            int[] firstVerts = lastVerts;
            if (cap && !pl.IsClosed)
            {
                capm = Mesh.CreateFromClosedPolyline(poly);
            }
            for (int i = 1; i < frames.Count; i++)
            {
                poly = new Polyline(polygon);
                poly.Transform(Transform.PlaneToPlane(Plane.WorldXY, frames[i]));
                int[] newVerts = new int[numSides];
                for (int p = 0; p < poly.Count - 1; p++)
                {
                    newVerts[p] = mesh.Vertices.Add(poly[p]);
                }
                for (int v = 1; v <= numSides; v++)
                {
                    if (v == numSides)
                    {
                        mesh.Faces.AddFace(lastVerts[v - 1], lastVerts[0], newVerts[0], newVerts[v - 1]);
                    }
                    else
                    {
                        mesh.Faces.AddFace(lastVerts[v - 1], lastVerts[v], newVerts[v], newVerts[v - 1]);

                    }
                }
                lastVerts = newVerts;
            }
            if (pl.IsClosed)
            {
                // resolve any twists
                for (int v = 1; v <= numSides; v++)
                {
                    if (v == numSides)
                    {
                        mesh.Faces.AddFace(lastVerts[v - 1], lastVerts[0], firstVerts[0], firstVerts[v - 1]);
                    }
                    else
                    {
                        mesh.Faces.AddFace(lastVerts[v - 1], lastVerts[v], firstVerts[v], firstVerts[v - 1]);

                    }
                }
            }
            else if (cap)
            {
                capm.Append(Mesh.CreateFromClosedPolyline(poly));
                mesh.Append(capm);
            }
            multimesh[pc] = mesh;
        });
        Mesh combined = new Mesh();
        foreach (var m in multimesh)
        {
            combined.Append(m);
        }
        combined.Normals.ComputeNormals();
        return combined;
    }
}
