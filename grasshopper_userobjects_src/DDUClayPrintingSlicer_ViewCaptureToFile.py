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
    Author: Max Eschenbach (based on a GhPython Script by Anders Holden Deleuran)
    License: MIT License
    Version: 250314
    """

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
        docpath = ghdoc.Path
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
        ghDef = ghdoc.Name.strip("*")
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
            sc.doc = ghdoc
            return path
        except Exception as e:
            print(e)
            sc.doc = ghdoc
            raise Exception(" Capture failed, check the path")

    def RunScript(self,
            Toggle: bool,
            Width: int,
            Height: int,
            BackgroundColor: System.Drawing.Color,
            Grid: bool,
            WorldAxes: bool,
            CPlaneAxes: bool,
            OpenFile: bool):
        # Set background color
        if BackgroundColor:
            Rhino.ApplicationSettings.AppearanceSettings.ViewportBackgroundColor = BackgroundColor
        # Capture
        Path = Grasshopper.DataTree[object]()
        if Toggle:
            if not Width:
                Width = 1920
            if not Height:
                Height = 1080
            capFolder = self.checkOrMakeFolder()
            fileName = self.makeFileName()
            try:
                path = os.path.join(capFolder, fileName + ".png")
                Path = self.captureActiveViewToFile(Width, Height, path, Grid, WorldAxes, CPlaneAxes)
                if OpenFile:
                    os.startfile(path)
            except Exception as e:
                print(e)
                raise Exception(" Capture failed, save the GH definition")
        # return outputs if you have them; here I try it for you:
        return Path
