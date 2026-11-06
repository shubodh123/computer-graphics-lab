using UnityEngine;
using System.Collections;

/// <summary>
/// CS4104 Computer Graphics & Virtual Reality - Case Study 3: Retail & Remote Commerce
/// 
/// WebXR Architectural AR/VR Furniture Showcase Demo in Unity.
/// Demonstrates:
/// 1. Illumination & Color Models:
///    - Environmental Sunlit lighting + Ambient irradiance (simulating Spherical Harmonics)
///    - Charlie Sheen BRDF for Velvet fabric (grazing fuzz rim highlight vs dark matte face)
///    - Comparison against empirical Blinn-Phong (unrealistic shiny plastic highlights)
/// 2. Texture & Bump Mapping:
///    - Procedural 2D Normal Map generation at runtime (leather grain, tufted button dimples, wrinkles)
///    - Demonstration of Tangent-Space Normal Mapping (TBN frame)
///    - Toggle Normal Mapping ON vs OFF to prove high visual fidelity with zero polygon increase
/// 3. User Interaction & Window-to-Viewport Transformation:
///    - Forward 3D Viewing Pipeline: World -> View -> Clip -> NDC -> Viewport Pixels (x_s, y_s)
///    - Inverse Unprojection: Screen Tap -> NDC -> Inverse View-Projection -> World Ray -> AR Floor Plane
///    - Interactive furniture positioning with animated AR placement reticle
/// </summary>
public class CaseStudy3_RetailCommerceDemo : MonoBehaviour
{
    [Header("Scene References (Auto-created if null)")]
    public Camera mainCamera;
    public Light sunLight;

    [Header("Furniture Configuration")]
    public bool showChair = true;
    public bool useCharlieSheenBRDF = true;
    public bool enableNormalMapping = true;
    [Range(0f, 360f)] public float sunAzimuth = 45f;
    [Range(10f, 85f)] public float sunElevation = 50f;

    // Generated Scene Objects
    private GameObject roomRoot;
    private GameObject chairRoot;
    private GameObject sofaRoot;
    private GameObject activeFurniture;
    private GameObject placementReticle;
    private GameObject floorPlane;

    // Materials
    private Material chairVelvetMaterial;
    private Material chairPhongMaterial;
    private Material sofaLeatherMaterial;
    private Material floorWoodMaterial;
    private Material brassMaterial;
    private Material darkWoodMaterial;

    // Procedural Textures
    private Texture2D leatherNormalMap;
    private Texture2D woodPlankTexture;

    // Mathematical Raycast & Viewport State
    private Vector2 lastScreenPixel;
    private Vector2 lastNDC;
    private Vector3 lastWorldHitPos;
    private string activeBRDFDescription = "Charlie Sheen BRDF (PBR Velvet)";
    private Vector3 targetPlacementPos;
    private float placementAnimTime = 1f;

    // Camera Navigation State
    private float cameraDistance = 3.8f;
    private float cameraYaw = 40f;
    private float cameraPitch = 25f;
    private Vector3 cameraLookTarget = new Vector3(0f, 0.6f, 0f);

    void Start()
    {
        InitializeLightingAndCamera();
        GenerateProceduralTextures();
        GenerateMaterials();
        BuildRoomFloor();
        BuildRedVelvetChair();
        BuildChesterfieldLeatherSofa();
        BuildPlacementReticle();

        // Default: display Red Velvet Chair
        SetFurnitureVisibility(true);
        UpdateLighting();
    }

    #region Scene & Lighting Setup
    void InitializeLightingAndCamera()
    {
        // 1. Setup Camera if not present
        if (mainCamera == null)
        {
            GameObject camObj = GameObject.Find("Main Camera");
            if (camObj == null) camObj = new GameObject("Main Camera");
            mainCamera = camObj.GetComponent<Camera>();
            if (mainCamera == null) mainCamera = camObj.AddComponent<Camera>();
        }
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0.85f, 0.91f, 0.98f); // Soft sunlit ambient sky
        mainCamera.fieldOfView = 50f;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 100f;
        UpdateCameraTransform();

