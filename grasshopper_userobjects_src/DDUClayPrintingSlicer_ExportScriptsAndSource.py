import System
import Rhino
import Grasshopper

import os
from pprint import pprint

import GhPython as ghpy
import ScriptComponents as scomp
import RhinoCodePluginGH as rcpgh

# GHENV COMPONENT SETTINGS
ghenv.Component.Name = "ExportScriptsAndSource"
ghenv.Component.NickName = "ExportScriptsAndSource"
ghenv.Component.Category = "DDUClayPrintingSlicer"
ghenv.Component.SubCategory = "0 Development"


class ExportScriptsAndSource(Grasshopper.Kernel.GH_ScriptInstance):
    """
    Author: Max Benjamin Eschenbach (based on a Python Script by Anders Holden Deleuran)
    License: MIT License
    Version: 250317
    """

    def get_source_version(self, source):
        """
        Attempts to get the first instance of the word "version" (or, "Version")
        in a multi line string. Then attempts to extract an integer from this line
        where the word "version" exists. So format version YYMMDD in the
        docstring like so (or any other integer system):
            Version: 160121
        """
        # Get first line with version in it
        src_lower = source.lower()
        version_str = [ln for ln in src_lower.split('\n') if "version" in ln]
        if version_str:
            # Get the first substring integer and return it
            version_int = [int(s) for s in version_str[0].split() if s.isdigit()]
            if version_int:
                return int(version_int[0])
        else:
            return None

    def process_document_objects(self, ghdocument, verbose=False):
        """
        Processes GH_Document object and return a list of all script components
        """
        # get all document objects
        comps = list(ghdocument.Objects)

        # types of python components
        ghpycomp = ghpy.Component.ZuiPythonComponent
        ipycomp = rcpgh.Components.IronPython2Component
        py3comp = rcpgh.Components.Python3Component

        # types of csharp components
        cscomp = scomp.Component_CSNET_Script
        cs9comp = rcpgh.Components.CSharpComponent

        # cluster component
        clustercomp = Grasshopper.Kernel.Special.GH_Cluster

        # extract component type strings
        ghpycomp_str = str(ghpycomp).split("'")[1]
        cscomp_str = str(cscomp).split("'")[1]
        ipycomp_str = str(ipycomp).split("'")[1]
        py3comp_str = str(py3comp).split("'")[1]
        cs9comp_str = str(cs9comp).split("'")[1]
        clustercomp_str = str(clustercomp).split("'")[1]

        # define script types
        id_ghpycomp = 'GHPY'
        id_cscomp = 'CS'
        id_ipycomp = 'IPY2'
        id_py3comp = 'PY3'
        id_cs9comp = 'CS9'

        # init dict for script component storage
        script_components = {}

        # loop over all document components
        # store all component objects in a dictionary to check
        # - are all "same" scripts the same code?
        # - are there scripts without category or version attribute?
        # - separate old from new scripts, dont export old scripts!
        for obj in comps:
            # extract basic info
            name = obj.Name
            nickname = obj.NickName
            iguid = str(obj.InstanceGuid)
            # extract type string
            objtype_str = str(obj.GetType())
            # OLD GHPYTHON COMPONENT
            if objtype_str == ghpycomp_str:
                source = obj.Code
                if source:
                    print(f'Found Source for OLD GHPY Component "{nickname}" ({iguid})') if verbose else None
                    scriptcomp = [id_ghpycomp, nickname, name, obj, source]
                    script_components[iguid] = scriptcomp
            # OLD C# COMPONENT
            elif objtype_str == cscomp_str:
                source = obj.ScriptSource.ScriptCode
                if source:
                    print(f'Found Source for OLD CS Component "{nickname}" ({iguid})') if verbose else None
                    scriptcomp = [id_cscomp, nickname, name, obj, source]
                    script_components[iguid] = scriptcomp
            # NEW C#9 COMPONENT
            elif objtype_str == cs9comp_str:
                bres, source = obj.TryGetSource()
                if bres:
                    print(f'Found Source for CS9 Component "{nickname}" ({iguid})') if verbose else None
                    scriptcomp = [id_cs9comp, nickname, name, obj, source]
                    script_components[iguid] = scriptcomp
            # NEW IRONPYTHON COMPONENT
            elif objtype_str == ipycomp_str:
                bres, source = obj.TryGetSource()
                if bres:
                    print(f'Found Source for IPY2 Component "{nickname}" ({iguid})') if verbose else None
                    scriptcomp = [id_ipycomp, nickname, name, obj, source]
                    script_components[iguid] = scriptcomp
            # NEW PYTHON3 COMPONENT
            elif objtype_str == py3comp_str:
                bres, source = obj.TryGetSource()
                if bres:
                    print(f'Found Source for PY3 Component "{nickname}" ({iguid})') if verbose else None
                    scriptcomp = [id_py3comp, nickname, name, obj, source]
                    script_components[iguid] = scriptcomp
            # CLUSTER COMPONENT
            elif objtype_str == clustercomp_str:
                # RECURSIVELY STEP THROUGH CLUSTERS AND LOOK FOR SCRIPTS AGAIN...
                print(f'Processing CLUSTER "{nickname}" ({name}, {iguid}) ...') if verbose else None
                cluster_scripts = self.process_document_objects(obj.Document(''), verbose=verbose)
                # add cluster script dict to main dict
                script_components.update(cluster_scripts)

        return script_components

    def process_script_components(self, script_components: dict, set_category: str):
        """
        Process found script components and get unique components.
        """
        unique_script_components = {}

        OldScriptsDebug = []
        CategoryDebug = []
        VersionDebug = []
        InfoMessages = []

        for iguid, values in script_components.items():
            script_type, nickname, name, obj, source = values
            category = obj.Category
            version = self.get_source_version(source)
            script_id = nickname + ' ' + name
            # NOT SEEN YET SCRIPT COMPONENTS
            if script_id not in unique_script_components:
                if script_type == 'GHPY':
                    OldScriptsDebug.append(f'{script_type} - {nickname} ({name})')
                    OldScriptsDebug.append('    - IS AN OLD GHPYTHON SCRIPT!')
                elif script_type == 'CS':
                    OldScriptsDebug.append(f'{script_type} - {nickname} ({name})')
                    OldScriptsDebug.append('    - IS AN OLD C# SCRIPT!')
                if category != set_category:
                    if category == 'Maths':
                        CategoryDebug.append(f'{script_type} - {nickname} ({name})')
                        CategoryDebug.append(f'    - HAS NO CATEGORY ({category})!')
                    else:
                        CategoryDebug.append(f'{script_type} - {nickname} ({name})')
                        CategoryDebug.append(f'    - {category} OUT OF SET CATEGORY {set_category}!')
                if version is None:
                    VersionDebug.append(' '.join([script_type, nickname, name]))
                    VersionDebug.append('    - HAS NO VERSION!')
                else:
                    InfoMessages.append(f'{script_type} - {nickname} ({name})')
                    unique_script_components[script_id] = values
            # ALREADY SEEN SCRIPT COMPONENTS
            else:
                existing_obj = unique_script_components[script_id][3]
                existing_source = unique_script_components[script_id][4]
                existing_version = self.get_source_version(existing_source)
                if version is None:
                    VersionDebug.append(f'{script_type} - {nickname} ({name})')
                    VersionDebug.append('    - HAS NO VERSION!')
                elif existing_version is None and version is not None:
                    VersionDebug.append(f'{script_type} - {nickname} ({name})')
                    VersionDebug.append(f'    - VERSION {version} > ALREADY FOUND VERSION {existing_version}!')
                    # REPLACE FOUND "NONE" VERSION WITH NAMED VERSION!
                    raise
                elif version < existing_version:
                    VersionDebug.append(f'{script_type} - {nickname} ({name})')
                    VersionDebug.append(f'    - VERSION {version} < {existing_version}! Continuing...')
                    continue
                elif version > existing_version:
                    VersionDebug.append(f'{script_type} - {nickname} ({name})')
                    VersionDebug.append(f'    - VERSION {version} > ALREADY FOUND VERSION {existing_version}!')
                    # REPLACE THE OBJECT IN DICT WITH NEWER VERSION
                    raise
        return unique_script_components, OldScriptsDebug, CategoryDebug, VersionDebug, InfoMessages

    def export_scriptcomp_usrobj(self, scriptcomp, usrobjpath, iconpath=''):
        """
        Automates the creation of a GHPython user object. Based on this thread:
        http://www.grasshopper3d.com/forum/topics/change-the-default-values-for-userobject-popup-menu
        scriptcomp = [script_type, nickname, name, obj, source]
        """
        try:
            # Make a user object
            uo = Grasshopper.Kernel.GH_UserObject()
            # Get component object
            obj = scriptcomp[3]
            # Process icon
            if iconpath:
                obj.SetIconOverride(System.Drawing.Bitmap.FromFile(iconpath))
            uo.Icon = obj.Icon_24x24
            # Set its properties based on the GHPython component properties
            uo.BaseGuid = obj.ComponentGuid
            uo.Exposure = ghenv.Component.Exposure.primary
            uo.Description.Name = obj.Name
            uo.Description.Description = obj.Description
            uo.Description.Category = obj.Category
            uo.Description.SubCategory = obj.SubCategory
            # Set the user object data and save to file
            uo.SetDataFromObject(obj)
            uo.Path = os.path.join(usrobjpath, obj.Category + '_' + obj.Name + '.ghuser')
            uo.SaveToFile()
        except Exception as e:
            print(e)
            return False
        return True

    def export_scriptcomp_source(self, scriptcomp, srcpath):
        """
        Export the source code of a script component
        scriptcomp = [script_type, nickname, name, obj, source]
        """
        # Get code and lines of code
        script_type = scriptcomp[0]
        name = scriptcomp[2]
        obj = scriptcomp[3]
        source = scriptcomp[4]
        code = source.replace('\r', '')
        lines = code.splitlines()
        loc = len(lines)
        # Check/make source file folder
        srcpath = os.path.join(srcpath)
        if not os.path.isdir(srcpath):
            os.makedirs(srcpath)
        # Write code to file
        if script_type == 'PY3' or script_type == 'IPY2':
            ext = '.py'
        elif script_type == 'CS9':
            ext = '.cs'
        else:
            # DO NOT EXPORT OLD SCRIPTS!
            ext = None
        if ext:
            src_file = os.path.join(srcpath, obj.Category + '_' + name + ext)
            with open(src_file, 'w') as f:
                f.write(code)
        return loc

    def RunScript(self,
            RunComponentAnalysis: bool,
            ExportUserObjectsAndSource: bool,
            Category: str,
            UserObjFolder,
            SourceFolder,
            IconPath: str):
        # Init outputs
        OldScriptsDebug = Grasshopper.DataTree[object]()
        CategoryDebug = Grasshopper.DataTree[object]()
        VersionDebug = Grasshopper.DataTree[object]()
        InfoMessages = Grasshopper.DataTree[object]()

        # Iterate the canvas and get to the GHPython components
        grasshopper_document = ghenv.Component.OnPingDocument()
        usrobjpath = os.path.normpath(os.path.abspath(UserObjFolder))
        srcpath = os.path.normpath(os.path.abspath(SourceFolder))
        iconpath = os.path.normpath(os.path.abspath(IconPath))

        if RunComponentAnalysis:
            # loop over all objects on the grasshopper canvas
            # and identify the components to export scriptsource from
            script_components = self.process_document_objects(grasshopper_document, verbose=False)

            # - are all "same" scripts the same code?
            # - are there scripts without category or version attribute?
            # - separate old from new scripts, dont export old scripts!
            (unique_script_components,
             OldScriptsDebug,
             CategoryDebug,
             VersionDebug,
             InfoMessages) = self.process_script_components(script_components, Category)

        # HERE LOOP OVER UNIQUE COMPONENTS
        if ExportUserObjectsAndSource and RunComponentAnalysis:
            # - SAVE USEROBJECT
            # - SAVE SOURCE
            for script_id, scriptcomp in unique_script_components.items():
                loc = self.export_scriptcomp_source(scriptcomp, srcpath)
                res = self.export_scriptcomp_usrobj(scriptcomp, usrobjpath, iconpath)
                print(res, loc)
        elif ExportUserObjectsAndSource and not RunComponentAnalysis:
            rml = ghenv.Component.RuntimeMessageLevel.Warning
            ghenv.Component.AddRuntimeMessage(
                rml,
                'UserObjects and Source cannot be exported without running Component Analysis!')

        return OldScriptsDebug, CategoryDebug, VersionDebug, InfoMessages
