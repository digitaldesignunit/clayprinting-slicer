import System
import Rhino
import Grasshopper
import rhinoscriptsyntax as rs

# GHENV COMPONENT SETTINGS
ghenv.Component.Name = "PrintingTimeInfo"
ghenv.Component.NickName = "PrintingTimeInfo"
ghenv.Component.Category = "DDUClayPrintingSlicer"
ghenv.Component.SubCategory = "9 Utilities"


class PrintingTimeInfo(Grasshopper.Kernel.GH_ScriptInstance):
    """
    Author: Max Benjamin Eschenbach
    License: MIT License
    Version: 250317
    """
    def RunScript(self, Hours: int, Minutes: int, Seconds: int):
        InfoString = ''
        if Hours > 0:
            InfoString += '{0} hours'
        if Minutes > 0:
            if Hours > 0:
                InfoString += ', '
            InfoString += '{1} minutes'
        if Seconds > 0:
            if Hours > 0 or Minutes > 0:
                InfoString += ', '
            InfoString += '{2} seconds'
        InfoString += '.'
        return InfoString
