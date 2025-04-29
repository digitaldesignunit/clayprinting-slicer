# RHINO SDK IMPORTS
import System
import Rhino
import Grasshopper

# GHENV COMPONENT SETTINGS
ghenv.Component.Name = "AnalysisTrimCurves"
ghenv.Component.NickName = "AnalysisTrimCurves"
ghenv.Component.Category = "DDUClayPrintingSlicer"
ghenv.Component.SubCategory = "9 Utilities"

class AnalysisTrimCurves(Grasshopper.Kernel.GH_ScriptInstance):
    """
    Author: Max Benjamin Eschenbach
    License: MIT License
    Version: 250429
    """
    def RunScript(self,
            A: System.Collections.Generic.List[float],
            B: System.Collections.Generic.List[float],
            C: Rhino.Geometry.Curve):

        if not A or not B or not C:
            rml = ghenv.Component.RuntimeMessageLevel.Warning
            if not A:
                ghenv.Component.AddRuntimeMessage(
                    rml,
                    'Input Parameter A failed to collect Data!')
            if not B:
                ghenv.Component.AddRuntimeMessage(
                    rml,
                    'Input Parameter A failed to collect Data!')
            if not C:
                ghenv.Component.AddRuntimeMessage(
                    rml,
                    'Input Parameter A failed to collect Data!')
            return (Grasshopper.DataTree[object](),
                    Grasshopper.DataTree[object]())

        A = list(A)
        B = list(B)

        if len(A) != len(B):
            ghenv.Component.AddRuntimeMessage(
                ghenv.Component.RuntimeMessageLevel.Error,
                'List counts do not match!')
            return (Grasshopper.DataTree[object](),
                    Grasshopper.DataTree[object]())

        domains = []
        subcrvs = []
        # loop over all domains
        for i, ai in enumerate(A):
            first_bi = 0.0
            bi = B[i]
            # first list items
            if i == 0:
                if A[i] > B[i] and A[i] > 0.999:
                    A[i] = 0.0
                if B[-1] < A[-1] and B[-1] <= 0.001:
                    B[-1] = 1.0
                ai = (A[-1] + B[-1]) * 0.5
                bi = (A[i] + B[i]) * 0.5
                first_bi = bi
            # all regular list items
            elif i > 0 and i < len(A) - 1:
                ai = (A[i - 1] + B[i - 1]) * 0.5
                bi = (A[i] + B[i]) * 0.5
            # last list items
            elif i == len(A) - 1:
                ai = domains[-1].T1
                bi = domains[0].T0

            domain = Rhino.Geometry.Interval(ai, bi)
            domains.append(domain)

            subcrv = C.Trim(ai, bi)
            subcrvs.append(subcrv)

        return (subcrvs, domains)
