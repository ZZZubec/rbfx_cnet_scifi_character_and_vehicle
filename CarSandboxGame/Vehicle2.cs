using System;
using System.Collections.Generic;
using Urho3DNet;

namespace CarSandboxGame
{
    public class Vehicle2 : LogicComponent
    {
        public const float CHASSIS_WIDTH = 1.5f;
        public const float ENGINE_POWER = 10.0f;
        public const float MAX_WHEEL_ANGLE = 22.5f;

        /// Storing points for emitters
        private readonly Vector3[] connectionPoints_ = new Vector3[4];

        /// Emitter data for saving.
        private readonly List<Node> particleEmitterNodeList_ = new List<Node>();

        private float engineForce_;
        private float brakingForce_;
        private float maxEngineForce_;
        private float wheelRadius_;
        private float suspensionRestLength_;
        private float wheelWidth_;
        private float suspensionStiffness_;
        private float suspensionDamping_;
        private float suspensionCompression_;
        private float wheelFriction_;
        private float rollInfluence_;
        private bool emittersCreated;

        /// Current left/right steering amount (-1 to 1.)
        private float steering_;

        /// Tmp storage for steering
        private float vehicleSteering_;

        /// Value to calculate acceleration.
        private Vector3 prevVelocity_;

        public Controls controls_ = new Controls();

        public float ctrl_forward_back;
        public float ctrl_left_right;

        public Vehicle2(Context context) : base(context)
        {
            UpdateEventMask = UpdateEvent.UseFixedupdate | UpdateEvent.UsePostupdate;
            engineForce_ = 0.0f;
            brakingForce_ = 50.0f;
            vehicleSteering_ = 0.0f;
            maxEngineForce_ = 2500.0f;
            wheelRadius_ = 0.25f;
            suspensionRestLength_ = 0.2f;
            wheelWidth_ = 0.25f;
            suspensionStiffness_ = 14.0f;
            suspensionDamping_ = 2.0f;
            suspensionCompression_ = 4.0f;
            wheelFriction_ = 1000.0f;
            rollInfluence_ = 0.12f;
            emittersCreated = false;
        }

        public Node AttachCamera { get; set; }

        public override void FixedUpdate(float timeStep)
        {
            base.FixedUpdate(timeStep);

            var newSteering = 0.0f;
            var accelerator = 0.0f;
            var brake = false;
            var vehicle = Node.GetComponent<RaycastVehicle>();
            // Read controls
            newSteering = ctrl_left_right;
            accelerator = ctrl_forward_back;
            //if (0 != (controls_.Buttons & CTRL_BRAKE)) brake = true;
            // When steering, wake up the wheel rigidbodies so that their orientation is updated
            if (newSteering != 0.0f)
                SetSteering(GetSteering() * 0.95f + newSteering * 0.05f);
            else
                SetSteering(GetSteering() * 0.8f + newSteering * 0.2f);
            // Set front wheel angles
            vehicleSteering_ = steering_;
            var wheelIndex = 0;
            vehicle.SetSteeringValue(wheelIndex, vehicleSteering_);
            wheelIndex = 1;
            vehicle.SetSteeringValue(wheelIndex, vehicleSteering_);
            // apply forces
            engineForce_ = maxEngineForce_ * accelerator;
            // 2x wheel drive
            vehicle.SetEngineForce(2, engineForce_);
            vehicle.SetEngineForce(3, engineForce_);
            for (var i = 0; i < vehicle.NumWheels; i++)
                if (brake)
                    vehicle.SetBrake(i, brakingForce_);
                else
                    vehicle.SetBrake(i, 0.0f);
        }

        public override void FixedPostUpdate(float timeStep)
        {
            base.FixedPostUpdate(timeStep);
            var vehicle = Node.GetComponent<RaycastVehicle>();
            var vehicleBody = Node.GetComponent<RigidBody>();
            var velocity = vehicleBody.LinearVelocity;
            var accel = (velocity - prevVelocity_) / timeStep;
            var planeAccel = new Vector3(accel.X, 0.0f, accel.Z).Length;

            prevVelocity_ = velocity;
        }

        /// Get steering value.
        private float GetSteering()
        {
            return steering_;
        }

        /// Set steering value.
        private void SetSteering(float steering)
        {
            steering_ = steering;
        }

        /// Get wheel radius.
        private float GetWheelRadius()
        {
            return wheelRadius_;
        }

        /// Get wheel width.
        private float GetWheelWidth()
        {
            return wheelWidth_;
        }

