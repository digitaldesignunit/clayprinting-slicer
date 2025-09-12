# clayprinting-slicer
Open-Source Grasshopper-based slicer for printing clay on the WASP40100 delta printer.

**Author: [Max Benjamin Eschenbach](https://github.com/fstwn)**  
**Contributors:** Mirko Dutschke, Iyad Ghazal  

**[Digital Design Unit (DDU)](https://www.dg.architektur.tu-darmstadt.de/)**  
*Prof. Dr.-Ing. Oliver Tessmann*  
Fachbereich Architektur  
Technical University of Darmstadt

## Slicing Software for 3D-Clay Printing
Most slicing software originates in the FFF (fused filament fabrication) domain, where extrusion dynamics, retraction, and toolpath strategies are optimized for thermoplastics. 3D-Clay Printing (also called large-deposition modeling, LDM) introduces different constraints: pressure-driven extrusion with lag, limited or no retraction, thicker extrusion beads, ideally continuous print paths. While off-the-shelf slicers such as Cura or PrusaSlicer are powerful and open source, they remain primarily tuned to filament and their abstractions do not align well with experimental LDM research and teaching.

In the architectural and academic context, several more specialized approaches have emerged:
- [COMPAS_SLICER](https://github.com/compas-dev/compas_slicer) is an open-source Python package developed within the COMPAS framework. It supports planar and non-planar slicing, metadata-rich print-points, and Grasshopper visualization. While algorithmically advanced, its dual Python/Grasshopper workflow can be a barrier in teaching contexts where students are less proficient in the computational design domain and not experienced with Python.
- Termite, developed by Julian Jauk in the context of his PhD, is a Grasshopper plugin tailored to LDM. It offers clay-specific controls and has been applied in research on reinforcement and co-extrusion. However, to our knowledge it is distributed as a compiled Grasshopper binary, without source code, limiting transparency and the possibility for modification in teaching and collaborative research.
- Silkworm, an earlier Grasshopper tool, translates curves into G-code and enables entirely custom toolpaths. While powerful for experimentation, it requires users to design path logic themselves, rather than providing a full slicing workflow.

## An Open Grasshopper-Based Slicer for Clay 3D-Printing
In response to these limitations, we developed a slicer that is fully open-source and distributed via GitHub. The repository contains:
- The complete Grasshopper definition as a template file as well as example projects that can be adapted
- Exported Grasshopper UserObjects for all Python and C# scripts within the Grasshopper definition
- The corresponding source code files for all UserObjects

This structure ensures transparency, reproducibility, and adaptability: students can use the template, modify one of the example projects, or they can inspect and modify the underlying code to understand and extend the slicing process, depending on their proficiency with computational design tools and scripting. The slicer is designed with the specific requirements of LDM with clay in mind:
- Flexible input handling, from “outside wall” meshes to complex wall structures with integrated features
- Custom curve injection, allowing custom curves to bypass slicing and serve as toolpath
- Non-planar slicing by interpolating gradients over tube-like mesh surfaces, robust to self-intersecting meshes (i.e. for interior wall structures)
- Multiple spiralization approaches to achieve continuous printing, also for non-planar toolpaths
- Simple infill strategies, sufficient for typical clay printing needs

Our slicer is not intended to replicate the algorithmic breadth of COMPAS_SLICER, it prioritizes accessibility and adaptability for teaching. Compared to Termite, it offers complete openness, with all logic being distributed in source form. The developed slicer is research-capable but first and foremost designed as a transparent, didactic tool that supports plug-and-play use, slight modification, and even deep exploration.

## Dependencies

- Clipper2GH (Install via Rhino 8 PackageManager: rhino8://package/search?name=Clipper2GH / [Food4Rhino](https://www.food4rhino.com/en/app/clipper2gh) / [GitHub](https://github.com/seghier/Clipper2GH), based on [Clipper2](https://github.com/AngusJohnson/Clipper2))

## Credits & References

- Original GCODE Generator Script by [Tom Svilans](https://github.com/tsvilans) ([CITA Copenhagen](https://github.com/CITA-cph)); adapted by [Max Benjamin Eschenbach](https://github.com/fstwn), 2023
- Original Heat Method Script by Daniel Piker ([McNeel Discourse Link](https://discourse.mcneel.com/t/heat-method/105135)); adapted for Non-Planar Slicing by [Max Benjamin Eschenbach](https://github.com/fstwn), 2023
- Original "DefinitionDependencies" GhPython Component by [Anders Holden Deleuran](https://github.com/AndersDeleuran); adapted by [Max Benjamin Eschenbach](https://github.com/fstwn), 2025
- Original "ViewCaptureToFile" GhPython Component by [Anders Holden Deleuran](https://github.com/AndersDeleuran); adapted by [Max Benjamin Eschenbach](https://github.com/fstwn), 2025
- "MeshPipeChromo" C# Component for fast mesh piping by Cameron Newnham ([Chromodoris Plugin](https://github.com/camnewnham/ChromodorisGH)), Copyright 2015-2016; Licensed and distributed under GNU GPL
- The definition uses some Components taken from [Xylinus](https://www.food4rhino.com/en/app/xylinus-novel-control-3d-printing#) by Ryan Hoover (Independent Studio / MICA dFab / BUGSS)

