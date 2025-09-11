# PYTHON STANDARD LIBRARY IMPORTS
import os
import datetime

# RHINO SDK IMPORTS
import System
import Rhino
import Grasshopper
import scriptcontext as sc

# GHENV COMPONENT SETTINGS
ghenv.Component.Name = "ViewCaptureToFile"
ghenv.Component.NickName = "ViewCaptureToFile"
ghenv.Component.Category = "DDUClayPrintingSlicer"
ghenv.Component.SubCategory = "8 Visualisation"


class ViewCaptureToFile(Grasshopper.Kernel.GH_ScriptInstance):
    """
    Author: Anders Holden Deleuran (updated 2025 by Max Benjamin Eschenbach)
    License: MIT License
    Version: 250902
    Description: Captures the active Rhino view to an image file
    """

    def __init__(self):
        super().__init__()
        # initialize props
        self.Component = ghenv.Component  # type: ignore[reportUnedfinedVariable] # NOQA
        self.InputParams = self.Component.Params.Input
        self.OutputParams = self.Component.Params.Output

    def _addRemark(self, msg: str = ''):
        """Add a remark message to the component runtime messages."""
        rml = self.Component.RuntimeMessageLevel.Remark
        self.AddRuntimeMessage(rml, msg)

    def _addWarning(self, msg: str = ''):
        """Add a warning message to the component runtime messages."""
        rml = self.Component.RuntimeMessageLevel.Warning
        self.AddRuntimeMessage(rml, msg)

    def _addError(self, msg: str = ''):
        """Add an error message to the component runtime messages."""
        rml = self.Component.RuntimeMessageLevel.Error
        self.AddRuntimeMessage(rml, msg)

    def checkOrMakeFolder(self):
        """
        Check/makes a "Captures" folder in the GH def folder and a subfolder
        in the format 'YYMMDD'.
        """
        # construct timestamp for subfolder
        date = datetime.date.today()
        yy = str(date.year)[-2:]
        mm = str(date.month).zfill(2)
        dd = str(date.day).zfill(2)
        timestamp = yy + mm + dd
        docpath = ghdoc.Path  # type: ignore[reportUnedfinedVariable] # NOQA
        if docpath:
            folder = os.path.dirname(docpath)
            captureFolder = folder + "\\Captures\\" + timestamp
            if not os.path.isdir(captureFolder):
                os.makedirs(captureFolder)
            return captureFolder

    def makeFileName(self):
        """ Make a string with the gh def name + current hourMinuteSecond """
        # Make hour minute seconds string
        n = datetime.datetime.now()
        ho, mt, sc = str(n.hour), str(n.minute), str(n.second)
        if len(ho) == 1:
            ho = "0" + ho
        if len(mt) == 1:
            mt = "0" + mt
        if len(sc) == 1:
            sc = "0" + sc
        hms = ho + mt + sc
        # Get name of GH def
        ghDef = ghdoc.Name.strip("*")  # type: ignore[reportUnedfinedVariable] # NOQA
        # Concatenate and return
        fileName = ghDef + "_" + hms
        return fileName

    def captureActiveViewToFile(
            self,
            width,
            height,
            path,
            grid,
            worldAxes,
            cplaneAxes):
        """
        Captures the active view to an image file at the path.
        Path looks like this:
        "C:\\Users\\user\\Desktop\\Captures\\foo_bar.png"
        """
        # Set the script context to the current Rhino doc
        sc.doc = Rhino.RhinoDoc.ActiveDoc
        # Get the active view and set image dimensions
        activeView = sc.doc.Views.ActiveView
        # Perform the capture
        try:
            viewcap = Rhino.Display.ViewCapture()
            viewcap.DrawGrid = grid
            viewcap.DrawGridAxes = worldAxes
            viewcap.DrawAxes = cplaneAxes
            viewcap.Height = height
            viewcap.Width = width
            imageCap = viewcap.CaptureToBitmap(activeView)
            System.Drawing.Bitmap.Save(imageCap, path)
            Rhino.RhinoApp.WriteLine(path)
            sc.doc = ghdoc  # type: ignore[reportUnedfinedVariable] # NOQA
            return path
        except Exception as e:
            sc.doc = ghdoc  # type: ignore[reportUnedfinedVariable] # NOQA
            raise Exception(f'Capture failed, check the path: {str(e)}')

    def RunScript(self,
            Toggle: bool,
            Width: int,
            Height: int,
            BackgroundColor: System.Drawing.Color,
            Grid: bool,
            WorldAxes: bool,
            CPlaneAxes: bool,
            OpenFile: bool):
        """
        Main execution method for capturing the active view to a file.

        Args:
            Toggle: Boolean to execute the capture operation
            Width: Image width in pixels (default: 1920)
            Height: Image height in pixels (default: 1080)
            BackgroundColor: Background color for the viewport
            Grid: Show grid in capture
            WorldAxes: Show world axes in capture
            CPlaneAxes: Show construction plane axes in capture
            OpenFile: Open the captured file after creation

        Returns:
            Path to the captured image file
        """
        # Initialize param descriptions (this has to be done in RunScript)
        self.InputParams[0].Description = (
            'Toggle to execute the view capture operation'
        )
        self.InputParams[1].Description = (
            'Image width in pixels (default: 1920 if not provided)'
        )
        self.InputParams[2].Description = (
            'Image height in pixels (default: 1080 if not provided)'
        )
        self.InputParams[3].Description = (
            'Background color for the viewport capture'
        )
        self.InputParams[4].Description = (
            'Show grid in the captured image'
        )
        self.InputParams[5].Description = (
            'Show world axes in the captured image'
        )
        self.InputParams[6].Description = (
            'Show construction plane axes in the captured image'
        )
        self.InputParams[7].Description = (
            'Open the captured file after creation'
        )

        # Initialize output param descriptions
        self.OutputParams[0].Description = (
            'Path to the captured image file'
        )

        # Set up output trees and results
        Path = Grasshopper.DataTree[System.Object]()

        if not Toggle:
            self.Component.Message = (
                'Ready to capture view (toggle to execute)'
            )
            return Path

        try:
            # Set background color if provided
            if BackgroundColor:
                settings = Rhino.ApplicationSettings.AppearanceSettings
                settings.ViewportBackgroundColor = BackgroundColor
                self._addRemark('Background color set for capture')

            # Set default dimensions if not provided
            if not Width:
                Width = 1920
            if not Height:
                Height = 1080

            self.Component.Message = 'Preparing capture...'

            # Create capture folder and filename
            capFolder = self.checkOrMakeFolder()
            fileName = self.makeFileName()
            path = os.path.join(capFolder, fileName + '.png')

            self.Component.Message = 'Capturing view...'

            # Perform the capture
            captured_path = self.captureActiveViewToFile(
                Width, Height, path, Grid, WorldAxes, CPlaneAxes
            )

            # Add to output
            ghp = Grasshopper.Kernel.Data.GH_Path(0)
            Path.Add(captured_path, ghp)

            # Open file if requested
            if OpenFile:
                os.startfile(path)
                self._addRemark('File opened after capture')

            self.Component.Message = f'View captured: {fileName}.png'
            self._addRemark(f'Successfully captured view to {captured_path}')

            return Path

        except Exception as e:
            msg = f'Capture failed: {str(e)}'
            self._addError(msg)
            self.Component.Message = msg
            return Path
