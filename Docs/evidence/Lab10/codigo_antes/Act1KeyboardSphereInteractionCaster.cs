using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

namespace CrisisVR
{
    /// <summary>
    /// Caster near con buffers reutilizables que crecen solo si el query se satura.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Act1KeyboardSphereInteractionCaster : InteractionCasterBase
    {
        const int InitialCapacity = 64;

        Collider[] overlapHits = new Collider[InitialCapacity];
        RaycastHit[] sphereCastHits = new RaycastHit[InitialCapacity];

        [SerializeField] LayerMask physicsLayerMask = -1;
        [SerializeField] QueryTriggerInteraction physicsTriggerInteraction = QueryTriggerInteraction.Ignore;
        [SerializeField, Min(0f)] float castRadius = 0.1f;

        bool firstFrame = true;
        Vector3 lastOrigin;
        PhysicsScene physicsScene;

        public LayerMask PhysicsLayerMask { get => physicsLayerMask; set => physicsLayerMask = value; }
        public QueryTriggerInteraction PhysicsTriggerInteraction { get => physicsTriggerInteraction; set => physicsTriggerInteraction = value; }
        public float CastRadius { get => castRadius; set => castRadius = Mathf.Max(0f, value); }

        protected override void OnEnable()
        {
            base.OnEnable();
            firstFrame = true;
            lastOrigin = Vector3.zero;
        }

        public override bool TryGetColliderTargets(XRInteractionManager interactionManager, List<Collider> targets)
        {
            if (!base.TryGetColliderTargets(interactionManager, targets))
                return false;

            Vector3 origin = effectiveCastOrigin.position;
            float radius = castRadius * transform.lossyScale.x;
            BurstPhysicsUtils.GetSphereOverlapParameters(lastOrigin, origin, out Vector3 direction, out float distanceSquared, out float distance);

            bool hasHit;
            if (firstFrame || distanceSquared < 0.001f)
            {
                int count = OverlapSphere(origin, radius);
                for (int i = 0; i < count; i++)
                    targets.Add(overlapHits[i]);
                hasHit = count > 0;
            }
            else
            {
                int count = SphereCast(lastOrigin, radius, direction, distance);
                for (int i = 0; i < count; i++)
                    targets.Add(sphereCastHits[i].collider);
                hasHit = count > 0;
            }

            lastOrigin = origin;
            firstFrame = false;
            return hasHit;
        }

        protected override bool InitializeCaster()
        {
            if (!isInitialized)
            {
                physicsScene = gameObject.scene.GetPhysicsScene();
                isInitialized = true;
            }

            return isInitialized;
        }

        int OverlapSphere(Vector3 origin, float radius)
        {
            while (true)
            {
                int count = physicsScene.OverlapSphere(origin, radius, overlapHits, physicsLayerMask, physicsTriggerInteraction);
                if (count < overlapHits.Length)
                    return count;

                Array.Resize(ref overlapHits, overlapHits.Length * 2);
            }
        }

        int SphereCast(Vector3 origin, float radius, Vector3 direction, float distance)
        {
            while (true)
            {
                int count = physicsScene.SphereCast(origin, radius, direction, sphereCastHits, distance, physicsLayerMask, physicsTriggerInteraction);
                if (count < sphereCastHits.Length)
                    return count;

                Array.Resize(ref sphereCastHits, sphereCastHits.Length * 2);
            }
        }
    }
}
