# Grasshopper Development

## Copy-Paste Info for Grasshopper Development

### Author & Version

```
Author: Max Benjamin Eschenbach
License: MIT License
Version: 250314
```

### Component Params (Python)
```
# set component params
ghenv.Component.Name = "GenerateGCODE";
ghenv.Component.NickName = "GenerateGCODE";
ghenv.Component.Category = "DDUClayPrintingSlicer";
ghenv.Component.SubCategory = "7 GCODE";
```

### Component Params (C#)
```
// set component params
this.Component.Name = "";
this.Component.NickName = "";
this.Component.Category = "DDUClayPrintingSlicer";

this.Component.SubCategory = "1 Slicing";
this.Component.SubCategory = "2 Slice Processing";;
this.Component.SubCategory = "3 Infill";

this.Component.SubCategory = "6 Analysis";
this.Component.SubCategory = "7 GCODE";
this.Component.SubCategory = "8 Visualisation";
this.Component.SubCategory = "9 Utilities";`
```