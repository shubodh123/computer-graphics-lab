# CS4104 Virtual Reality & Computer Graphics — Unity Case Studies

This directory contains production-ready, standalone Unity C# implementations for the **CS4104 CG & VR Case Studies**, with primary focus on **Case Study 3: Retail & Remote Commerce**.

All scripts are written to be self-contained: they programmatically construct all required 3D meshes, procedural textures, materials, directional lights, cameras, and interactive GUIs at runtime with **zero external asset dependencies**.

---

## 🛋️ Case Study 3: Retail and Remote Commerce (WebXR AR/VR)

**Script**: [`CaseStudy3_RetailCommerceDemo.cs`](CaseStudy3_Retail_Commerce/CaseStudy3_RetailCommerceDemo.cs) (also at repo root: `CaseStudy3_RetailCommerceDemo.cs`)  
**Custom Shader**: [`VelvetSheenShader.shader`](CaseStudy3_Retail_Commerce/VelvetSheenShader.shader)

### Theoretical & Algorithmic Concepts Demonstrated in Unity:
1. **Illumination & Color Models (Velvet in Sunlit Room)**:
   - **Charlie Sheen BRDF vs. Empirical Blinn-Phong**:
     Standard microfacet models assume shiny specular highlights that fail on velvet pile. In this demo, the Charlie Sheen BRDF simulates microscopic vertical fibers that absorb head-on light (creating a deep matte crimson) and strongly scatter grazing light (creating a luminous pinkish edge halo).
     The user can switch in real-time between **Charlie Sheen PBR** and **Blinn-Phong** to observe how Blinn-Phong produces artificial "plastic wrap" highlights.
   - **Environmental Light Estimation**:
     Directional Sunlight with soft shadow mapping and adjustable azimuth/elevation ($0^\circ\text{–}360^\circ$) simulates the sun moving through room windows.
     Ambient lighting uses Trilight spherical harmonics simulation (sky blue, wall bounce, hardwood floor bounce).
   - **Shadow Catcher Plane**:
     The virtual floor plane receives directional sunlight shadows, grounding the furniture naturally into the augmented environment.

2. **Texture & Bump / Normal Mapping (Chesterfield Leather Sofa)**:
   - **Zero Polygon Increase**: The leather sofa base and tufted backrest are built from simple low-polygon primitive boxes ($< 850$ triangles).
   - **Procedural Tangent-Space Normal Map**: Generates a runtime normal map texture (`RGBA32`) encoding micro-pores, wrinkling, and diamond tufted button dimples.
   - **Interactive Toggle**: The user can toggle **Normal Mapping: ENABLED vs. DISABLED** to observe how tangent-space normal perturbation transforms a flat plastic box into luxurious tufted leather without adding a single vertex.

3. **User Interaction & Window-to-Viewport Transformation**:
   - Demonstrates the complete forward and inverse mathematical viewing pipeline:
     $$\text{Screen Pixels } (x_s, y_s) \longrightarrow \text{NDC } [-1, 1] \longrightarrow \text{World Ray } \mathbf{r}(t) \longrightarrow \text{Ground Plane } (x_w, 0, z_w)$$
   - Clicking or tapping anywhere on the hardwood floor unprojects the 2D cursor and smoothly animates the selected furniture piece to that exact 3D spot with an animated AR reticle.
   - Real-time on-screen HUD displays pixel coordinates, NDC coordinates, and 3D world hit coordinates.

---

## 🏥 Companion Case Studies in Unity

### Case Study 1: Medical & Healthcare Training (Orthopedic VR Surgery)
* **Script**: [`CaseStudy1_MedicalHapticBoneDrillingDemo.cs`](CaseStudy1_MedicalHapticBoneDrillingDemo.cs)
* **Features**:
  * Multi-rate architecture: 1000 Hz haptic impedance loop decoupled from 90 Hz visual render loop.
  * God-Object / Virtual Proxy tracking and restoring force calculations:
    $$\mathbf{F} = -k_{\text{stiffness}}(\mathbf{x}_{\text{proxy}} - \mathbf{x}_{\text{HIP}}) - b_{\text{damping}}\mathbf{v}$$
  * Tactile "Plunge" Effect: As the drill penetrates through dense cortical bone ($k = 12\text{ kN/m}$, force $\sim 25\text{ N}$) into porous cancellous bone ($k = 2.5\text{ kN/m}$, force $\sim 5\text{ N}$), the on-screen live force graph illustrates the resistance drop.

### Case Study 2: Industrial Design & Architecture (1:1 Collaborative Automotive VR)
* **Script**: [`CaseStudy2_AutomotiveTransformDemo.cs`](CaseStudy2_AutomotiveTransformDemo.cs)
* **Features**:
  * 1:1 scale car wheel rim with central hub, spokes, and tire.
  * Interactive composite transformation matrix derivation in homogeneous coordinates:
    $$\mathbf{M} = \mathbf{T}(0, 0, 50\text{ mm}) \cdot \mathbf{R}(45^\circ) \cdot \mathbf{S}(1.5, 1.5, 1.5)$$
  * Displays the exact $4 \times 4$ composite matrix on-screen with real-time numeric evaluation.
  * Demonstrates Sutherland-Hodgman polygon clipping and Hi-Z occlusion culling metrics.

### Case Study 4: Behavioral Simulation & Crowd Dynamics (Metro Emergency Evacuation)
* **Script**: [`CaseStudy4_MetroCrowdEvacuationDemo.cs`](CaseStudy4_MetroCrowdEvacuationDemo.cs)
* **Features**:
  * 80+ autonomous pedestrian agents on an underground metro platform.
  * Helbing's Social Force Model (SFM): Self-propulsion force, psychological distance repulsion, and physical body compression with sliding friction.
  * Simulated Track Fire alarm event: Exit A is blocked by smoke/flames, panic contagion spreads, desired speed escalates from $1.34\text{ m/s}$ to $3.6\text{ m/s}$, producing bottleneck arching ("Faster-is-Slower" effect) followed by dynamic NavMesh rerouting to Emergency Exit B.

---

## 🚀 How to Run in Unity (Step-by-Step)

### Option A: Open or Create a Project in Unity (Unity 6 / 2022 / 2021)
1. Open **Unity Hub** and click **New Project** (or open an existing 3D project).
2. Choose **3D (Built-in or URP)** and name it (e.g., `CS4104_Unity_Demos`).
3. Drag the folder `Unity_Case_Studies/` (or individual `.cs` files) into the Unity Project's **`Assets/`** folder.
4. In Unity, create an empty GameObject in the scene:
   * **GameObject $\to$ Create Empty** (name it `DemoManager`).
5. Attach the desired script (e.g. drag `CaseStudy3_RetailCommerceDemo.cs` onto `DemoManager`).
6. Press the **Play (▶)** button!
   * Left-click on the floor to raycast and move furniture.
   * Right-click and drag to orbit the camera around the room.
   * Use on-screen buttons to toggle Charlie Sheen BRDF, Normal Mapping, and adjust sunlight angles.

### Option B: Archive Package
All scripts and shaders are also bundled in:
[`Unity_CS4104_Case_Studies_Full_Project.zip`](../Unity_CS4104_Case_Studies_Full_Project.zip)
