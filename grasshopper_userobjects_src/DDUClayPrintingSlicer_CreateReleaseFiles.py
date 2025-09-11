#! python3
# venv: DDU_CSC
# r: requests==2.31.0
# r: numpy==1.26.4
# r: scipy==1.13.0
# r: scikit-learn==1.4.2

# PYTHON STANDARD LIBRARY IMPORTS ---------------------------------------------
import os
from datetime import datetime

# RHINO AND GH RELATED IMPORTS ------------------------------------------------
import System  # type: ignore[reportMissingImport] # NOQA
import Grasshopper  # type: ignore[reportMissingImport] # NOQA
import Rhino  # type: ignore[reportMissingImport] # NOQA
import scriptcontext as sc  # type: ignore[reportMissingImport] # NOQA

# GHENV COMPONENT SETTINGS ----------------------------------------------------
ghenv.Component.Name = 'CreateReleaseFiles'  # type: ignore[reportUnedfinedVariable] # NOQA
ghenv.Component.NickName = 'CreateReleaseFiles'  # type: ignore[reportUnedfinedVariable] # NOQA
ghenv.Component.Category = 'DDUClayPrintingSlicer'  # type: ignore[reportUnedfinedVariable] # NOQA
ghenv.Component.SubCategory = '0 Development'  # type: ignore[reportUnedfinedVariable] # NOQA


