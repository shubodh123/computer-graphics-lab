using UnityEngine;

// Experiment 9: Create a simple interactive 3D scene using Unity.
// Attach this script to an empty GameObject in a Unity scene.
// Controls: WASD = move, Q/E = rotate, Space = jump, mouse = look around.
public class Interactive3DScene : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 90f;
    public float mouseSensitivity = 2f;
    public float jumpForce = 5f;

    private Transform player;
    private Camera sceneCamera;
    private float verticalVelocity;
    private float pitch;
    private bool grounded;

    void Start()
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Ground";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(5f, 1f, 5f);

        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Interactive Cube";
        cube.transform.position = new Vector3(0f, 1f, 0f);
        cube.GetComponent<Renderer>().material.color = new Color(0.2f, 0.65f, 1f);

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Sphere";
        sphere.transform.position = new Vector3(3f, 1f, 2f);
        sphere.GetComponent<Renderer>().material.color = new Color(1f, 0.45f, 0.2f);

        GameObject directionalLight = new GameObject("Directional Light");
        Light light = directionalLight.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        directionalLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        GameObject playerObject = new GameObject("Player");
        player = playerObject.transform;
        player.position = new Vector3(0f, 1f, -5f);

        GameObject cameraObject = new GameObject("Player Camera");
        sceneCamera = cameraObject.AddComponent<Camera>();
        cameraObject.transform.SetParent(player);
        cameraObject.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        cameraObject.transform.localRotation = Quaternion.identity;
        sceneCamera.tag = "MainCamera";

        // Add a simple collider-based ground check using a CharacterController.
        CharacterController controller = playerObject.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.35f;
        controller.center = new Vector3(0f, 0.9f, 0f);

        playerObject.AddComponent<AudioListener>();
    }

    void Update()
    {
        if (player == null || sceneCamera == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        player.Rotate(0f, mouseX, 0f);
        pitch = Mathf.Clamp(pitch - mouseY, -80f, 80f);
        sceneCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 move = player.right * horizontal + player.forward * vertical;
        move = Vector3.ClampMagnitude(move, 1f) * moveSpeed;

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -1f;
        if (controller.isGrounded && Input.GetKeyDown(KeyCode.Space)) verticalVelocity = jumpForce;
        verticalVelocity += Physics.gravity.y * Time.deltaTime;

        Vector3 velocity = move + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        if (Input.GetKey(KeyCode.Q)) player.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime);
        if (Input.GetKey(KeyCode.E)) player.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}
