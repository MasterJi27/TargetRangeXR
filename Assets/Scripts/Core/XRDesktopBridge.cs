using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using TargetRange.Weapon;

namespace TargetRange.Core
{
    /// <summary>One input owner for desktop play; XR simulation is an explicit, separate mode.</summary>
    public sealed class XRDesktopBridge : MonoBehaviour
    {
        [SerializeField] private GameManager game;
        [SerializeField] private Pistol pistol;
        [SerializeField] private Camera headCamera;
        [SerializeField] private Transform xrOrigin;
        [SerializeField] private GameObject simulatorRoot;
        [SerializeField] private float mouseSensitivity = 0.09f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private Vector3 gunViewOffset = new Vector3(0.20f, -0.22f, 0.40f);
        private readonly List<Behaviour> rigDrivers = new List<Behaviour>();
        private readonly List<GameObject> controllers = new List<GameObject>();
        private InputActionMap input;
        private InputAction move, look, keyboardAim, equip, fire, escape, restart, switchMode, quit, help;
        private Canvas[] simulatorCanvases;
        private bool helpVisible;
        private float pitch, yaw;
        private bool physicalHeadset;
        private Vector3 headHome, originHome;
        public bool IsDesktopMode { get; private set; }
        public bool IsCaptured => Cursor.lockState == CursorLockMode.Locked;
        public bool HasPhysicalHeadset => physicalHeadset;

