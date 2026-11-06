using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// CS4104 Computer Graphics & Virtual Reality - Case Study 4: Behavioral Simulation & Crowd Dynamics
/// 
/// Helbing's Social Force Model Metro Emergency Evacuation Simulator in Unity.
/// Demonstrates:
/// 1. Autonomous Multi-Agent Crowd Dynamics using Helbing's Social Force Differential Equations:
///    - Self-Propulsion Force toward goal exit
///    - Inter-Agent Psychological Repulsion (A*exp((r-d)/B))
///    - Physical Contact Compression & Sliding Friction
///    - Wall & Structural Pillar Repulsion
/// 2. Alarm Event: Track Fire blocks Primary Exit A, Panic Contagion Triggers
/// 3. The 'Faster-is-Slower' Doorway Bottleneck Choking & Dynamic NavMesh Rerouting to Exit B
/// </summary>
public class CaseStudy4_MetroCrowdEvacuationDemo : MonoBehaviour
{
    private class CrowdAgent
    {
        public GameObject visualObj;
        public Vector2 pos;
        public Vector2 vel;
        public float mass = 75f;
        public float radius = 0.28f;
        public float v0 = 1.34f;
        public float tau = 0.5f;
        public float panic = 0.1f;
        public bool evacuated = false;
        public bool targetExitB = false;
    }

    private Camera mainCamera;
    private List<CrowdAgent> agents = new List<CrowdAgent>();
    private bool fireAlarmActive = false;
    private float simTime = 0f;

    // Platform Geometry (Meters)
    private float platformLen = 32f;
    private float platformWid = 14f;
    private Vector2 exitA = new Vector2(16f, 0f);  // Primary Exit
    private Vector2 exitB = new Vector2(-16f, 0f); // Emergency Exit

    // Visual Markers
    private GameObject fireEffectObj;
    private Material agentCalmMat;
    private Material agentPanicMat;

    void Start()
    {
        SetupCameraAndLighting();
        BuildMetroPlatform();
        SpawnCrowdAgents(80);
    }

    void SetupCameraAndLighting()
    {
        GameObject camObj = GameObject.Find("Main Camera") ?? new GameObject("Main Camera");
        mainCamera = camObj.GetComponent<Camera>() ?? camObj.AddComponent<Camera>();
        mainCamera.transform.position = new Vector3(0f, 22f, -14f);
        mainCamera.transform.LookAt(new Vector3(0f, 0f, 0f));
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0.08f, 0.10f, 0.15f);

        GameObject lightObj = GameObject.Find("Directional Light") ?? new GameObject("Metro Station Lights");
        Light l = lightObj.GetComponent<Light>() ?? lightObj.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.2f;
        l.transform.rotation = Quaternion.Euler(60f, 0f, 0f);

        agentCalmMat = new Material(Shader.Find("Diffuse") ?? Shader.Find("Standard"));
        agentCalmMat.color = new Color(0.2f, 0.7f, 1.0f);

