import System
import Rhino
import Grasshopper
import rhinoscriptsyntax as rs

# GHENV COMPONENT SETTINGS
ghenv.Component.Name = "NonPlanarSlicingInfo"
ghenv.Component.NickName = "NonPlanarSlicingInfo"
ghenv.Component.Category = "DDUClayPrintingSlicer"
ghenv.Component.SubCategory = "9 Utilities"

class NonPlanarSlicingInfo(Grasshopper.Kernel.GH_ScriptInstance):
    """
    Author: Max Benjamin Eschenbach
    License: MIT License
    Version: 250314
    """
    
    def RunScript(self,
            NPSlicingEnabled: bool,
            PrintingGeoIsMesh: bool,
            PrintingGeometryClosed: bool,
            InjectCustomCurves: bool):
        INFO = Grasshopper.DataTree[object]()
        if NPSlicingEnabled and not PrintingGeoIsMesh:
            INFO = '[SETTING IGNORED] Non Planar slicing is impossible for BREPs! To use this feature, activate remeshing.'
        elif NPSlicingEnabled and PrintingGeoIsMesh and not PrintingGeometryClosed:
            INFO = '[INFO] Performing Non Planar slicing...'
        elif NPSlicingEnabled and PrintingGeometryClosed:
            INFO = ['[SETTING IGNORED] Non Planar slicing is currently not available for closed objects!',
                    '[INFO] Performing parallel slicing...']
        elif InjectCustomCurves:
            INFO = '[INFO] No slicing is being done! Custom CURVES are injected...'
        else:
            INFO = '[INFO] Performing parallel slicing...'
        return INFO
