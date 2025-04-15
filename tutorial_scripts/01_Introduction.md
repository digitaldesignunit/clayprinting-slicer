# 🎬 Clayprinting Slicer Tutorial 01 – Introduction

## 👋 Introduction
Welcome to the Introduction Tutorial for the DDU 3D Clay Printing Slicer.

The Slicer is actually a Grasshopper definition.

And I will walk you through this Grasshopper definition for preparing geometry for the WASP 40100 clay 3D printer.

---

## 🧩 Before You Start
Ensure all necessary dependencies and plugins are installed.

We try to keep the dependencies on external plugins minimal, although sometimes it doesn’t work without them.

In this case, at the time of recording this video, you need the plugin **Clipper2GH**, which is available via Food4Rhino or the Rhino Package Manager.

Next, once you have downloaded the slicer, you will get a zip archive which you should unpack.

Inside the zip, you have one **Rhino template file**, and the **Grasshopper definition**, which is the slicer itself.

- Open the Rhino template file.
- Drag and drop the Grasshopper definition file into Rhino.

---

## 🗺️ Definition Overview
Press the zoom-out button to see the entire script.

Most of the script is designed to work in the background — only a few green-colored groups require user input.

So don’t be intimidated — I know it is a large script, but keep in mind:  
**You don’t need to understand the whole script — just how to apply and use it!**

If you are more advanced, of course you can dive in!

### 🎨 Color-coded structure:
- 🟩 Green: User interaction  
- ⚪ White / Gray: Geometry processing  
- 🔵 Blue: Visualization  
- 🟣 Violet: Output  
- 🟡 Yellow: Analysis  
- 💖 Pink: Control flow  
- 🟧 Orange: Advanced features  
- 🔴 Red: Do not touch – may break the script

---

## ⚙️ Settings & Parameters
All user settings are grouped at the far left of the Grasshopper canvas.

This includes:
- Input & geometry pipeline  
- Custom curve injection (advanced)  
- G-Code pre-processing (preconfigured, skip as beginner)  
- General slicing settings  
- Non-planar slicing (advanced)  
- Spiralization options  
- Floor/cap/infill settings  
- Analysis tools  
- Visualization configuration  
- G-Code export  

---

## 🖥️ Remote Control Panel
Go to **View > Remote Control Panel** in Grasshopper.

This panel mirrors all slicer settings and can be docked in Rhino.

You can close Grasshopper and control everything via the panel.

> If it breaks, just reopen Grasshopper — your settings are still there.

---

## 💬 Heads-Up Display (HUD)
In the Rhino viewport, if “No input geometry is present” appears:

- No geometry loaded = no slicing.
- Enable/disable HUD via the Visualization Settings (toggle: “Enable Heads-Up Display”).

---

## 🏺 First Geometry Setup
Example object: Simple vase → a single wall surface (open top and bottom).

Recommended starting point: one open surface or polysurface, no caps.

---

## 📦 Load Your Geometry
Go to **Input & Geometry Pipeline**.

- Click **Create and Reference** → Auto-generates a layer set (timestamped).
- Only care about the `printing_geometry` layer.
- Add your geometry to this layer.
- Ensure only one single poly surface is on this layer.
- Make this layer set active using the checkmark in Rhino.
- Click **Load and Reference** → This may take a moment to process.

---

## ✅ Loaded! What You Should See
Different stages in the viewport:

- Input geometry (as modeled)  
- Printing geometry (post-processed)  
- Printing layers (even slicing)  
- Print layer simulation  
- WASP 40100 print bed circle (40 cm diameter)  

---

## 📊 Analysis
Next thing we are going to look at is the **Overhang Analysis**.

- Enable it by going to **Analysis Tools** and activating the **Overhang Analysis Toggle**.
- A new stage in the viewport will appear and show you the results.

Interpretation:
- Parts with minimal overhang = green
- Increasing overhang = yellow → red
- Red = exceeds or comes close to the overhang threshold

> You can set the angle threshold — default is 30°.

From experience:  
**Overhangs ≥ 30° are difficult to print** without reinforcement, as they tend to collapse.

---

## 🧵 Seam Adjustments
Found under **Seam Adjustment** (just above Spiralization).

- Adjust seam position using a slider (0–1 range).
- Or enable **Randomize Seam** to distribute weak points.

---

## 🌀 Spiralization
Spiralization has its own dedicated settings.

It enables a continuous, smooth printing path — ideal for clay.

- It works by connecting individual layers with an interpolated ramp.
- Only possible for single wall objects.
- Not usable for designs with interior or branching details.

**Recommendation:**  
Design with spiralization in mind for best results.

> If Spiralization is OFF, be sure to adjust the seam to reduce print weaknesses.

---

## 🔄 Create GCODE & Simulate the Print
Time to create the actual GCODE!

- Go to **GCODE Generation, Simulation & Export**
- Activate the **Create GCODE Toggle**

You will now see:
- GCODE visualization in the simulation area
- Use **Visualize Printing Progress** slider to scroll through stages (0%–100%)

> Grasshopper sliders are smoother than Remote Panel sliders.

Enable **print simulation toggle** to see:
- 3D mesh visualization of printed GCODE  
- Use same progress slider to scroll through it

---

## ✅ Wrap-Up
If everything looks good like in the example:

- Click the **Save GCODE** button.
- Choose a location to save (e.g., desktop).

🎉 That concludes our introduction tutorial on the **DDU 3D Clay Printing Slicer**.

**I hope you learned something and enjoyed it.**  
**See you in the next tutorial!**
