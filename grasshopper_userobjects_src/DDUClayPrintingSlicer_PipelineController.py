# PYTHON STANDARD LIBRARY IMPORTS
import time

# RHINO SDK IMPORTS
import Grasshopper
import System
import Rhino
import rhinoscriptsyntax as rs

# CUSTOM RHINO IMPORTS
import scriptcontext as sc
from scriptcontext import sticky as st

# ADDITIONAL IMPORTS
from System.Drawing import Color

# GHENV COMPONENT SETTINGS
ghenv.Component.Name = "PipelineController"
ghenv.Component.NickName = "PipelineController"
ghenv.Component.Category = "DDUClayPrintingSlicer"
ghenv.Component.SubCategory = "9 Utilities"


class PipelineController(Grasshopper.Kernel.GH_ScriptInstance):
    """
    Author: Max Benjamin Eschenbach
    License: MIT License
    Version: 250314
    """

    # UNIQUE STICKY KEY OR STORING LAYERNAMES
    LNKEY = str(ghenv.Component.InstanceGuid) + "___LAYERNAMES"
    EVKEY = str(ghenv.Component.InstanceGuid) + "___EVENTS"
    FLAG = str(ghenv.Component.InstanceGuid) + "___FLAG"

    # COMPONENT UPDATING -------------------------------------------------------

    def updateComponent(self):
        # define callback action
        def callBack(e):
            ghenv.Component.ExpireSolution(False)
        # schedule new solution
        ghdoc.ScheduleSolution(1, Grasshopper.Kernel.GH_Document.GH_ScheduleDelegate(callBack))

    # EVENT HANDLING -----------------------------------------------------------

    def subscribe_to(self, event, func, key):
        if self.EVKEY not in st:
            st[self.EVKEY] = {}
        if key not in st[self.EVKEY]:
            st[self.EVKEY][key] = event
        ukey = str(self.InstanceGuid) + "_" + key
        if ukey not in st:
            st[ukey] = func
            event += st[ukey]

    def unsubscribe_all(self):
        if self.EVKEY in st:
            for key in st[self.EVKEY]:
                ukey = str(ghenv.Component.InstanceGuid) + "_" + key
                if ukey in st:
                    st[self.EVKEY][key] -= st[ukey]
                    st.Remove(ukey)
        st[self.EVKEY] = {}

    def flagEvent(self, sender, e):
        st[self.FLAG] = True

    def updateEvent(self, sender, e):
        if st[self.FLAG] == True:
            st[self.FLAG] = False
            self.updateComponent()

    def unsubEvent(self, sender, e):
        self.unsubscribe_all()

    # LAYER SET HANDLING -------------------------------------------------------

    def CreateReferenceLayers(self, parentPrefix, refLayers, norefLayers, colours):
        # get timestamp
        ts = time.strftime("%y%m%d_%H-%M")
        # create parent layer name
        parentLayername = parentPrefix + ts
        ghenv.Component.Message = "Referenced: " + str(parentLayername)
        # check if parent layer exists
        sc.doc = Rhino.RhinoDoc.ActiveDoc
        exists = rs.IsLayer(parentLayername)
        if exists:
            ghenv.Component.AddRuntimeMessage(ghenv.Component.RuntimeMessageLevel.Remark,
                "Parent Layer already exists! Returning valid existing layers.")
            # get all children layers
            allchildren = rs.LayerChildren(parentLayername)
            # check all children layers for validity
            validchildren = [parentLayername + "::" + vc for vc in refLayers]
            realchildren = []
            for c in allchildren:
                if c in validchildren:
                    realchildren.append(c)
            # set sticky to real found child layers
            st[self.LNKEY] = realchildren

            # switch back to ghdoc
            sc.doc = ghdoc
            # return layer names
            return realchildren
        else:
            # switch to Rhino doc
            sc.doc = Rhino.RhinoDoc.ActiveDoc
            # create parent layer
            parentLayer = rs.AddLayer(parentLayername, Color.Black)
            print(parentLayername)
            # create referenced layers
            newLayers = []
            for i, rl in enumerate(refLayers + norefLayers):
                lay = rs.AddLayer(parentLayername + '::' + rl,
                                  colours[i])
                if rl in refLayers:
                    newLayers.append(lay)
            # add them to the sticky
            st[self.LNKEY] = newLayers
            # switch back to ghdoc
            sc.doc = ghdoc
            # return layer names/paths
            return newLayers

    def LoadCurrentLayers(self, refLayers):
        # switch to RhinoDoc
        sc.doc = Rhino.RhinoDoc.ActiveDoc
        # retrieve current layer
        cl = rs.CurrentLayer()
        # check childcount
        childcount = rs.LayerChildCount(cl)
        # if no children, layer has to be childlayer or unrelated
        if childcount == 0:
            # get parent layer
            parent = rs.ParentLayer(cl)
            # switch back to GhDoc
            sc.doc = ghdoc
            if parent:
                allvalid = [parent + "::" + lay for lay in refLayers]
                # set message and return all valid children
                ghenv.Component.Message = "Referenced: " + str(parent)
                st[self.LNKEY] = allvalid
                return allvalid
            else:
                st[self.LNKEY] = None
                ghenv.Component.Message = None
                return None
        # if no children, layer has to be parent layer
        elif childcount > 0:
            parent = cl
            # switch back to GhDoc and return all valid layers
            sc.doc = ghdoc
            if parent:
                allvalid = [parent + "::" + lay for lay in refLayers]
                # set message and return all valid children
                ghenv.Component.Message = "Referenced: " + str(parent)
                st[self.LNKEY] = allvalid
                return allvalid
        else:
            # switch back to GhDoc
            sc.doc = ghdoc
            st[self.LNKEY] = None
            return None

    def retrieveGeometry(self, layers):
        geometry = []
        # switch to rhinodoc
        sc.doc = Rhino.RhinoDoc.ActiveDoc
        # loop over layers
        for i, layer in enumerate(layers):
            objs = [rs.coercegeometry(obj) for obj in rs.ObjectsByLayer(layer)]
            if objs is None:
                objs = []
            for i, obj in enumerate(objs):
                if type(obj) == Rhino.Geometry.Point:
                    objs[i] = Rhino.Geometry.Point3d(obj.Location)
            geometry.append(objs)
        # switch back to ghdoc
        sc.doc = ghdoc
        return geometry

    def RunScript(self,
            DynamicUpdate: bool,
            CreateAndRef: bool,
            LoadAndRef: bool,
            ParentLayerPrefix: str,
            ReferenceLayers: list[str],
            AssistanceLayers: list[str],
            LayerColours: list[System.Drawing.Color]):
        # check for unique key in sticky dictionary
        if self.LNKEY not in st:
            st[self.LNKEY] = None
        # define output variables ----------------------------------------------
        AssemblyGeo = Grasshopper.DataTree[object]()
        CurrentLayers = []
        # subscribe to events for automatic component updating -----------------
        if DynamicUpdate:
            self.subscribe_to(Rhino.RhinoDoc.BeforeTransformObjects, self.flagEvent, "BeforeTransformObjects")
            self.subscribe_to(Rhino.RhinoDoc.DeleteRhinoObject, self.flagEvent, "DeleteRhinoObject")
            self.subscribe_to(Rhino.RhinoDoc.AddRhinoObject, self.flagEvent, "AddRhinoObject")
            # self.subscribe_to(Rhino.RhinoDoc.LayerTableEvent, self.flagEvent, "LayerTableEvent")
            self.subscribe_to(Rhino.RhinoDoc.UndeleteRhinoObject, self.flagEvent, "UndeleteRhinoObjects")
            self.subscribe_to(Rhino.RhinoApp.Idle, self.updateEvent, "Idle")
            self.subscribe_to(Rhino.RhinoDoc.CloseDocument, self.unsubEvent, "CloseDocument")
            self.subscribe_to(Rhino.RhinoDoc.NewDocument, self.unsubEvent, "NewDocument")
        else:
            self.unsubscribe_all()
        # catch missing layer colours ------------------------------------------
        laycount = len(ReferenceLayers + AssistanceLayers)
        if not LayerColours or len(LayerColours) == 0:
            LayerColours = [Color.Black] * laycount
        elif len(LayerColours) < len(ReferenceLayers + AssistanceLayers):
            addCols = [Color.Black] * (laycount - len(LayerColours))
            LayerColours.extend(addCols)
        # if create button is pressed, create new layers and reference them ----
        if CreateAndRef is True:
            CurrentLayers = self.CreateReferenceLayers(ParentLayerPrefix,
                                                       ReferenceLayers,
                                                       AssistanceLayers,
                                                       LayerColours)
        # if load button is pressed, load from the current rhino-layer ---------
        elif LoadAndRef is True:
            # retrieve the current layerset based on the active rhino layer
            CurrentLayers = self.LoadCurrentLayers(ReferenceLayers)
        # if no button is pressed, make some checks and run standard procedure -
        elif self.LNKEY in st and st[self.LNKEY] is not None:
            layer_names = st[self.LNKEY]
            CurrentLayers = layer_names
        # fall back and return if there are no layer names stored in sticky
        elif st[self.LNKEY] is None:
            try:
                # retrieve the current layerset based on the active rhino layer
                CurrentLayers = self.LoadCurrentLayers(ReferenceLayers)
            except Exception:
                return AssemblyGeo, CurrentLayers
        # retrieve all geometry
        if CurrentLayers:
            try:
                allgeometry = self.retrieveGeometry(CurrentLayers)
            except ValueError as errMsg:
                ghenv.Component.AddRuntimeMessage(ghenv.Component.RuntimeMessageLevel.Error,
                                       str(errMsg))
                allgeometry = None
        else:
            ghenv.Component.AddRuntimeMessage(ghenv.Component.RuntimeMessageLevel.Warning,
                "Could not load layerset. Check your current Rhino layer or " +
                "the controllers input parameters")
            allgeometry = None
        if allgeometry:
            for i, geo in enumerate(allgeometry):
                AssemblyGeo.AddRange(geo, Grasshopper.Kernel.Data.GH_Path(i))
        # return outputs
        return AssemblyGeo, CurrentLayers