using UnityEngine;
using UnityEngine.InputSystem;

public class GunController : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public InputActionAsset inputActions;
    public Transform holdPosition; // For Kinetic Gun floating
    public Transform spawnPoint;   // Where new objects appear
    public Transform HoldObjPosition;  // Where objects are held

    [Header("Gun Visuals")]
    public GameObject kineticVisual;
    public GameObject eraseVisual;
    public GameObject combustVisual;

    // NEW: Line Renderer components attached directly to your gun blocks
    [Header("Laser Beam Renderers")]
    public LineRenderer kineticBeam;
    public LineRenderer eraseBeam;
    public LineRenderer combustBeam;

    [Header("Your 5 Spawnable Prefabs")]
    public GameObject cratePrefab;
    public GameObject ballPrefab;
    public GameObject plankPrefab;
    public GameObject barrelPrefab;
    public GameObject bombPrefab;

    private int currentGun = 1; // 1 = Kinetic, 2 = Erase, 3 = Combust
    private GameObject grabbedObject;

    // Input Action Tracking variables
    private InputAction fireAction;
    private InputAction switch1Action, switch2Action, switch3Action;
    private InputAction yAction, uAction, iAction, oAction, pAction;

    void Awake()
    {
        var gameplayMap = inputActions.FindActionMap("OnFoot"); 
        
        // Gun Controls setup
        fireAction = gameplayMap.FindAction("Fire");
        switch1Action = gameplayMap.FindAction("Switch1");
        switch2Action = gameplayMap.FindAction("Switch2");
        switch3Action = gameplayMap.FindAction("Switch3");

        // Spawner Controls setup
        yAction = gameplayMap.FindAction("SpawnY");
        uAction = gameplayMap.FindAction("SpawnU");
        iAction = gameplayMap.FindAction("SpawnI");
        oAction = gameplayMap.FindAction("SpawnO");
        pAction = gameplayMap.FindAction("SpawnP");
    }

    void OnEnable()
    {
        fireAction.Enable();
        switch1Action.Enable(); switch2Action.Enable(); switch3Action.Enable();
        yAction.Enable(); uAction.Enable(); iAction.Enable(); oAction.Enable(); pAction.Enable();

        fireAction.started += ctx => StartFiring();
        fireAction.canceled += ctx => StopFiring();
    }

    void OnDisable()
    {
        fireAction.Disable();
        switch1Action.Disable(); switch2Action.Disable(); switch3Action.Disable();
        yAction.Disable(); uAction.Disable(); iAction.Disable(); oAction.Disable(); pAction.Disable();
    }

    void Start()
    {
        DisableAllBeams();
    }

    void Update()
    {
        if (switch1Action.WasPressedThisFrame()) SwitchGun(1);
        if (switch2Action.WasPressedThisFrame()) SwitchGun(2);
        if (switch3Action.WasPressedThisFrame()) SwitchGun(3);

        if (yAction.WasPressedThisFrame()) Instantiate(cratePrefab, spawnPoint.position, Quaternion.identity);
        if (uAction.WasPressedThisFrame()) Instantiate(ballPrefab, spawnPoint.position, Quaternion.identity);
        if (iAction.WasPressedThisFrame()) Instantiate(plankPrefab, spawnPoint.position, Quaternion.identity);
        if (oAction.WasPressedThisFrame()) Instantiate(barrelPrefab, spawnPoint.position, Quaternion.identity);
        if (pAction.WasPressedThisFrame()) Instantiate(bombPrefab, spawnPoint.position, Quaternion.identity);

        if (currentGun == 1 && fireAction.IsPressed() && grabbedObject != null)
        {
            grabbedObject.transform.position = Vector3.Lerp(grabbedObject.transform.position, HoldObjPosition.position, Time.deltaTime * 10f);
            
            // NEW: Keep drawing the kinetic beam tractor line relative to the gun visual position
            DrawLaserBeam(kineticBeam, kineticVisual.transform, grabbedObject.transform.position);
        }
    }

    void SwitchGun(int gunIndex)
    {
        currentGun = gunIndex;
        kineticVisual.SetActive(gunIndex == 1);
        eraseVisual.SetActive(gunIndex == 2);
        combustVisual.SetActive(gunIndex == 3);
        DisableAllBeams();
    }

    void StartFiring()
    {
        RaycastHit hit;
        Vector3 endPoint;

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, 50f))
        {
            endPoint = hit.point; 
            Rigidbody targetRigidbody = hit.collider.GetComponentInParent<Rigidbody>();

            // Gun 1: Kinetic grab mechanics
            if (currentGun == 1 && targetRigidbody != null)
            {
                grabbedObject = targetRigidbody.gameObject;
                grabbedObject.GetComponent<Rigidbody>().isKinematic = true;
            }

            // Gun 2: Erasing deletion filter
            if (currentGun == 2)
            {
                if (hit.collider.CompareTag("SpawnedObject") || (hit.collider.transform.parent != null && hit.collider.transform.parent.CompareTag("SpawnedObject")))
                {
                    // NEW: Flash erase laser line
                    DrawLaserBeam(eraseBeam, eraseVisual.transform, endPoint);
                    Invoke("DisableAllBeams", 0.08f);
                    
                    Destroy(hit.collider.transform.root.gameObject);
                }
            }

            // Gun 3: Combustible force explosion shockwave
            if (currentGun == 3)
            {
                // NEW: Flash explosive laser line
                DrawLaserBeam(combustBeam, combustVisual.transform, endPoint);
                Invoke("DisableAllBeams", 0.12f);

                Collider[] colliders = Physics.OverlapSphere(hit.point, 5f);
                System.Collections.Generic.HashSet<Rigidbody> pushedRigidbodies = new System.Collections.Generic.HashSet<Rigidbody>();

                foreach (Collider nearby in colliders)
                {
                    Rigidbody rb = nearby.GetComponentInParent<Rigidbody>();
                    if (rb != null && !pushedRigidbodies.Contains(rb))
                    {
                        rb.AddExplosionForce(500f, hit.point, 5f);
                        pushedRigidbodies.Add(rb); 
                    }
                }
            }
        }
        else
        {
            endPoint = playerCamera.transform.position + (playerCamera.transform.forward * 50f);
            
            if (currentGun == 2) { DrawLaserBeam(eraseBeam, eraseVisual.transform, endPoint); Invoke("DisableAllBeams", 0.08f); }
            if (currentGun == 3) { DrawLaserBeam(combustBeam, combustVisual.transform, endPoint); Invoke("DisableAllBeams", 0.12f); }
        }
    }

    void StopFiring()
    {
        if (grabbedObject != null)
        {
            grabbedObject.GetComponent<Rigidbody>().isKinematic = false;
            grabbedObject = null;
        }
        DisableAllBeams();
    }

    // NEW: Draw laser beam by converting global hit point back to local space relative to the active gun
    void DrawLaserBeam(LineRenderer beam, Transform gunTransform, Vector3 globalEndPos)
    {
        if (beam == null) return;
        beam.gameObject.SetActive(true);
        
        // Point 0 is local center origin (0,0,0) of the gun model itself
        beam.SetPosition(0, Vector3.zero); 
        
        // Convert the global 3D hit point back into local coordinates for the gun
        Vector3 localEndPos = gunTransform.InverseTransformPoint(globalEndPos);
        beam.SetPosition(1, localEndPos);   
    }

    void DisableAllBeams()
    {
        if (kineticBeam != null) kineticBeam.gameObject.SetActive(false);
        if (eraseBeam != null) eraseBeam.gameObject.SetActive(false);
        if (combustBeam != null) combustBeam.gameObject.SetActive(false);
    }
}