        private void Awake()
        {
            var displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);
            physicalHeadset = XRSettings.isDeviceActive || displays.Exists(d => d.running);
            originHome = xrOrigin.position;
            headHome = headCamera.transform.localPosition;
            foreach (var driver in xrOrigin.GetComponentsInChildren<Behaviour>(true))
                if (driver.enabled && driver != headCamera && !(driver is AudioListener) &&
                    driver.GetType().Name != "UniversalAdditionalCameraData") rigDrivers.Add(driver);
            foreach (Transform t in xrOrigin.GetComponentsInChildren<Transform>(true))
                if (t.name == "Left Controller" || t.name == "Right Controller") controllers.Add(t.gameObject);
            input = new InputActionMap("Desktop Range");
            move = input.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            look = input.AddAction("Look", InputActionType.Value, "<Mouse>/delta");
            keyboardAim = input.AddAction("Keyboard Aim", InputActionType.Value);
            keyboardAim.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            equip = input.AddAction("Equip", InputActionType.Button, "<Keyboard>/e");
            equip.AddBinding("<Keyboard>/enter");
            fire = input.AddAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
            fire.AddBinding("<Keyboard>/space");
            escape = input.AddAction("Release Cursor", InputActionType.Button, "<Keyboard>/escape");
            restart = input.AddAction("Replay", InputActionType.Button, "<Keyboard>/r");
            switchMode = input.AddAction("Switch Mode", InputActionType.Button, "<Keyboard>/f2");
            quit = input.AddAction("Exit Results", InputActionType.Button, "<Keyboard>/q");
            help = input.AddAction("Simulator Help", InputActionType.Button, "<Keyboard>/f1");
        }
        private void OnEnable()
        {
            input.Enable();
            game.StateChanged += OnStateChanged;
            game.RoundReset += ResetPose;
        }
        private void Start() => ApplyMode(!physicalHeadset);
        private void OnDisable()
        {
            input.Disable();
            game.StateChanged -= OnStateChanged;
            game.RoundReset -= ResetPose;
            Capture(false);
        }
        private void OnDestroy() => input?.Dispose();
        private void OnApplicationFocus(bool focused) { if (!focused && IsDesktopMode) Capture(false); }
        public void SetSimulatorMode(bool simulator)
        {
            if (physicalHeadset) return;
            game.RestartRound();
            ApplyMode(!simulator);
        }
        private void ApplyMode(bool desktop)
        {
            pistol.ReturnToTable();
            IsDesktopMode = desktop;
            foreach (var driver in rigDrivers) if (driver != null) driver.enabled = !desktop;
            foreach (var controller in controllers) controller.SetActive(!desktop);
            simulatorRoot.SetActive(!desktop && !physicalHeadset);
            simulatorCanvases = null;
            helpVisible = false;
            ResetPose();
            Capture(false);
        }
        private void ResetPose()
        {
            pitch = yaw = 0f;
            xrOrigin.SetPositionAndRotation(originHome, Quaternion.identity);
            if (IsDesktopMode) headCamera.transform.position = xrOrigin.TransformPoint(new Vector3(0f, 1.65f, 0f));
            else headCamera.transform.localPosition = headHome;
            headCamera.transform.localRotation = Quaternion.identity;
            Capture(false);
        }
        private void OnStateChanged(GameState state)
        {
            if (IsDesktopMode && state != GameState.Playing) Capture(false);
        }
        public void EquipDesktopPistol()
        {
            if (!IsDesktopMode || game.State == GameState.Ended) return;
            pistol.SetDesktopHeld(true);
            Capture(true);
            AimPistol();
        }
        public void Resume() { if (IsDesktopMode && game.State == GameState.Playing) Capture(true); }
        public void Replay() => game.RestartRound();
        private void Update()
        {
            if (switchMode.WasPressedThisFrame() && !physicalHeadset)
            {
                SetSimulatorMode(IsDesktopMode);
                return;
            }
            if (!IsDesktopMode)
            {
                if (!physicalHeadset)
                {
                    if (simulatorCanvases == null || simulatorCanvases.Length == 0)
                        simulatorCanvases = simulatorRoot.GetComponentsInChildren<Canvas>(true);
                    if (help.WasPressedThisFrame()) helpVisible = !helpVisible;
                    foreach (var canvas in simulatorCanvases) canvas.enabled = helpVisible;
                }
                return;
            }
            if (game.State == GameState.Ended)
            {
                if (restart.WasPressedThisFrame() || equip.WasPressedThisFrame()) Replay();
                else if (quit.WasPressedThisFrame()) game.QuitGame();
                return;
            }
            if (escape.WasPressedThisFrame()) { Capture(!IsCaptured && game.State == GameState.Playing); return; }
            if (equip.WasPressedThisFrame())
            {
                if (pistol.IsHeld && IsCaptured) pistol.ReturnToTable();
                else if (!pistol.IsHeld) EquipDesktopPistol();
                else Resume();
                return;
            }
            if (game.State != GameState.Playing || !IsCaptured) return;
            Vector2 delta = look.ReadValue<Vector2>() * mouseSensitivity + keyboardAim.ReadValue<Vector2>() * (45f * Time.unscaledDeltaTime);
            yaw += delta.x;
            pitch = Mathf.Clamp(pitch - delta.y, -65f, 65f);
            headCamera.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector2 movement = move.ReadValue<Vector2>();
            Vector3 forward = Vector3.ProjectOnPlane(headCamera.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(headCamera.transform.right, Vector3.up).normalized;
            Vector3 position = xrOrigin.position + (forward * movement.y + right * movement.x) * (moveSpeed * Time.deltaTime);
            xrOrigin.position = new Vector3(Mathf.Clamp(position.x, -1.05f, 1.05f), originHome.y, Mathf.Clamp(position.z, -1.8f, -0.15f));
            if (!pistol.IsHeld) return;
            AimPistol();
            if (fire.WasPressedThisFrame()) pistol.TryFire();
        }
        public void AimPistol()
        {
            Vector3 position = headCamera.transform.TransformPoint(gunViewOffset);
            Ray ray = headCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            Vector3 point = Physics.Raycast(ray, out RaycastHit hit, 60f, game.Config.bulletMask, QueryTriggerInteraction.Ignore)
                ? hit.point : ray.GetPoint(60f);
            Quaternion rotation = headCamera.transform.rotation;
            pistol.transform.SetPositionAndRotation(position, rotation);
            // Converge the offset barrel onto the centre reticle, including the local muzzle offset.
            for (int i = 0; i < 3; i++)
            {
                rotation = Quaternion.FromToRotation(pistol.Muzzle.forward, point - pistol.Muzzle.position) * rotation;
                pistol.transform.rotation = rotation;
            }
        }
        private static void Capture(bool capture)
        {
            Cursor.lockState = capture ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !capture;
        }
    }
}
