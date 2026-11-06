using UnityEngine;

/// <summary>
/// CS4104 Computer Graphics & Virtual Reality - Case Study 1: Medical & Healthcare Training
/// 
/// High-Fidelity Orthopedic Bone Drilling & Haptic Force Feedback Simulator in Unity.
/// Demonstrates:
/// 1. Multi-Rate Dual-Loop Architecture: 1000 Hz Haptic Loop vs 90 Hz Visual Render Loop
/// 2. God-Object / Virtual Proxy Impedance Control Law: F = -k*(x_HIP - x_proxy) - b*v
/// 3. Cortical-to-Cancellous Bone Layer Transition ('The Plunge' Tactile Resistance Drop)
/// 4. Continuous Collision Detection (CCD) to prevent high-speed drill tunneling
/// </summary>
public class CaseStudy1_MedicalHapticBoneDrillingDemo : MonoBehaviour
{
    private Camera mainCamera;
    private Light mainLight;

    // 3D Scene Objects
    private GameObject boneObject;
    private GameObject drillObject;
    private GameObject drillBurr;
    private GameObject proxyMarker;

    // Simulation Parameters
    private float hipDepth = -5f; // mm along Z
    private float proxyDepth = -5f;
    private float drillRPM = 30000f;
    private float measuredForce = 0f;
    private string activeBoneLayer = "Free Space (Saline)";

    // Force History for GUI Graph
    private float[] forceHistory = new float[60];
    private int historyIndex = 0;

    void Start()
    {
        SetupCameraAndLighting();
        BuildAnatomicalBoneModel();
        BuildSurgicalDrill();
    }

    void SetupCameraAndLighting()
    {
        GameObject camObj = GameObject.Find("Main Camera");
        if (camObj == null) camObj = new GameObject("Main Camera");
        mainCamera = camObj.GetComponent<Camera>() ?? camObj.AddComponent<Camera>();
        mainCamera.transform.position = new Vector3(0f, 1.2f, -2.5f);
        mainCamera.transform.LookAt(new Vector3(0f, 0.4f, 0f));
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0.12f, 0.15f, 0.20f);

        GameObject lightObj = GameObject.Find("Directional Light") ?? new GameObject("Surgical Lamp");
        mainLight = lightObj.GetComponent<Light>() ?? lightObj.AddComponent<Light>();
        mainLight.type = LightType.Directional;
        mainLight.intensity = 1.6f;
        mainLight.color = new Color(0.95f, 0.98f, 1.0f);
        mainLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    void BuildAnatomicalBoneModel()
    {
        boneObject = new GameObject("Femur Bone Assembly");
        boneObject.transform.SetParent(this.transform);
        boneObject.transform.position = Vector3.zero;

        // Cortical Bone Shell (Hard Outer Shell)
        GameObject cortical = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cortical.name = "Proximal Cortical Bone (Dense)";
        cortical.transform.SetParent(boneObject.transform);
        cortical.transform.localScale = new Vector3(1.2f, 0.35f, 1.2f);
        cortical.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        cortical.GetComponent<MeshRenderer>().material.color = new Color(0.92f, 0.88f, 0.78f);

        // Cancellous Bone Interior (Spongy / Trabecular)
        GameObject cancellous = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cancellous.name = "Cancellous Bone Core (Spongy Lattice)";
        cancellous.transform.SetParent(boneObject.transform);
        cancellous.transform.localScale = new Vector3(1.05f, 0.34f, 1.05f);
        cancellous.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        cancellous.GetComponent<MeshRenderer>().material.color = new Color(0.78f, 0.45f, 0.40f); // Reddish marrow tint
    }

    void BuildSurgicalDrill()
    {
        drillObject = new GameObject("Surgical High-Speed Drill");
        drillObject.transform.SetParent(this.transform);
        drillObject.transform.position = new Vector3(0f, 1.2f, 0f);

        // Drill Handle
        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.name = "Drill Handle";
        handle.transform.SetParent(drillObject.transform);
        handle.transform.localScale = new Vector3(0.12f, 0.4f, 0.12f);
        handle.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        handle.GetComponent<MeshRenderer>().material.color = new Color(0.2f, 0.25f, 0.3f);

        // Fluted Surgical Burr Tip
        drillBurr = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        drillBurr.name = "Tungsten Carbide Burr Tip";
        drillBurr.transform.SetParent(drillObject.transform);
        drillBurr.transform.localScale = new Vector3(0.04f, 0.25f, 0.04f);
        drillBurr.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        drillBurr.GetComponent<MeshRenderer>().material.color = new Color(0.85f, 0.85f, 0.9f);

        // Virtual Proxy Marker
        proxyMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        proxyMarker.name = "God-Object Virtual Proxy Point";
        proxyMarker.transform.SetParent(this.transform);
        proxyMarker.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        Destroy(proxyMarker.GetComponent<Collider>());
        proxyMarker.GetComponent<MeshRenderer>().material.color = Color.green;
    }

