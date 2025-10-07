# clayprinting-slicer

![Python](https://img.shields.io/badge/Python-3.8-blue.svg)
![License](https://img.shields.io/badge/License-MIT-blue.svg)
[![DOI]]

Open-Source Grasshopper-based slicer, primarily for printing clay on the WASP40100.

## Contents

- [Introduction](#introduction)
- [Origins](#origins)
  - [Slicing Software for 3D-Clay Printing](#slicing-software-for-3d-clay-printing)
  - [An Open Grasshopper-Based Slicer for Clay 3d-printing](#an-open-grasshopper-based-slicer-for-clay-3d-printing)
  - [References](#references)
- [Installation](#installation)
- [Dependencies](#dependencies)
- [Learn](#learn)
- [Contribute](#contribute)
- [Credits](#credits)

## Introduction

This is an experimental slicer for 3d-printing, mainly developed for printing clay on the WASP40100 LDM printer. Its main part is an almost self-contained [Grasshopper](https://www.rhino3d.com/features/#grasshopper) definition with just a [single dependency](#dependencies).

The project was started in 2022 at DDU internally to create a template for research and experiments with our clay 3d-printer - with the aim that it should also be incorporated into teaching contexts for a student audience with widely varying Rhino/Grasshopper experience. While there were and are plenty of established softwares and already developed plug-ins out there (today even more so than back then), we felt none of the existing solutions fit our case well. You can read up on some of the details and alternatives in the [Origins](#origins) section.

## Origins

### Slicing Software for 3D-Clay Printing
Most slicing software originates in the FFF (fused filament fabrication) domain, where extrusion dynamics, retraction, and toolpath strategies are optimized for thermoplastics. 3D-Clay Printing (also called large-deposition modeling, LDM) introduces different constraints: pressure-driven extrusion with lag, limited or no retraction, thicker extrusion beads, ideally continuous print paths. While off-the-shelf slicers such as Cura or PrusaSlicer are powerful and open source, they remain primarily tuned to filament and their abstractions do not align well with experimental LDM research and teaching.

In the architectural and academic context, several more specialized approaches have emerged:
- [COMPAS_SLICER](https://github.com/compas-dev/compas_slicer) [[1](#references)] is an open-source Python package developed within the [COMPAS](https://github.com/compas-dev/compas) [[2](#references)] framework. It supports planar and non-planar slicing, metadata-rich print-points, and Grasshopper visualization. While algorithmically advanced, its dual Python/Grasshopper workflow can be a barrier in teaching contexts where students are less proficient in the computational design domain and not experienced with Python.
- [Termite](https://www.food4rhino.com/en/app/termite), developed by Julian Jauk in the context of his PhD [[3](#references)], is a Grasshopper plugin tailored to LDM. It offers clay-specific controls and has been applied in research on reinforcement and co-extrusion. However, to our knowledge it is distributed as a compiled Grasshopper binary, without source code, limiting transparency and the possibility for modification in teaching and collaborative research.
- [Silkworm](https://www.food4rhino.com/en/app/silkworm), an earlier Grasshopper tool, translates curves into G-code and enables entirely custom toolpaths. While powerful for experimentation, it requires users to design path logic themselves, rather than providing a full slicing workflow.

### An Open Grasshopper-Based Slicer for Clay 3D-Printing
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

### References

[1] Mitropoulou, I., Burger, J., 2020. COMPAS_SLICER: Slicing functionality for COMPAS. Open-source Software. Available online: https://github.com/compas-dev/compas_slicer

[2] Mele, T.V., others, many, 2017. COMPAS: A framework for computational research in architecture and structures. https://doi.org/10.5281/zenodo.2594510

[3] Jauk, J., 2024. Advancing 3D Printing of Clay in Architecture. Available online: https://www.researchgate.net/publication/378822643_Advancing_3D_Printing_of_Clay_in_Architecture

## Installation

- **Install Clipper2GH.** It is a dependency for the slicer (see [dependencies](#dependencies))

- Go the the Releases page and download the latest release.
- You should get a `*.zip` file containing:
    - A Rhino/Grasshopper file duo (`YYMMDD_DDU_Clay3DPrinting_Slicer_RELEASE.3dm` & `DDU_Clay3DPrinting_Slicer_RELEASE.gh`). This is the main slicer definition!
    - A `UserObjects` folder, containing all the Python & C# scripts within the slicer script, exported as Grasshopper UserObjects.

### If you only want to use the slicer definition...
...open the Rhino file and the corresponding Grasshopper file, look at the examples and you're good to go!

### If you want to use the scripts as UserObjects...
...copy/move the `*.ghuser` files from the downloaded `UserObjects` folder to your Grassoppper UserObjects folder:
- on Windows: `%APPDATA%\Grasshopper\UserObjects`
- on OSX: ...easiest to access using `File -> Special Folders -> UserObjects Folder` in Grasshopper

(...don't forget to unblock them!)

## Dependencies

- Clipper2GH 1.2.6 (Install via Rhino 8 PackageManager: rhino8://package/search?name=Clipper2GH / [Food4Rhino](https://www.food4rhino.com/en/app/clipper2gh) / [GitHub](https://github.com/seghier/Clipper2GH), based on [Clipper2](https://github.com/AngusJohnson/Clipper2))

## Learn

For an introduction on how to get started with the slicer, we have the following YouTube tutorials.

<p align="center">
  <a href="https://www.youtube.com/watch?v=xtB6QfOa1HQ/" target="_blank">
    <img src="resources/250416_3DClayPrintingSlicer_Introduction_Thumbnail.jpg" alt="Tutorial 1: Introduction to the Slicer" width="480" />
    <br>
    <b>Tutorial 1: Introduction to the Slicer</b>
  </a>
</p>


## Contribute

Found a Bug? Have an idea? Know how to solve something we've been chewing on for ages? Feedback and contributions are always welcome! Feel free to look at our [code of conduct](CODE_OF_CONDUCT.md) for contributions.

## Credits

**Author: [Max Benjamin Eschenbach](https://github.com/fstwn)**  
**Contributors:** [Nadja Gaudillière-Jami](https://ngj.fyi/), Mirko Dutschke, Iyad Ghazal  

**[Digital Design Unit (DDU)](https://www.dg.architektur.tu-darmstadt.de/)**  
*Prof. Dr.-Ing. Oliver Tessmann*  
Fachbereich Architektur  
Technical University of Darmstadt

- Original GCODE Generator Script by [Tom Svilans](https://github.com/tsvilans) ([CITA Copenhagen](https://github.com/CITA-cph)); adapted by [Max Benjamin Eschenbach](https://github.com/fstwn), 2023
- Original Heat Method Script by Daniel Piker ([McNeel Discourse Link](https://discourse.mcneel.com/t/heat-method/105135)); adapted for Non-Planar Slicing by [Max Benjamin Eschenbach](https://github.com/fstwn), 2023
- Original "DefinitionDependencies" GhPython Component by [Anders Holden Deleuran](https://github.com/AndersDeleuran); adapted by [Max Benjamin Eschenbach](https://github.com/fstwn), 2025
- Original "ViewCaptureToFile" GhPython Component by [Anders Holden Deleuran](https://github.com/AndersDeleuran); adapted by [Max Benjamin Eschenbach](https://github.com/fstwn), 2025
- "MeshPipeChromo" C# Component for fast mesh piping by Cameron Newnham ([Chromodoris Plugin](https://github.com/camnewnham/ChromodorisGH)), Copyright 2015-2016; Licensed and distributed under GNU GPL
- The definition uses some Components taken from [Xylinus](https://www.food4rhino.com/en/app/xylinus-novel-control-3d-printing#) by Ryan Hoover (Independent Studio / MICA dFab / BUGSS)