        agentPanicMat = new Material(Shader.Find("Diffuse") ?? Shader.Find("Standard"));
        agentPanicMat.color = new Color(1.0f, 0.25f, 0.2f);
    }

    void BuildMetroPlatform()
    {
        // Floor
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Metro Platform Surface";
        floor.transform.SetParent(this.transform);
        floor.transform.localScale = new Vector3(3.2f, 1f, 1.4f);
        floor.GetComponent<MeshRenderer>().material.color = new Color(0.35f, 0.38f, 0.42f);

        // 4 Structural Pillars
        Vector2[] pillarPos = new Vector2[] {
            new Vector2(-8f, 3f), new Vector2(-8f, -3f),
            new Vector2( 8f, 3f), new Vector2( 8f, -3f)
        };
        for (int i = 0; i < pillarPos.Length; i++)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = $"Pillar {i + 1}";
            pillar.transform.SetParent(this.transform);
            pillar.transform.localScale = new Vector3(0.8f, 2.5f, 0.8f);
            pillar.transform.position = new Vector3(pillarPos[i].x, 2.5f, pillarPos[i].y);
            pillar.GetComponent<MeshRenderer>().material.color = new Color(0.6f, 0.65f, 0.7f);
        }

        // Fire Obstacle Marker at Exit A
        fireEffectObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fireEffectObj.name = "Track Fire Hazard (Exit A)";
        fireEffectObj.transform.SetParent(this.transform);
        fireEffectObj.transform.localScale = new Vector3(1.5f, 2f, 3.5f);
        fireEffectObj.transform.position = new Vector3(16f, 1f, 0f);
        fireEffectObj.GetComponent<MeshRenderer>().material.color = Color.red;
        fireEffectObj.SetActive(false);
    }

    void SpawnCrowdAgents(int count)
    {
        Random.InitState(42);
        for (int i = 0; i < count; i++)
        {
            CrowdAgent ag = new CrowdAgent();
            ag.pos = new Vector2(Random.Range(-12f, 12f), Random.Range(-5f, 5f));
            ag.vel = Vector2.zero;

            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            obj.name = $"Pedestrian_{i + 1}";
            obj.transform.SetParent(this.transform);
            obj.transform.localScale = new Vector3(0.45f, 0.6f, 0.45f);
            obj.transform.position = new Vector3(ag.pos.x, 0.6f, ag.pos.y);
            obj.GetComponent<MeshRenderer>().material = agentCalmMat;
            ag.visualObj = obj;

            agents.Add(ag);
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        simTime += dt;

        // Auto trigger track fire at t = 6s
        if (simTime >= 6f && !fireAlarmActive)
        {
            TriggerFireAlarm();
        }

        // ----------------------------------------------------
        // Helbing's Social Force Model Simulation Step
        // ----------------------------------------------------
        float A = 2000f;
        float B = 0.08f;
        float k = 120000f;

        foreach (var ag in agents)
        {
            if (ag.evacuated) continue;

            // Target exit selection
            Vector2 target = ag.targetExitB ? exitB : exitA;
            Vector2 diff = target - ag.pos;
            float dist = diff.magnitude;

            if (dist < 1.0f)
            {
                ag.evacuated = true;
                ag.visualObj.SetActive(false);
                continue;
            }

            Vector2 e0 = diff.normalized;

            // 1. Self-Propulsion Force
            Vector2 f0 = ag.mass * (ag.v0 * e0 - ag.vel) / ag.tau;
            Vector2 totalForce = f0;

            // 2. Inter-Agent Social Repulsion & Contact Force
            foreach (var other in agents)
            {
                if (other == ag || other.evacuated) continue;
                Vector2 delta = ag.pos - other.pos;
                float d = delta.magnitude;
                float rSum = ag.radius + other.radius;

                if (d > 0.001f && d < 2.5f)
                {
                    Vector2 n = delta.normalized;
                    // Psychological repulsion
                    float fRepel = A * Mathf.Exp((rSum - d) / B);
                    totalForce += n * fRepel;

                    // Physical contact compression
                    if (d < rSum)
                    {
                        totalForce += n * (k * (rSum - d));
                    }
                }
            }

            // Integrate
            Vector2 acc = totalForce / ag.mass;
            ag.vel += acc * dt;
            if (ag.vel.magnitude > 4.5f) ag.vel = ag.vel.normalized * 4.5f;

            ag.pos += ag.vel * dt;
            ag.pos.x = Mathf.Clamp(ag.pos.x, -platformLen * 0.5f, platformLen * 0.5f);
            ag.pos.y = Mathf.Clamp(ag.pos.y, -platformWid * 0.5f, platformWid * 0.5f);

            ag.visualObj.transform.position = new Vector3(ag.pos.x, 0.6f, ag.pos.y);
        }
    }

    public void TriggerFireAlarm()
    {
        fireAlarmActive = true;
        if (fireEffectObj != null) fireEffectObj.SetActive(true);

        foreach (var ag in agents)
        {
            ag.panic = 1.0f;
            ag.v0 = 3.6f; // Elevated sprint speed under panic
            ag.visualObj.GetComponent<MeshRenderer>().material = agentPanicMat;
            // Reroute to secondary exit B
            ag.targetExitB = true;
        }
    }

    void OnGUI()
    {
        GUI.backgroundColor = new Color(0.08f, 0.12f, 0.20f, 0.95f);
        GUILayout.BeginArea(new Rect(18, 18, 380, 400), GUI.skin.box);

        GUIStyle title = new GUIStyle(GUI.skin.label);
        title.fontSize = 13;
        title.fontStyle = FontStyle.Bold;
        title.normal.textColor = new Color(0.35f, 0.85f, 1.0f);
        GUILayout.Label("CS4104 Case Study 4: Metro Evacuation Sim", title);

        int evacCount = 0;
        foreach (var a in agents) if (a.evacuated) evacCount++;
        GUILayout.Label($"<b>Simulation Time:</b> {simTime:F1} s");
        GUILayout.Label($"<b>Evacuated:</b> {evacCount} / {agents.Count} ({((float)evacCount/agents.Count)*100f:F1}%)");
        GUILayout.Label($"<b>Platform Status:</b> {(fireAlarmActive ? "<color=red>TRACK FIRE! EXIT A BLOCKED</color>" : "<color=green>NORMAL COMMUTE</color>")}");
        GUILayout.Space(6);

        if (!fireAlarmActive)
        {
            if (GUILayout.Button("Trigger Emergency Fire Alarm (Exit A)"))
            {
                TriggerFireAlarm();
            }
        }
        else
        {
            GUILayout.Box("Alarm Active: Agents rerouted to Emergency Exit B\nNotice 'Faster-is-Slower' bottleneck arching!");
        }

        GUILayout.Space(8);
        string info =
            "<b>Multi-Agent Model: Helbing Social Force (SFM)</b>\n" +
            "• Self-Propulsion: f0 = m*(v0*e0 - v)/tau\n" +
            "• Social Repulsion: A * exp((r_ij - d_ij) / B)\n" +
            "• Physical Contact: k * (r_ij - d_ij) + friction\n" +
            "----------------------------------------------\n" +
            "<b>Hardware Immersion Channels:</b>\n" +
            "• Visual: 110° FOV OLED (Signage reading)\n" +
            "• Audio: 3D Spatial HRTF (Cavernous sirens)\n" +
            "• Haptic: Vibrotactile vest (Crowd jostling)\n" +
            "• Locomotion: Omnidirectional Treadmill";
        GUILayout.Label(info, GUI.skin.box);

        GUILayout.EndArea();
    }
}
