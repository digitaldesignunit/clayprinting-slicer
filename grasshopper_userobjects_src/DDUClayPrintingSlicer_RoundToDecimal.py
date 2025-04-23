# PYTHON STANDARD LIBRARY IMPORTS
from __future__ import division, print_function

# GHPYTHONLIB SDK IMPORTS
import Grasshopper
import System
import Rhino

# GHENV COMPONENT SETTINGS
ghenv.Component.Name = "RoundToDecimal"
ghenv.Component.NickName = "RTD"
ghenv.Component.Category = "DDUClayPrintingSlicer"
ghenv.Component.SubCategory = "9 Utilities"

class RoundToDecimal(Grasshopper.Kernel.GH_ScriptInstance):
    """
    Author: Max Benjamin Eschenbach
    License: MIT License
    Version: 250423
    """

    def RunScript(self,
            Numbers: System.Collections.Generic.List[float],
            DecimalPlaces: System.Collections.Generic.List[int]):
        
        if not DecimalPlaces:
            DecimalPlaces = [0]
        else:
            DecimalPlaces = list(DecimalPlaces)

        if Numbers and Numbers != [None]:
            rounded = []
            for i, num in enumerate(Numbers):
                # retrieve decimalplaces
                if i < len(DecimalPlaces):
                    dp = DecimalPlaces[i]
                else:
                    dp = DecimalPlaces[-1]
                # round number
                rounded.append(round(num, dp))
        else:
            rounded = Grasshopper.DataTree[object]()

        # return outputs if you have them; here I try it for you:
        return rounded
