import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs

# GHENV COMPONENT SETTINGS
ghenv.Component.Name = "AdditionalFloorInfo"
ghenv.Component.NickName = "AdditionalFloorInfo"
ghenv.Component.Category = "DDUClayPrintingSlicer"
ghenv.Component.SubCategory = "9 Utilities"

class AdditionalFloorInfo(Grasshopper.Kernel.GH_ScriptInstance):
    """
    Author: Max Benjamin Eschenbach
    License: MIT License
    Version: 250314
    """
    def RunScript(self,
            PrintingGeometryClosed: bool,
            AddFloorToOpenObjects: bool,
            PrintingGeometryHasLowerBoundary: bool):
        GATE = 0
        if PrintingGeometryClosed is True and AddFloorToOpenObjects is True:
            GATE = 1
        elif PrintingGeometryClosed is False and AddFloorToOpenObjects is True:
            if PrintingGeometryHasLowerBoundary is False:
                GATE = 2
            else:
                GATE = 3
        return GATE
