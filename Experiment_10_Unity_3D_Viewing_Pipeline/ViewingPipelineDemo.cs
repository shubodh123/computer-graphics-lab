using UnityEngine;

// Experiment 10: Demonstrates the 3D viewing pipeline in Unity.
// Attach to an empty GameObject. The script creates a 3D object and lets you
// switch between perspective and orthographic projection, and adjust the viewport.
public class ViewingPipelineDemo : MonoBehaviour
{
    public float rotationSpeed = 35f;

    private Camera demoCamera;
    private GameObject demoObject;
    private bool perspective = true;
    private Rect fullViewport = new Rect(0f, 0f, 1f, 1f);
    private Rect insetViewport = new Rect(0.62f, 0.62f, 0.34f, 0.30f);

    void Start()
    {
        demoObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        demoObject.name = "Viewing Pipeline Cube";
        demoObject.transform.position = Vector3.zero;
        demoObject.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        demoObject.GetComponent<Renderer>().material.color = new Color(0.2f, 0.75f, 0.95f);

        GameObject lightObject = new GameObject("Viewing Pipeline Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.3f;
        lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

        GameObject cameraObject = new GameObject("Viewing Pipeline Camera");
        demoCamera = cameraObject.AddComponent<Camera>();
        demoCamera.transform.position = new Vector3(4f, 3f, -6f);
        demoCamera.transform.LookAt(Vector3.zero);
        demoCamera.nearClipPlane = 0.1f;
        demoCamera.farClipPlane = 100f;
        demoCamera.fieldOfView = 60f;
        demoCamera.clearFlags = CameraClearFlags.Skybox;
        demoCamera.rect = fullViewport;

        // Make this camera the active camera for the scene.
        Camera.main?.gameObject.SetActive(false);
        demoCamera.tag = "MainCamera";
    }

    void Update()
    {
        if (demoObject == null || demoCamera == null) return;

        demoObject.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        demoObject.transform.Rotate(Vector3.right, rotationSpeed * 0.5f * Time.deltaTime, Space.World);

        if (Input.GetKeyDown(KeyCode.P))
        {
            perspective = true;
            demoCamera.orthographic = false;
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            perspective = false;
            demoCamera.orthographic = true;
            demoCamera.orthographicSize = 3f;
        }

        // V toggles between full-screen viewport and an inset viewport.
        if (Input.GetKeyDown(KeyCode.V))
            demoCamera.rect = demoCamera.rect == fullViewport ? insetViewport : fullViewport;

        // Arrow keys orbit the camera around the object.
        if (Input.GetKey(KeyCode.LeftArrow)) demoCamera.transform.RotateAround(Vector3.zero, Vector3.up, -50f * Time.deltaTime);
        if (Input.GetKey(KeyCode.RightArrow)) demoCamera.transform.RotateAround(Vector3.zero, Vector3.up, 50f * Time.deltaTime);
        if (Input.GetKey(KeyCode.UpArrow)) demoCamera.transform.RotateAround(Vector3.zero, demoCamera.transform.right, -50f * Time.deltaTime);
        if (Input.GetKey(KeyCode.DownArrow)) demoCamera.transform.RotateAround(Vector3.zero, demoCamera.transform.right, 50f * Time.deltaTime);

        demoCamera.transform.LookAt(Vector3.zero);
    }

    void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 360, 100),
            "Experiment 10: 3D Viewing Pipeline\\nP: Perspective | O: Orthographic\\nV: Toggle viewport | Arrow keys: Orbit camera");
    }
}
