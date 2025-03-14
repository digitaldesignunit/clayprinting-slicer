#region Usings
using System;
using System.IO;
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
    Version: 250312
    */
    #endregion

    private void RunScript(bool Save, string ArchiveFolder, ref object Message)
    {
        // set component params
        this.Component.Name = "SaveAndSaveGHX";
        this.Component.NickName = "SaveAndSaveGHX";
        this.Component.Category = "DDUClayPrintingSlicer";
        this.Component.SubCategory = "0 Development";

        // This output will tell us if the save succeeded or failed.
        // The active Grasshopper document in this scripting component context

        DataTree<string> msgOut = new DataTree<string>();
        GH_Path ghp = new GH_Path(0, 1);

        if (!Save)
        {
            return;
        }

        GH_Document doc = this.GrasshopperDocument;
        if (doc == null)
        {
            msgOut.Add("Error: GrasshopperDocument is null", ghp);
            return;
        }

        // Set current and archive filepath
        string originalFilePath = doc.FilePath;
        string ghxFilePath = originalFilePath + "x";
        string archivePath = Path.Combine(Path.GetDirectoryName(ghxFilePath), ArchiveFolder);
        string archiveFilePath = Path.Combine(archivePath, DateTime.Now.ToString("yyMMdd") + "_" + Path.GetFileName(originalFilePath));

        // Only attempt to save when 'saveTrigger' is set to true.
        if (!Save) return;
        
        // Make sure the parent directory exists
        if (!Directory.Exists(Path.GetDirectoryName(ghxFilePath)))
        {
            msgOut.Add("Error: directory does not exist", ghp);
            return;
        }

        // Make sure Archive Directory exists
        // Make sure the parent directory exists
        
        if (!Directory.Exists(Path.GetDirectoryName(archivePath)))
        {
            msgOut.Add("Error: Archive directory does not exist", ghp);
            return;
        }

        // Create a GH_DocumentIO object based on our current GH_Document.
        GH_DocumentIO io = new GH_DocumentIO(doc);
        // Save this document
        io.Save();

        // Write the current document to an XML-based .ghx file
        bool success_a = io.SaveQuiet(ghxFilePath);
        bool success_b = io.SaveQuiet(originalFilePath);
        bool success_c = io.SaveQuiet(archiveFilePath);

        if (success_a & success_b & success_c)
            msgOut.Add($"Files saved successfully:\n{ghxFilePath}\n{archiveFilePath}", ghp);
        else
        {
            if (!success_a)
                msgOut.Add($"Error: failed to save .ghx file:\n{ghxFilePath}", ghp);
            if (!success_c)
                msgOut.Add($"Error: failed to save Archive .gh file:\n{archiveFilePath}", ghp);
        }
        Message = msgOut;
    }
}