class CreateReleaseFiles(Grasshopper.Kernel.GH_ScriptInstance):
    """
    Author: Max Benjamin Eschenbach
    License: MIT License
    Version: 250911

    This component can be used to create a Grasshopper release file in a
    separate folder.

    It creates a copy of the current Grasshopper document in
    memory, removes all components that belong to a specified group (i.e.
    development components), and saves the modified document to a specified
    folder.

    The folder path can be relative, i.e. "..\\grasshopper_release".

    The filename can be specified, otherwise a timestamp-based filename will be
    generated.
    """

    def __init__(self):
        super().__init__()
        # initialize props
        self.Component = ghenv.Component  # type: ignore[reportUnedfinedVariable] # NOQA
        self.InputParams = self.Component.Params.Input
        self.OutputParams = self.Component.Params.Output

    def _addRemark(self, msg: str = ''):
        """Add a remark message to the component."""
        rml = self.Component.RuntimeMessageLevel.Remark
        self.AddRuntimeMessage(rml, msg)

    def _addWarning(self, msg: str = ''):
        """Add a warning message to the component."""
        rml = self.Component.RuntimeMessageLevel.Warning
        self.AddRuntimeMessage(rml, msg)

    def _addError(self, msg: str = ''):
        """Add an error message to the component."""
        rml = self.Component.RuntimeMessageLevel.Error
        self.AddRuntimeMessage(rml, msg)

    def copy_document(self, source_doc):
        """
        Create a copy of the Grasshopper document in memory.

        Args:
            source_doc: The source Grasshopper document to copy

        Returns:
            GH_Document: A copy of the source document, or None if failed
        """
        try:
            if source_doc is None:
                self._addError('Source document is None')
                return None

            # Use the DuplicateDocument method to create a copy
            duplicate_doc = Grasshopper.Kernel.GH_Document.DuplicateDocument(
                source_doc
            )

            if duplicate_doc is None:
                self._addError('Failed to duplicate the document')
                return None

            self._addRemark('Successfully created document copy')
            return duplicate_doc

        except Exception as e:
            self._addError(f'Error copying document: {str(e)}')
            return None

    def clear_panels(self, doc, target_panels):
        """
        Find all GH_Panel instances in the document with one of the specified
        names and clear them.
        """
        panels_cleared = 0
        try:
            # loop over target panels and clear them
            for panel in target_panels:
                # UserText property holds the panel contents so we clear it
                panel.UserText = ''
                # we clear the volatile data as well
                panel.ClearData()
                panels_cleared += 1
        except Exception as e:
            self._addError(f'Error clearing panels: {str(e)}')
            return panels_cleared
        # return target groups and objects
        return panels_cleared

    def find_groups_and_objects(self, doc, group_name, panel_names=[]):
        """
        Find all groups and objects in the document with the specified name.
        """
        # get type for groups as string for verification
        grouptype = Grasshopper.Kernel.Special.GH_Group
        grouptype_str = str(grouptype).split("'")[1]
        # get type for panels as string for verification
        paneltype = Grasshopper.Kernel.Special.GH_Panel
        paneltype_str = str(paneltype).split("'")[1]
        # loop over all document objects
        doc_objects = doc.Objects
        target_groups = []
        target_objects = []
        target_panels = []
        try:
            for doc_obj in doc_objects:
                # ensure matching group name
                if doc_obj.NickName == group_name:
                    # ensure matching type
                    if str(doc_obj.GetType()) == grouptype_str:
                        target_groups.append(doc_obj)
                        for grp_obj in doc_obj.Objects():
                            target_objects.append(grp_obj)
                elif panel_names and doc_obj.NickName in panel_names:
                    # ensure matching type
                    if str(doc_obj.GetType()) == paneltype_str:
                        target_panels.append(doc_obj)
        except Exception as e:
            self._addError(f'Error finding groups and objects: {str(e)}')
            return None, None, None
        # return target groups and objects
        return target_groups, target_objects, target_panels

    def remove_objects_from_doc(self, doc, objects, groups=None):
        """
        Remove objects from the document.
        """
        components_removed = 0
        try:
            # loop over objects and remove them
            for obj in objects:
                doc.RemoveObject(obj, True)
                components_removed += 1
            # loop over groups and remove them
            if groups:
                for grp in groups:
                    doc.RemoveObject(grp, True)
                    components_removed += 1
        except Exception as e:
            self._addError(f'Error removing objects from document: {str(e)}')
            return 0
        # return number of components removed
        return components_removed

    def save_document(self, doc, save_path, filename=None):
        """
        Save the Grasshopper document to the specified path.

        Args:
            doc: The Grasshopper document to save
            save_path: The folder path where to save the document
            filename: Optional filename
                      (if None, generates timestamp-basedname)

        Returns:
            bool: True if successful, False otherwise
        """
        try:
            if doc is None:
                self._addError('Document is None, cannot save')
                return False, ''

            if not save_path:
                self._addError('Save path is empty')
                return False, ''

            # Handle relative paths - get the current GH
            # document directory as base
            current_doc = self.Component.OnPingDocument()
            if current_doc and current_doc.FilePath:
                current_dir = os.path.dirname(current_doc.FilePath)
                # Convert relative path to absolute path
                if not os.path.isabs(save_path):
                    save_path = os.path.normpath(
                        os.path.join(current_dir, save_path)
                    )
            else:
                # If no current document, use current working directory
                save_path = os.path.abspath(os.path.normpath(save_path))

            # Ensure the save directory exists
            if not os.path.exists(save_path):
                try:
                    os.makedirs(save_path, exist_ok=True)
                    self._addRemark(f'Created directory: {save_path}')
                except Exception as e:
                    self._addError(
                        f'Failed to create directory {save_path}: '
                        f'{str(e)}'
                    )
                    return False, ''

            # Generate filename if not provided
            if not filename:
                timestamp = datetime.now().strftime('%y%m%d_%H%M%S')
                filename = f'{timestamp}_ReleaseFile.gh'

            # Ensure filename has .gh extension
            if not filename.endswith('.gh'):
                filename += '.gh'

            # Create full file path
            full_path = os.path.join(save_path, filename)

            # Use GH_DocumentIO to save the document
            # (following SaveAndSaveGHX pattern)
            doc_io = Grasshopper.Kernel.GH_DocumentIO(doc)

            # Save the document
            success = doc_io.SaveQuiet(full_path)

            if success:
                self._addRemark(f'Document saved successfully: {full_path}')
                return True, full_path
            else:
                self._addError(f'Failed to save document to: {full_path}')
                return False, ''
        except Exception as e:
            self._addError(f'Error saving document: {str(e)}')
            return False, ''

    def validate_inputs(self, group_name, clear_panel_names, save_path):
        """
        Validate the input parameters.

        Args:
            group_name: The name of the group to remove
            save_path: The path where to save the document

        Returns:
            bool: True if inputs are valid, False otherwise
        """
        if not group_name or not group_name.strip():
            self._addError('Group name cannot be empty')
            return False

        if not save_path or not save_path.strip():
            self._addError('Save path cannot be empty')
            return False

        if not clear_panel_names:
            clear_panel_names = []
        for cpn in clear_panel_names:
            if not isinstance(cpn, str):
                self._addError('ClearPanelNames contains non-string')
                return False

        # Check if save path is valid
        try:
            # Handle relative paths - get the current
            # GH document directory as base
            current_doc = self.Component.OnPingDocument()
            if current_doc and current_doc.FilePath:
                current_dir = os.path.dirname(current_doc.FilePath)
                # Convert relative path to absolute path
                if not os.path.isabs(save_path):
                    normalized_path = os.path.normpath(
                        os.path.join(current_dir, save_path)
                    )
                else:
                    normalized_path = os.path.normpath(save_path)
            else:
                # If no current document, use current working directory
                normalized_path = os.path.abspath(os.path.normpath(save_path))
            # Check if the parent directory exists or can be created
            parent_dir = os.path.dirname(normalized_path)
            if parent_dir and not os.path.exists(parent_dir):
                # Try to create the parent directory
                try:
                    os.makedirs(parent_dir, exist_ok=True)
                except Exception:
                    self._addError(f'Cannot create directory: {parent_dir}')
                    return False
        except Exception as e:
            self._addError(f'Invalid save path: {str(e)}')
            return False

        return True

    def RunScript(self,
            Run: bool,
            RemoveGroupName: str,
            ClearPanelNames: System.Collections.Generic.List[str],
            ReleasePath: str,
            Filename: str):
        """
        Main execution method for the component.

        Args:
            Run: Boolean trigger to Run the operation
            RemoveGroupName: Name of the group to remove components from
            ReleasePath: Path where to save the modified document
            Filename: Optional filename for the saved document
        """
        # Initialize param descriptions# Initialize param descriptions
        self.InputParams[0].Description = (
            'Run the document copy, object removal, and save operation'
        )
        self.InputParams[1].Description = (
            'Name of the group(s) to remove for relese file'
        )
        self.InputParams[2].Description = (
            'List of names of optional GH_Panel objects to clear,'
            ' (i.e. for passwords)'
        )
        self.InputParams[3].Description = (
            'Folder path where to save the modified release document'
        )
        self.InputParams[4].Description = (
            'Optional filename for the saved document '
            '(without .gh extension)'
        )

        # Initialize output param descriptions
        self.OutputParams[0].Description = (
            'Success status of the operation (True/False)'
        )
        self.OutputParams[1].Description = (
            'Number of components removed'
        )
        self.OutputParams[2].Description = (
            'Full path of the saved document'
        )
        self.OutputParams[3].Description = (
            'Status message describing the operation result'
        )

        try:
            # Initialize output variables
            success = False
            components_removed = 0
            saved_file_path = ''
            status_message = ''

            # Check if execution is requested
            if not Run:
                status_message = 'Run is False - operation not performed'
                self.Component.Message = status_message
                return (success, components_removed, saved_file_path,
                        status_message)

            # Validate inputs
            if not self.validate_inputs(
                    RemoveGroupName, ClearPanelNames, ReleasePath):
                status_message = 'Input validation failed'
                self.Component.Message = status_message
                return (success, components_removed, saved_file_path,
                        status_message)

            # Get the current Grasshopper document
            current_doc = self.Component.OnPingDocument()
            if current_doc is None:
                status_message = 'No active Grasshopper document found'
                self._addError(status_message)
                self.Component.Message = status_message
                return (success, components_removed, saved_file_path,
                        status_message)

            # Step 1: Copy the current document
            self.Component.Message = 'Copying document...'
            copied_doc = self.copy_document(current_doc)
            if copied_doc is None:
                status_message = 'Failed to copy document'
                self.Component.Message = status_message
                return (success, components_removed, saved_file_path,
                        status_message)

            # Step 2: Find the specified group
            self.Component.Message = 'Finding groups...'
            (target_groups,
                target_objects,
                target_panels) = self.find_groups_and_objects(
                copied_doc,
                RemoveGroupName,
                ClearPanelNames
            )
            if target_groups is None or target_objects is None:
                status_message = 'Failed to find groups and objects'
                self.Component.Message = status_message
                return (success, components_removed, saved_file_path,
                        status_message)

            # Step 3: Clear Panels if ClearPanelNames were supplied
            if target_panels:
                self.Component.Message = 'Clearing Panels...'
                panels_cleared = self.clear_panels(copied_doc, target_panels)
                if panels_cleared == 0:
                    status_message = (
                        'No panels have been cleared!'
                    )
                    self._addWarning(status_message)

            # Step 4: Remove components from the group
            self.Component.Message = 'Removing components...'
            components_removed = self.remove_objects_from_doc(
                copied_doc,
                target_objects,
                target_groups
            )
            if components_removed == 0:
                status_message = (
                    'No components were removed from '
                    f'group "{RemoveGroupName}"'
                )
                self._addWarning(status_message)
            else:
                status_message = (
                    f'Removed {components_removed} components from group '
                    f'"{RemoveGroupName}"'
                )
            # Step 5: Save the modified document
            self.Component.Message = 'Saving document...'
            save_success, saved_file_path = self.save_document(
                copied_doc,
                ReleasePath,
                Filename
            )
            if save_success:
                saved_file_path = os.path.normpath(
                    os.path.abspath(saved_file_path)
                )
                success = True
                status_message += f' and saved to: {saved_file_path}'
                self.Component.Message = 'Operation completed successfully'
            else:
                status_message += ' but failed to save document'
                self.Component.Message = 'Save operation failed'

            return (success, components_removed, saved_file_path,
                    status_message)

        except Exception as e:
            error_msg = f'Unexpected error: {str(e)}'
            self._addError(error_msg)
            self.Component.Message = 'Operation failed'
            return (False, 0, '', error_msg)