        public void Init()
        {
            var vehicle = Node.CreateComponent<RaycastVehicle>();
            vehicle.Init();
            var hullBody = Node.GetComponent<RigidBody>();
            hullBody.Mass = 800.0f;
            hullBody.LinearDamping = 0.5f; // Some air resistance
            hullBody.AngularDamping = 0.5f;
            hullBody.CollisionLayer = 1;
            // This function is called only from the main program when initially creating the vehicle, not on scene load
            var cache = GetSubsystem<ResourceCache>();
            var hullObject = Node.CreateComponent<StaticModel>();
            // Setting-up collision shape
            var hullColShape = Node.CreateComponent<CollisionShape>();
            var v3BoxExtents = Vector3.One;
            hullColShape.SetBox(new Vector3(1, 0.1f, 1));
            hullObject.SetModel(cache.GetResource<Model>("Models/Car_body.mdl"));
            //hullObject.SetMaterial(cache.GetResource<Material>("Materials/Stone.xml"));
            hullObject.CastShadows = true;

            var nLadle = Node.CreateChild("Ladle");
            nLadle.Position = new Vector3(0.0f, 0.7f, -0.3f);
            var ladle = nLadle.CreateComponent<StaticModel>();
            ladle.SetModel(cache.GetResource<Model>("Models/Ladle.mdl"));

            var connectionHeight = 0.2f;
            var isFrontWheel = true;
            var wheelDirection = new Vector3(0, -1);
            var wheelAxle = new Vector3(-1);
            // We use not scaled coordinates here as everything will be scaled.
            // Wheels are on bottom at edges of the chassis
            // Note we don't set wheel nodes as children of hull (while we could) to avoid scaling to affect them.
            var wheelX = CHASSIS_WIDTH / 2.0f - wheelWidth_;
            // Front left
            connectionPoints_[0] = new Vector3(-wheelX, connectionHeight, 1.1f - GetWheelRadius() * 2.0f);
            // Front right
            connectionPoints_[1] = new Vector3(wheelX, connectionHeight, 1.1f - GetWheelRadius() * 2.0f);
            // Back left
            connectionPoints_[2] = new Vector3(-wheelX, connectionHeight, -1.1f + GetWheelRadius() * 2.0f);
            // Back right
            connectionPoints_[3] = new Vector3(wheelX, connectionHeight, -1.1f + GetWheelRadius() * 2.0f);
            var LtBrown = new Color(0.972f, 0.780f, 0.412f);
            for (var id = 0; id < connectionPoints_.Length; id++)
            {
                var wheelNode = Scene.CreateChild();
                var connectionPoint = connectionPoints_[id];
                // Front wheels are at front (z > 0)
                // back wheels are at z < 0
                // Setting rotation according to wheel position
                isFrontWheel = connectionPoints_[id].Z > 0.0f;
                wheelNode.Rotation = connectionPoint.X >= 0.0
                    ? new Quaternion(0.0f, 0.0f, -90.0f)
                    : new Quaternion(0.0f, 0.0f, 90.0f);
                wheelNode.WorldPosition = Node.WorldPosition + Node.WorldRotation * connectionPoints_[id];
                vehicle.AddWheel(wheelNode, wheelDirection, wheelAxle, suspensionRestLength_, wheelRadius_,
                    isFrontWheel);
                vehicle.SetWheelSuspensionStiffness(id, suspensionStiffness_);
                vehicle.SetWheelDampingRelaxation(id, suspensionDamping_);
                vehicle.SetWheelDampingCompression(id, suspensionCompression_);
                vehicle.SetWheelFrictionSlip(id, wheelFriction_);
                vehicle.SetWheelRollInfluence(id, rollInfluence_);
                //wheelNode.SetScale(new Vector3(1.0f, 0.65f, 1.0f));
                var pWheel = wheelNode.CreateComponent<StaticModel>();
                pWheel.SetModel(cache.GetResource<Model>("Models/Wheel.001.mdl"));
                //pWheel.SetMaterial(cache.GetResource<Material>("Materials/Stone.xml"));
                pWheel.CastShadows = true;
                CreateEmitter(connectionPoints_[id]);
            }

            emittersCreated = true;
            vehicle.ResetWheels();
        }

        private void CreateEmitter(Vector3 place)
        {
            var cache = GetSubsystem<ResourceCache>();
            var emitter = Scene.CreateChild();
            emitter.WorldPosition = Node.WorldPosition + Node.WorldRotation * place + new Vector3(0, -wheelRadius_);
            var particleEmitter = emitter.CreateComponent<ParticleEmitter>();
            //particleEmitter.Effect = cache.GetResource<ParticleEffect>("Particle/Dust.xml");
            particleEmitter.IsEmitting = false;
            particleEmitterNodeList_.Add(emitter);
            emitter.IsTemporary = true;
        }

        /// Applying attributes
        public override void ApplyAttributes()
        {
            var vehicle = Node.GetOrCreateComponent<RaycastVehicle>();
            if (emittersCreated)
                return;
            foreach (var connectionPoint in connectionPoints_) CreateEmitter(connectionPoint);
            emittersCreated = true;
        }

    }
}