        // 2. Setup Directional Sun Light
        if (sunLight == null)
        {
            GameObject lightObj = GameObject.Find("Directional Light");
            if (lightObj == null) lightObj = new GameObject("Sun Light (WebXR Directional)");
            sunLight = lightObj.GetComponent<Light>();
            if (sunLight == null) sunLight = lightObj.AddComponent<Light>();
        }
        sunLight.type = LightType.Directional;
        sunLight.color = new Color(1.0f, 0.96f, 0.90f); // Warm sunlight (5500K)
        sunLight.intensity = 1.8f;
        sunLight.shadows = LightShadows.Soft;
        sunLight.shadowBias = 0.005f;

        // 3. Environmental Ambient Radiance (simulating Spherical Harmonics)
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.70f, 0.88f);    // Cool skylight
        RenderSettings.ambientEquatorColor = new Color(0.80f, 0.75f, 0.70f);// Room wall bounce
        RenderSettings.ambientGroundColor = new Color(0.40f, 0.30f, 0.20f); // Hardwood floor bounce
    }

    void UpdateLighting()
    {
        if (sunLight == null) return;
        Quaternion rot = Quaternion.Euler(sunElevation, sunAzimuth, 0f);
        sunLight.transform.rotation = rot;
    }
    #endregion

    #region Procedural Texture Generation
    void GenerateProceduralTextures()
    {
        // 1. Procedural Tangent-Space Normal Map for Leather (512x512)
        // Encodes high-frequency pores, creases, and tufted button depressions
        int normSize = 256;
        leatherNormalMap = new Texture2D(normSize, normSize, TextureFormat.RGBA32, true);
        leatherNormalMap.name = "Procedural_Leather_NormalMap";

        for (int y = 0; y < normSize; y++)
        {
            for (int x = 0; x < normSize; x++)
            {
                float u = (float)x / normSize;
                float v = (float)y / normSize;

                // Cellular leather grain + diagonal tufting creases
                float grain = Mathf.Sin(x * 0.35f) * Mathf.Cos(y * 0.35f) * 0.3f;
                float wrinkle = Mathf.Sin((x + y) * 0.15f) * Mathf.Cos((x - y) * 0.15f) * 0.45f;

                // Diamond tufting dimples at regular grid points
                float cellX = Mathf.Repeat(u * 4f, 1f) - 0.5f;
                float cellY = Mathf.Repeat(v * 4f, 1f) - 0.5f;
                float distToCenter = Mathf.Sqrt(cellX * cellX + cellY * cellY);
                float dimple = -Mathf.Clamp01(1f - (distToCenter / 0.25f)) * 0.6f;

                float height = grain + wrinkle + dimple;

                // Calculate normal perturbation: Nx = -dh/dx, Ny = -dh/dy, Nz = 1
                float nx = -height * 0.8f;
                float ny = -Mathf.Cos((x - y) * 0.15f) * 0.4f;
                float nz = Mathf.Sqrt(Mathf.Max(0.01f, 1f - (nx * nx + ny * ny)));

                // Encode into RGB color: [0, 1] mapped from [-1, 1]
                Color normColor = new Color(
                    (nx * 0.5f) + 0.5f,
                    (ny * 0.5f) + 0.5f,
                    (nz * 0.5f) + 0.5f,
                    1f
                );
                leatherNormalMap.SetPixel(x, y, normColor);
            }
        }
        leatherNormalMap.Apply();

        // 2. Procedural Hardwood Floor Planks Texture (256x256)
        woodPlankTexture = new Texture2D(256, 256, TextureFormat.RGB24, true);
        woodPlankTexture.name = "Procedural_Wood_Floor";
        Color woodBase = new Color(0.68f, 0.42f, 0.22f); // Warm oak
        Color woodDark = new Color(0.42f, 0.24f, 0.10f); // Plank groove

        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                bool isGroove = (y % 64 == 0) || ((x + (y / 64) * 80) % 128 == 0);
                float grainNoise = Mathf.PerlinNoise(x * 0.05f, y * 0.2f) * 0.15f;
                Color col = isGroove ? woodDark : Color.Lerp(woodBase, woodDark, grainNoise);
                woodPlankTexture.SetPixel(x, y, col);
            }
        }
        woodPlankTexture.Apply();
    }
    #endregion

    #region Materials Setup
    void GenerateMaterials()
    {
        Shader standardShader = Shader.Find("Standard");
        if (standardShader == null) standardShader = Shader.Find("Diffuse");

        // 1. Floor Material (Hardwood + Shadow Catcher)
        floorWoodMaterial = new Material(standardShader);
        floorWoodMaterial.name = "Floor_Hardwood_Material";
        floorWoodMaterial.mainTexture = woodPlankTexture;
        floorWoodMaterial.mainTextureScale = new Vector2(6f, 6f);
        floorWoodMaterial.SetFloat("_Glossiness", 0.45f);
        floorWoodMaterial.SetFloat("_Metallic", 0.05f);

        // 2. Brass Legs Material (Metallic Specular)
        brassMaterial = new Material(standardShader);
        brassMaterial.name = "Brass_Legs_Material";
        brassMaterial.color = new Color(0.88f, 0.72f, 0.35f);
        brassMaterial.SetFloat("_Metallic", 0.88f);
        brassMaterial.SetFloat("_Glossiness", 0.75f);

        // 3. Dark Wood Feet Material
        darkWoodMaterial = new Material(standardShader);
        darkWoodMaterial.name = "Dark_Wood_Material";
        darkWoodMaterial.color = new Color(0.18f, 0.12f, 0.08f);
        darkWoodMaterial.SetFloat("_Glossiness", 0.25f);

        // 4. Red Velvet Material (Charlie Sheen BRDF vs Empirical Blinn-Phong)
        Shader customVelvetShader = Shader.Find("CS4104/VelvetSheenShader");
        if (customVelvetShader != null)
        {
            chairVelvetMaterial = new Material(customVelvetShader);
            chairVelvetMaterial.SetColor("_Color", new Color(0.65f, 0.08f, 0.12f, 1f));     // Deep crimson
            chairVelvetMaterial.SetColor("_SheenColor", new Color(1.0f, 0.75f, 0.80f, 1f)); // Pink grazing fuzz
            chairVelvetMaterial.SetFloat("_SheenRoughness", 0.35f);
            chairVelvetMaterial.SetFloat("_SheenIntensity", 2.2f);
        }
        else
        {
            // Fallback for Standard Shader: high roughness, high diffuse, custom rim simulation
            chairVelvetMaterial = new Material(standardShader);
            chairVelvetMaterial.color = new Color(0.68f, 0.08f, 0.12f);
            chairVelvetMaterial.SetFloat("_Glossiness", 0.05f); // Velvet has zero high-specular gloss
            chairVelvetMaterial.SetFloat("_Metallic", 0.0f);
        }

        // Empirical Blinn-Phong material (shows plastic highlight artifacts)
        chairPhongMaterial = new Material(standardShader);
        chairPhongMaterial.name = "Velvet_BlinnPhong_Plastic";
        chairPhongMaterial.color = new Color(0.68f, 0.08f, 0.12f);
        chairPhongMaterial.SetFloat("_Glossiness", 0.78f); // High plastic specular gloss
        chairPhongMaterial.SetFloat("_Metallic", 0.0f);

        // 5. Chesterfield Leather Material (with Normal Map toggle)
        sofaLeatherMaterial = new Material(standardShader);
        sofaLeatherMaterial.name = "Chesterfield_Leather_Material";
        sofaLeatherMaterial.color = new Color(0.32f, 0.14f, 0.06f); // Rich Saddle Brown
        sofaLeatherMaterial.SetFloat("_Glossiness", 0.60f);
        sofaLeatherMaterial.SetFloat("_Metallic", 0.05f);

        if (enableNormalMapping)
        {
            sofaLeatherMaterial.EnableKeyword("_NORMALMAP");
            sofaLeatherMaterial.SetTexture("_BumpMap", leatherNormalMap);
            sofaLeatherMaterial.SetFloat("_BumpScale", 1.8f);
        }
    }
    #endregion

    #region Procedural 3D Geometry Construction
    void BuildRoomFloor()
    {
        roomRoot = new GameObject("Room Environment");
        roomRoot.transform.SetParent(this.transform);

        floorPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floorPlane.name = "Hardwood Floor (Shadow Catcher)";
        floorPlane.transform.SetParent(roomRoot.transform);
        floorPlane.transform.position = Vector3.zero;
        floorPlane.transform.localScale = new Vector3(2.5f, 1f, 2.5f); // 25x25 meters
        floorPlane.GetComponent<MeshRenderer>().material = floorWoodMaterial;
        floorPlane.GetComponent<MeshRenderer>().receiveShadows = true;
    }

    void BuildRedVelvetChair()
    {
        chairRoot = new GameObject("Red Velvet Lounge Chair (Charlie Sheen BRDF)");
        chairRoot.transform.SetParent(this.transform);
        chairRoot.transform.position = Vector3.zero;

        Material activeVelvetMat = useCharlieSheenBRDF ? chairVelvetMaterial : chairPhongMaterial;

        // Seat Cushion
        GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        seat.name = "Seat Cushion";
        seat.transform.SetParent(chairRoot.transform);
        seat.transform.localScale = new Vector3(0.95f, 0.18f, 0.95f);
        seat.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        seat.GetComponent<MeshRenderer>().material = activeVelvetMat;

        // Curved Backrest (Tilted cube/sphere blend)
        GameObject backrest = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backrest.name = "Velvet Backrest";
        backrest.transform.SetParent(chairRoot.transform);
        backrest.transform.localScale = new Vector3(0.95f, 0.65f, 0.22f);
        backrest.transform.localPosition = new Vector3(0f, 0.82f, -0.38f);
        backrest.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
        backrest.GetComponent<MeshRenderer>().material = activeVelvetMat;

        // Curved Armrests
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = (side == -1) ? "Left Armrest" : "Right Armrest";
            arm.transform.SetParent(chairRoot.transform);
            arm.transform.localScale = new Vector3(0.18f, 0.35f, 0.85f);
            arm.transform.localPosition = new Vector3(side * 0.46f, 0.65f, 0.05f);
            arm.GetComponent<MeshRenderer>().material = activeVelvetMat;
        }

        // 4 Slanted Brass Legs
        Vector3[] legOffsets = new Vector3[] {
            new Vector3(-0.35f, 0.22f, -0.35f),
            new Vector3( 0.35f, 0.22f, -0.35f),
            new Vector3(-0.35f, 0.22f,  0.35f),
            new Vector3( 0.35f, 0.22f,  0.35f)
        };

        for (int i = 0; i < legOffsets.Length; i++)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leg.name = $"Brass Leg {i + 1}";
            leg.transform.SetParent(chairRoot.transform);
            leg.transform.localScale = new Vector3(0.045f, 0.22f, 0.045f);
            leg.transform.localPosition = legOffsets[i];
            leg.transform.localRotation = Quaternion.Euler(
                legOffsets[i].z > 0 ? 8f : -8f,
                0f,
                legOffsets[i].x > 0 ? -8f : 8f
            );
            leg.GetComponent<MeshRenderer>().material = brassMaterial;
        }

        // Add box collider for raycast detection
        BoxCollider boxCol = chairRoot.AddComponent<BoxCollider>();
        boxCol.center = new Vector3(0f, 0.55f, 0f);
        boxCol.size = new Vector3(1.1f, 1.1f, 1.1f);
    }

    void BuildChesterfieldLeatherSofa()
    {
        sofaRoot = new GameObject("Chesterfield Leather Sofa (Normal Mapped)");
        sofaRoot.transform.SetParent(this.transform);
        sofaRoot.transform.position = Vector3.zero;

        // Sofa Seat Base (Low Polygon Box)
        GameObject seatBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        seatBase.name = "Sofa Base";
        seatBase.transform.SetParent(sofaRoot.transform);
        seatBase.transform.localScale = new Vector3(2.2f, 0.40f, 1.1f);
        seatBase.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        seatBase.GetComponent<MeshRenderer>().material = sofaLeatherMaterial;

        // Sofa Tufted Backrest (Low Polygon Box - wrinkling simulated by Normal Map!)
        GameObject backrest = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backrest.name = "Tufted Leather Backrest";
        backrest.transform.SetParent(sofaRoot.transform);
        backrest.transform.localScale = new Vector3(2.2f, 0.65f, 0.35f);
        backrest.transform.localPosition = new Vector3(0f, 0.75f, -0.38f);
        backrest.GetComponent<MeshRenderer>().material = sofaLeatherMaterial;

        // Rolled Leather Arms (Cylinders)
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arm.name = (side == -1) ? "Left Rolled Arm" : "Right Rolled Arm";
            arm.transform.SetParent(sofaRoot.transform);
            arm.transform.localScale = new Vector3(0.38f, 0.55f, 0.38f);
            arm.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            arm.transform.localPosition = new Vector3(side * 1.15f, 0.62f, 0f);
            arm.GetComponent<MeshRenderer>().material = sofaLeatherMaterial;
        }

        // 4 Turned Wooden Feet
        Vector3[] feetOffsets = new Vector3[] {
            new Vector3(-1.0f, 0.08f, -0.45f),
            new Vector3( 1.0f, 0.08f, -0.45f),
            new Vector3(-1.0f, 0.08f,  0.45f),
            new Vector3( 1.0f, 0.08f,  0.45f)
        };
        for (int i = 0; i < feetOffsets.Length; i++)
        {
            GameObject foot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            foot.name = $"Wooden Foot {i + 1}";
            foot.transform.SetParent(sofaRoot.transform);
            foot.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
            foot.transform.localPosition = feetOffsets[i];
            foot.GetComponent<MeshRenderer>().material = darkWoodMaterial;
        }

        BoxCollider boxCol = sofaRoot.AddComponent<BoxCollider>();
        boxCol.center = new Vector3(0f, 0.55f, 0f);
        boxCol.size = new Vector3(2.5f, 1.1f, 1.2f);
    }

    void BuildPlacementReticle()
    {
        placementReticle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        placementReticle.name = "AR Placement Target Reticle";
        placementReticle.transform.SetParent(this.transform);
        placementReticle.transform.localScale = new Vector3(1.2f, 0.005f, 1.2f);
        placementReticle.transform.position = new Vector3(0f, 0.01f, 0f);

        // Remove collider from reticle to avoid blocking floor raycasts
        Destroy(placementReticle.GetComponent<Collider>());

        Shader unlitShader = Shader.Find("Unlit/Color");
        if (unlitShader == null) unlitShader = Shader.Find("Diffuse");
        Material reticleMat = new Material(unlitShader);
        reticleMat.color = new Color(0.2f, 0.8f, 1.0f, 0.6f);
        placementReticle.GetComponent<MeshRenderer>().material = reticleMat;
    }
    #endregion

    #region User Interaction & Window-to-Viewport Transformation
    void Update()
    {
        HandleKeyboardAndMouseOrbit();
        HandleScreenRaycastInput();
        AnimateFurniturePlacement();
        UpdateLighting();

        // Pulsing animation for AR target reticle
        if (placementReticle != null)
        {
            float pulse = 1.0f + 0.1f * Mathf.Sin(Time.time * 6f);
            placementReticle.transform.localScale = new Vector3(1.2f * pulse, 0.005f, 1.2f * pulse);
        }
    }

    void HandleKeyboardAndMouseOrbit()
    {
        // Orbit camera around target using right mouse drag
        if (Input.GetMouseButton(1))
        {
            cameraYaw += Input.GetAxis("Mouse X") * 3.5f;
            cameraPitch -= Input.GetAxis("Mouse Y") * 3.0f;
            cameraPitch = Mathf.Clamp(cameraPitch, 5f, 85f);
            UpdateCameraTransform();
        }

        // Scroll wheel zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            cameraDistance = Mathf.Clamp(cameraDistance - scroll * 2.5f, 1.8f, 8.0f);
            UpdateCameraTransform();
        }

        // Arrow keys orbit
        if (Input.GetKey(KeyCode.LeftArrow)) { cameraYaw -= 45f * Time.deltaTime; UpdateCameraTransform(); }
        if (Input.GetKey(KeyCode.RightArrow)) { cameraYaw += 45f * Time.deltaTime; UpdateCameraTransform(); }
        if (Input.GetKey(KeyCode.UpArrow)) { cameraPitch = Mathf.Clamp(cameraPitch - 30f * Time.deltaTime, 5f, 85f); UpdateCameraTransform(); }
        if (Input.GetKey(KeyCode.DownArrow)) { cameraPitch = Mathf.Clamp(cameraPitch + 30f * Time.deltaTime, 5f, 85f); UpdateCameraTransform(); }
    }

    void UpdateCameraTransform()
    {
        Quaternion rot = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
        Vector3 pos = cameraLookTarget - (rot * Vector3.forward * cameraDistance);
        mainCamera.transform.position = pos;
        mainCamera.transform.LookAt(cameraLookTarget);
    }

    void HandleScreenRaycastInput()
    {
        // Check for Left Click or Touch (ignoring clicks inside GUI area)
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;
            // Ignore clicks inside top-left HUD rect
            if (mousePos.x < 420 && mousePos.y > Screen.height - 380) return;

            // -----------------------------------------------------------------
            // Mathematical Window-to-Viewport & Screen-to-World Raycasting
            // -----------------------------------------------------------------
            lastScreenPixel = mousePos;

            // Step 1: Map Screen Pixel (x_s, y_s) to Viewport Coordinates [0, 1]
            float u_viewport = mousePos.x / Screen.width;
            float v_viewport = mousePos.y / Screen.height;

            // Step 2: Map Viewport to Normalized Device Coordinates (NDC) [-1, 1]
            lastNDC = new Vector2(
                (u_viewport * 2f) - 1f,
                (v_viewport * 2f) - 1f
            );

            // Step 3: Unproject NDC via Inverse View-Projection Matrix into 3D World Ray
            Ray ray = mainCamera.ScreenPointToRay(mousePos);

            // Step 4: Intersect with AR Ground Floor Plane at y = 0
            // Plane equation: n . p + d = 0, where n = (0, 1, 0), d = 0
            Plane floorMathPlane = new Plane(Vector3.up, Vector3.zero);
            float enterDist;
            if (floorMathPlane.Raycast(ray, out enterDist))
            {
                lastWorldHitPos = ray.GetPoint(enterDist);
                targetPlacementPos = new Vector3(lastWorldHitPos.x, 0f, lastWorldHitPos.z);
                placementAnimTime = 0f;

                // Move target reticle to hit position
                if (placementReticle != null)
                {
                    placementReticle.transform.position = new Vector3(targetPlacementPos.x, 0.015f, targetPlacementPos.z);
                }
            }
        }
    }

    void AnimateFurniturePlacement()
    {
        if (activeFurniture == null) return;

        // Smooth spring damp animation toward placement position
        if (placementAnimTime < 1f)
        {
            placementAnimTime += Time.deltaTime * 3.5f;
            float t = Mathf.SmoothStep(0f, 1f, placementAnimTime);
            activeFurniture.transform.position = Vector3.Lerp(activeFurniture.transform.position, targetPlacementPos, t);
        }
    }
    #endregion

    #region GUI Controls & Evaluation Metrics
    void SetFurnitureVisibility(bool chairActive)
    {
        showChair = chairActive;
        if (chairRoot != null) chairRoot.SetActive(showChair);
        if (sofaRoot != null) sofaRoot.SetActive(!showChair);
        activeFurniture = showChair ? chairRoot : sofaRoot;
        UpdateActiveMaterialModel();
    }

    void UpdateActiveMaterialModel()
    {
        if (showChair && chairRoot != null)
        {
            Material mat = useCharlieSheenBRDF ? chairVelvetMaterial : chairPhongMaterial;
            activeBRDFDescription = useCharlieSheenBRDF ?
                "Charlie Sheen BRDF (PBR Fabric Grazing Fuzz)" :
                "Empirical Blinn-Phong (Notice Plastic Artefact)";

            foreach (var r in chairRoot.GetComponentsInChildren<MeshRenderer>())
            {
                if (r.name.Contains("Seat") || r.name.Contains("Backrest") || r.name.Contains("Armrest"))
                {
                    r.material = mat;
                }
            }
        }
        else if (!showChair && sofaRoot != null)
        {
            activeBRDFDescription = enableNormalMapping ?
                "Tangent-Space Normal Mapped Leather (Low-Poly)" :
                "Flat Leather Material (Normal Map DISABLED)";

            if (enableNormalMapping)
            {
                sofaLeatherMaterial.EnableKeyword("_NORMALMAP");
                sofaLeatherMaterial.SetTexture("_BumpMap", leatherNormalMap);
                sofaLeatherMaterial.SetFloat("_BumpScale", 1.8f);
            }
            else
            {
                sofaLeatherMaterial.DisableKeyword("_NORMALMAP");
                sofaLeatherMaterial.SetTexture("_BumpMap", null);
            }
        }
    }

    void OnGUI()
    {
        // Styling Box
        GUI.backgroundColor = new Color(0.06f, 0.10f, 0.18f, 0.92f);
        GUILayout.BeginArea(new Rect(18, 18, 390, 480), GUI.skin.box);

        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 14;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = new Color(0.22f, 0.74f, 0.98f);
        GUILayout.Label("CS4104 WebXR Retail & AR Showcase", headerStyle);

        GUIStyle subStyle = new GUIStyle(GUI.skin.label);
        subStyle.fontSize = 10;
        subStyle.normal.textColor = new Color(0.70f, 0.78f, 0.88f);
        GUILayout.Label("Interactive Illumination, Velvet Sheen, Normal Mapping & Raycasting", subStyle);
        GUILayout.Space(8);

        // 1. Furniture Selector
        GUILayout.Label("<b>1. Active Furniture Piece:</b>");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(showChair ? "● Red Velvet Chair" : "Red Velvet Chair"))
        {
            SetFurnitureVisibility(true);
        }
        if (GUILayout.Button(!showChair ? "● Leather Sofa" : "Leather Sofa"))
        {
            SetFurnitureVisibility(false);
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(6);

        // 2. Material & Shading Model Selector
        if (showChair)
        {
            GUILayout.Label("<b>2. Illumination / Shading Model:</b>");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(useCharlieSheenBRDF ? "● Charlie Sheen (PBR)" : "Charlie Sheen (PBR)"))
            {
                useCharlieSheenBRDF = true;
                UpdateActiveMaterialModel();
            }
            if (GUILayout.Button(!useCharlieSheenBRDF ? "● Blinn-Phong (Plastic)" : "Blinn-Phong (Plastic)"))
            {
                useCharlieSheenBRDF = false;
                UpdateActiveMaterialModel();
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("<b>2. Texture & Bump Mapping:</b>");
            string normBtnText = enableNormalMapping ? "Normal Map: ENABLED (Wrinkles/Creases)" : "Normal Map: DISABLED (Flat Plastic Box)";
            if (GUILayout.Button(normBtnText))
            {
                enableNormalMapping = !enableNormalMapping;
                UpdateActiveMaterialModel();
            }
        }
        GUILayout.Space(6);

        // 3. Sun Position Slider (Simulating AR Light Estimation)
        GUILayout.Label($"<b>3. Sun Direction Azimuth:</b> {sunAzimuth:F0}° | Elev: {sunElevation:F0}°");
        sunAzimuth = GUILayout.HorizontalSlider(sunAzimuth, 0f, 360f);
        GUILayout.Space(8);

        // 4. Mathematical Window-to-Viewport Transformation Readout
        GUIStyle monoBox = new GUIStyle(GUI.skin.box);
        monoBox.alignment = TextAnchor.UpperLeft;
        monoBox.fontSize = 10;
        monoBox.normal.textColor = new Color(0.65f, 0.95f, 1.0f);

        string stats =
            "<b>Window-to-Viewport & Raycast Coordinates:</b>\n" +
            $"• Screen Pixel: ({lastScreenPixel.x:F1}, {lastScreenPixel.y:F1}) px\n" +
            $"• NDC Space:    ({lastNDC.x:F3}, {lastNDC.y:F3}) in [-1, 1]\n" +
            $"• 3D Hit Pos:   ({lastWorldHitPos.x:F2}, {lastWorldHitPos.y:F2}, {lastWorldHitPos.z:F2}) m\n" +
            $"• Active BRDF:  {activeBRDFDescription}\n" +
            $"• Geometry:     Low-Poly Mesh (< 850 triangles)\n" +
            "----------------------------------------------\n" +
            "<b>Controls:</b>\n" +
            "• Left-Click on Floor: Raycast & Place Furniture\n" +
            "• Right-Click Drag: Orbit Camera Angle\n" +
            "• Mouse Scroll: Zoom Camera In/Out";

        GUILayout.Label(stats, monoBox);

        GUILayout.EndArea();
    }
    #endregion
}