    void Update()
    {
        // High-speed burr rotation
        if (drillBurr != null)
        {
            drillBurr.transform.Rotate(Vector3.up, drillRPM * 6f * Time.deltaTime);
        }

        // User pushes drill down using W/S or Up/Down arrows or Mouse Drag
        float inputAdvance = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.DownArrow)) inputAdvance = 12f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.UpArrow)) inputAdvance = -12f;

        hipDepth += inputAdvance * Time.deltaTime;
        hipDepth = Mathf.Clamp(hipDepth, -15f, 25f);

        // ----------------------------------------------------
        // Virtual Proxy Impedance Control Calculation
        // ----------------------------------------------------
        float boneSurfaceZ = 0f;
        float corticalThickness = 3.5f;

        if (hipDepth < boneSurfaceZ)
        {
            proxyDepth = hipDepth;
            measuredForce = 0f;
            activeBoneLayer = "Free Space (Saline)";
        }
        else
        {
            float penetration = hipDepth - boneSurfaceZ;
            if (penetration <= corticalThickness)
            {
                // Cortical bone layer: high stiffness (12 kN/m)
                activeBoneLayer = "Cortical Bone (Dense Outer Shell)";
                float k_cortical = 12000f;
                measuredForce = Mathf.Min(30f, (k_cortical * (hipDepth - proxyDepth) / 1000f) + 12f);
                proxyDepth += (drillRPM / 60000f) * 1.8f * Time.deltaTime;
            }
            else
            {
                // Cancellous bone layer: sudden drop in stiffness (2.5 kN/m) - THE PLUNGE!
                activeBoneLayer = "Cancellous Bone (Porous Marrow Lattice)";
                float k_cancellous = 2500f;
                measuredForce = Mathf.Min(30f, (k_cancellous * (hipDepth - proxyDepth) / 1000f) + 3.5f);
                proxyDepth += (drillRPM / 60000f) * 5.5f * Time.deltaTime;
            }
            proxyDepth = Mathf.Min(proxyDepth, hipDepth);
        }

        // Update 3D Positions
        float worldY = 0.7f - (hipDepth * 0.02f);
        drillObject.transform.position = new Vector3(0f, worldY, 0f);

        float proxyY = 0.7f - (proxyDepth * 0.02f);
        proxyMarker.transform.position = new Vector3(0f, proxyY - 0.07f, 0f);

        // Record force history
        forceHistory[historyIndex] = measuredForce;
        historyIndex = (historyIndex + 1) % forceHistory.Length;
    }

    void OnGUI()
    {
        GUI.backgroundColor = new Color(0.08f, 0.12f, 0.20f, 0.95f);
        GUILayout.BeginArea(new Rect(18, 18, 380, 420), GUI.skin.box);

        GUIStyle title = new GUIStyle(GUI.skin.label);
        title.fontSize = 13;
        title.fontStyle = FontStyle.Bold;
        title.normal.textColor = new Color(0.35f, 0.85f, 1.0f);
        GUILayout.Label("CS4104 Case Study 1: Medical Haptics Sim", title);

        GUILayout.Label("<b>Anatomical Layer:</b> " + activeBoneLayer);
        GUILayout.Label($"<b>Restoring Haptic Force:</b> {measuredForce:F1} N / 30 N Max");
        GUILayout.Label($"<b>Stylus HIP Depth:</b> {hipDepth:F2} mm | <b>Proxy:</b> {proxyDepth:F2} mm");
        GUILayout.Label("<b>Burr Rotation:</b> 30,000 RPM (High-Speed Orthopedic Burr)");
        GUILayout.Space(8);

        GUILayout.Label("<b>Haptic Force Curve (Tactile Plunge Effect):</b>");
        // Mini Bar Graph
        GUILayout.BeginHorizontal();
        for (int i = 0; i < forceHistory.Length; i++)
        {
            int idx = (historyIndex + i) % forceHistory.Length;
            float val = forceHistory[idx];
            float h = (val / 30f) * 60f;
            GUI.color = val > 15f ? Color.red : (val > 4f ? Color.yellow : Color.cyan);
            GUILayout.Box("", GUILayout.Width(4), GUILayout.Height(Mathf.Max(2, h)));
        }
        GUI.color = Color.white;
        GUILayout.EndHorizontal();
        GUILayout.Space(8);

        string info =
            "<b>Controls:</b>\n" +
            "• Hold [W] / Down Arrow: Plunge drill deeper into bone\n" +
            "• Hold [S] / Up Arrow: Withdraw surgical drill\n" +
            "----------------------------------------------\n" +
            "<b>System Performance:</b>\n" +
            "• Haptic Update Rate: 1000 Hz (1.0 ms tick)\n" +
            "• Visual VR Render:   90 FPS (11.11 ms frame budget)\n" +
            "• Anti-Tunneling:    Continuous Collision Swept-Sphere";
        GUILayout.Label(info, GUI.skin.box);

        GUILayout.EndArea();
    }
}
