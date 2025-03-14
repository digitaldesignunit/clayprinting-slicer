import System
import Rhino
import Grasshopper
import rhinoscriptsyntax as rs

# GHENV COMPONENT SETTINGS
ghenv.Component.Name = "AdjustSeamInfo"
ghenv.Component.NickName = "AdjustSeamInfo"
ghenv.Component.Category = "DDUClayPrintingSlicer"
ghenv.Component.SubCategory = "9 Utilities"

class AdjustSeamInfo(Grasshopper.Kernel.GH_ScriptInstance):
    """
    Author: Max Benjamin Eschenbach
    License: MIT License
    Version: 250314
    """
    def RunScript(self,
            RandomizeSeam: bool,
            SpiralizeContours: bool,
            GeometryIsBranching: bool):
        GATE = 0
        if not RandomizeSeam:
            GATE = 0
        elif RandomizeSeam and not SpiralizeContours:
            GATE = 2
        elif RandomizeSeam and GeometryIsBranching:
            GATE = 2
        else:
            GATE = 1
        return GATE
