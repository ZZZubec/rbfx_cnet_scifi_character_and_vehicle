using System.Diagnostics;
using Urho3DNet;

namespace CarSandboxGame
{
    public class Player : LogicComponent
    {
        public Camera Camera { get; set; }

        public Node SelectedNode
        {
            get { return _selectedNode; }
            set
            {
                if (_selectedNode != value)
                {
                    if (_selectedNode != null)
                    {
                        _selectedNode.SendEvent("Unselected", Context.EventDataMap);
                    }

                    _selectedNode = value;
                    if (_selectedNode != null)
                    {
                        _selectedNode.SendEvent("Selected", Context.EventDataMap);
                    }
                }
            }
        }

        public Node AttractionTarget { get; set; }

        public RigidBody BodyInArms { get; set; }

        public Constraint Constraint { get; set; }

        public InputMap InputMap { get; set; }

        public Vehicle2 vehicle { get; set; }
        
        public bool isCarInput = false;
        private float oldChangeCamera = -1.0f;
        public Node cameraRoot;

        public Player(Context context) : base(context)
        {
            UpdateEventMask = UpdateEvent.UseUpdate | UpdateEvent.UseFixedupdate;
            _raycastResult = new PhysicsRaycastResult();
        }

        public override void Update(float timeStep)
        {
            base.Update(timeStep);
            

            var usePressed = InputMap.Evaluate("Use") > 0.5f;
            if (usePressed != _usePressed)
            {
                _usePressed = usePressed;
                if (_usePressed)
                {
                    BodyInArms = SelectedNode?.GetComponent<RigidBody>();
                    if (BodyInArms != null && BodyInArms.Mass > 0)
                    {
                        Constraint.OtherBody = BodyInArms;
                        SelectedNode = null;
                    }
                }
                else
                {
                    Constraint.OtherBody = null;
                    BodyInArms = null;
                }
            }
            
            if(oldChangeCamera >= 0.0f)
            {
                oldChangeCamera -= timeStep;
            }
            else
            {
                if (InputMap.Evaluate("ChangeCamera") > 0.5f)
                {
                    oldChangeCamera = 1.0f;
                    isCarInput = !isCarInput;
                    if(isCarInput)
                    {
                        cameraRoot = Camera.Node.Parent;
                        Camera.Node.Parent = vehicle.AttachCamera;
                        Camera.Node.Direction = Vector3.Forward;
                    }
                    else
                    {
                        Camera.Node.Parent = cameraRoot;
                        Camera.Node.Direction = Vector3.Forward;
                    }
                }
            }


            if (BodyInArms == null)
            {
                if (InputMap.Evaluate("Select") < 0.5f)
                {

                }
                var world = Scene.GetComponent<PhysicsWorld>();
                world.RaycastSingle(_raycastResult, new Ray(Camera.Node.WorldPosition, Camera.Node.WorldDirection),
                    4.0f - Camera.Node.Position.Z);
                var selectedNode = _raycastResult.Body?.Node;
                SelectedNode = selectedNode;
            }

            vehicle.ctrl_forward_back = 0.0f;
            vehicle.ctrl_left_right = 0.0f;
            
            if(isCarInput)
            {

                if (InputMap.Evaluate("Forward") > 0.5f)
                {
                    vehicle.ctrl_forward_back = 1.0f;
                }
                else if (InputMap.Evaluate("Back") > 0.5f)
                {
                    vehicle.ctrl_forward_back = -1.0f;
                }
                if (InputMap.Evaluate("Left") > 0.5)
                {
                    vehicle.ctrl_left_right = -1.0f;
                }
                else if (InputMap.Evaluate("Right") > 0.5)
                {
                    vehicle.ctrl_left_right = 1.0f;
                }
            }
        }

        private PhysicsRaycastResult _raycastResult;
        private bool _usePressed;
        private Node _selectedNode;
    }
}