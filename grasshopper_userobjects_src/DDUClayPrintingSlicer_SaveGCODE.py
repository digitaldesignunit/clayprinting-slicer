# PYTHON STANDARD LIBRARY IMPORTS
from __future__ import division, print_function
from os.path import abspath, basename, dirname, normpath

# GHPYTHON SDK IMPORTS
import System
import Rhino
import Grasshopper
import rhinoscriptsyntax as rs

# GHENV COMPONENT SETTINGS
ghenv.Component.Name = "SaveGCODE"
ghenv.Component.NickName = "SaveGCODE"
ghenv.Component.Category = "DDUClayPrintingSlicer"
ghenv.Component.SubCategory = "7 GCODE"

class SaveGCODE(Grasshopper.Kernel.GH_ScriptInstance):
    """
    Author: Max Benjamin Eschenbach
    License: MIT License
    Version: 250314
    """

    def SaveFileDialog(self, title=None, filter=None, folder=None,
                       filename=None, extension=None):
        """
        Opens a SaveFileDialog with the specified settings and lets the user
        pick a file. Returns the full path of the picked file.

        Based on:
        https://discourse.mcneel.com/t/rhino-ui-openfiledialog-showopendialog-
        with-multiselect-true-not-working/40068
        """

        fd = Rhino.UI.SaveFileDialog()
        if title:
            fd.Title = title
        if filter:
            fd.Filter = filter
        if folder:
            fd.InitialDirectory = folder
        if filename:
            fd.FileName = filename
        if extension:
            fd.DefaultExt = extension
        if fd.ShowSaveDialog():
            return normpath(abspath(fd.FileName))

        # return None if something goes wrong while picking the file
        return None

    def RunScript(self, Save: bool, GCODE: System.Collections.Generic.List[object]):
        # on button click, execute save
        if Save and GCODE:
            if not GCODE or GCODE == [None]:
                Rhino.UI.Dialogs.ShowMessage("No GCODE to save!", "Warning")
                return
            fp = self.SaveFileDialog(
                                "Save GCODE File",
                                "GCODE Files (*.gcode)",
                                dirname(ghenv.Component.OnPingDocument().FilePath),
                                None,
                                ".gcode")
            # write gcode to selected file
            if fp:
                # mac os file extension fix
                if not fp.endswith(".gcode"):
                    fp = fp + ".gcode"
                # write every line to file
                with open(fp, "w") as f:
                    for l in GCODE:
                        f.write(l + "\n")
                # report success
                rml = Grasshopper.Kernel.GH_RuntimeMessageLevel.Remark
                ghenv.Component.AddRuntimeMessage(rml,
                                       ("Successfully wrote {0} "
                                        "lines to {1}!".format(len(GCODE),
                                                               basename(fp))))
                return
        elif not GCODE:
            rml = Grasshopper.Kernel.GH_RuntimeMessageLevel.Warning
            ghenv.Component.AddRuntimeMessage(rml,
                                   "Parameter GCODE failed to collect data!")
            return
