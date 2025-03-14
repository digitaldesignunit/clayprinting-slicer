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

    Author: Giulio Piacentino (updated by Anders Holden Deleuran)
    Updated 2025 by Max Benjamin Eschenbach
    License: Apache License 2.0
    Version: 250307
    */
    #endregion

    private void RunScript(bool Toggle, ref object Core, ref object AddOns)
    {   
        // set component params
        this.Component.Name = "DefinitionDependencies";
        this.Component.NickName = "DefinitionDependencies";
        this.Component.Category = "DDUClayPrintingSlicer";
        this.Component.SubCategory = "0 Development";

        List<string> coreLibs = new List<string>();
        List<string> addLibs = new List<string>();
        if (Toggle)
        {
            Dictionary<string, GH_AssemblyInfo> coreLibraries = new Dictionary<string, GH_AssemblyInfo>();
            Dictionary<string, GH_AssemblyInfo> addonLibraries = new Dictionary<string, GH_AssemblyInfo>();
            Dictionary<string, string> objids = new Dictionary<string, string>();
            GH_ComponentServer server = Grasshopper.Instances.ComponentServer;
            foreach (GH_DocumentObject obj in this.Component.OnPingDocument().Objects)
            {
                if (obj == (GH_DocumentObject) this.Component)
                {
                    continue;
                }
                string objid = obj.ComponentGuid.ToString();
                if (objids.ContainsKey(objid)) continue;
                GH_AssemblyInfo lib = server.FindAssemblyByObject(obj);
                if (lib == null) continue;
                if (coreLibraries.ContainsKey(lib.Id.ToString()) | addonLibraries.ContainsKey(lib.Id.ToString())) continue;
                if (lib.IsCoreLibrary) coreLibraries.Add(lib.Id.ToString(), lib);
                else addonLibraries.Add(lib.Id.ToString(), lib);
            }
            foreach(KeyValuePair<string, GH_AssemblyInfo> entry in coreLibraries)
            {
                coreLibs.Add($"{entry.Value.Name} {entry.Value.Version}");
            }
            foreach(KeyValuePair<string, GH_AssemblyInfo> entry in addonLibraries)
            {
                addLibs.Add($"{entry.Value.Name} {entry.Value.Version}");
            }
        }
        Core = coreLibs;
        AddOns = addLibs;
    }
}
