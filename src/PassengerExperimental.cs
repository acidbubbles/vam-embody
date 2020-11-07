using System.Linq;
using MeshVR;
using UnityEngine;

public class PassengerExperimental : MVRScript
{
    private JSONStorableBool _activeJSON;
    private Rigidbody _link;
    private Transform _cameraRig;
    private Transform _cameraRigParent;
    private Quaternion _cameraRigRotationBackup;
    private Vector3 _cameraRigPositionBackup;
    private GameObject _debug;

    public override void Init()
    {
        _activeJSON = new JSONStorableBool("Active", false, val =>
        {
            if (val)
                Activate();
            else
                Deactivate();
        });
        RegisterBool(_activeJSON);
        CreateToggle(_activeJSON);

        // var x = SuperController.singleton.centerCameraTarget.transform;
        // while(true)
        // {
        //     SuperController.LogMessage($"{x.name}: {x.localPosition}");
        //     x = x.parent;
        //     if(x == null) break;
        // }
        // SuperController.LogMessage($"DONE");
    }

    public void OnDisable()
    {
        if (_activeJSON.val) _activeJSON.val = false;
    }

    private void Activate()
    {
        _link = containingAtom.rigidbodies.First(rb => rb.name == "head");
        _debug = Cross(Color.red);

        // GlobalSceneOptions.singleton.disableNavigation = true;

        Camera.onPreCull += OnPreCull;

        // _cameraRig = SuperController.singleton.GetAtomByUid("[CameraRig]").transform.GetChild(0);
        _cameraRig = SuperController.singleton.centerCameraTarget.transform.parent;
        _cameraRigParent = _cameraRig.transform.parent;

        _cameraRigRotationBackup = _cameraRig.transform.localRotation;
        _cameraRigPositionBackup = _cameraRig.transform.localPosition;

        _cameraRig.transform.SetParent(_link.transform, false);

        // for (var i = 0; i < _cameraRig.transform.childCount; i++)
        // {
        //     var c = _cameraRig.transform.GetChild(i);
        //     SuperController.LogMessage($"{c}");
        //     foreach (var x in c.GetComponents<MonoBehaviour>())
        //         SuperController.LogMessage($"  {x}");
        // }
    }

    private void Deactivate()
    {
        Camera.onPreCull -= OnPreCull;

        Destroy(_debug);

        // GlobalSceneOptions.singleton.disableNavigation = false;

        _cameraRig.transform.SetParent(_cameraRigParent, true);
        _cameraRig.transform.localRotation = _cameraRigRotationBackup;
        _cameraRig.transform.localPosition = _cameraRigPositionBackup;

        _cameraRig = null;
        _cameraRigParent = null;

        _link = null;
    }

    public void Update()
    {
        if (!_activeJSON.val)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                _activeJSON.val = true;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space))
        {
            _activeJSON.val = false;
            return;
        }

        // PositionCamera();
    }

    public void LateUpdate()
    {
        if (!_activeJSON.val) return;

        // PositionCamera();
    }

    public void FixedUpdate()

    {
        if (!_activeJSON.val) return;

        PositionCamera();
    }

    private void OnPreCull(Camera cam)
    {
        // PositionCamera();
    }

    public void PositionCamera()
    {
        _cameraRig.localPosition = Vector3.zero;
        // _cameraRig.transform.position = _link.position;
        // _cameraRig.transform.rotation = _link.rotation;
    }

        public static GameObject Cross(Color color) {
            var go = new GameObject();
            var size = 0.8f; var width = 0.005f;
            CreatePrimitive(go.transform, PrimitiveType.Cube, Color.red).transform.localScale = new Vector3(size, width, width);
            CreatePrimitive(go.transform, PrimitiveType.Cube, Color.green).transform.localScale = new Vector3(width, size, width);
            CreatePrimitive(go.transform, PrimitiveType.Cube, Color.blue).transform.localScale = new Vector3(width, width, size);
            CreatePrimitive(go.transform, PrimitiveType.Sphere, color).transform.localScale = new Vector3(size / 8f, size / 8f, size / 8f);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            return go;
        }

        public static GameObject CreatePrimitive(Transform parent, PrimitiveType type, Color color) {
            var go = GameObject.CreatePrimitive(type);
            go.GetComponent<Renderer>().material = new Material(Shader.Find("Sprites/Default")) { color = color, renderQueue = 4000 };
            foreach (var c in go.GetComponents<Collider>()) { c.enabled = false; Destroy(c); }
            go.transform.parent = parent;
            return go;
        }
}
