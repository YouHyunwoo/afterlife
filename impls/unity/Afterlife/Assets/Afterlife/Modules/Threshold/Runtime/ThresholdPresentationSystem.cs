using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Modules.Threshold.Unity
{
    /// <summary>Systems operate on registered data components. Adding a child page/pose requires no scene-specific switch.</summary>
    internal static class ThresholdPresentationSystem
    {
        internal const float PortalArrival = .38f;
        internal const float PortalCutover = .48f;
        public static void Pages(List<ThresholdPart> parts, ThresholdSession state, float modal, float delta)
        {
            foreach (var part in parts)
            {
                if (!(part is ThresholdPage page)) continue;
                var group = page.Group != null ? page.Group : page.GetComponent<CanvasGroup>();
                float alpha;
                if (page.IsModal) alpha = modal;
                else if (!state.IsMoving) alpha = page.Screen == state.Current ? 1 : 0;
                else if (page.Screen == state.From) alpha = 1 - Mathf.Clamp01(state.Progress / .16f);
                else if (page.Screen == state.To) alpha = Mathf.InverseLerp(state.IsResultEntry ? .55f : .83f, 1, state.Progress);
                else alpha = 0;
                group.alpha = alpha;
                group.interactable = !state.IsMoving && alpha > .98f && (page.IsModal ? state.ModalOpen : !state.ModalOpen && modal < .01f);
                group.blocksRaycasts = group.interactable;
            }
        }

        public static void Camera(List<ThresholdPart> parts, ThresholdSession state, Camera camera, Transform portalEntry, bool reducedMotion)
        {
            if (camera == null) return;
            ThresholdCameraPose from = null, to = null;
            foreach (var part in parts)
            {
                if (!(part is ThresholdCameraPose pose)) continue;
                if (pose.Screen == ThresholdSession.BackgroundFor(state.IsMoving ? state.From : state.Current)) from = pose;
                if (pose.Screen == ThresholdSession.BackgroundFor(state.To)) to = pose;
            }
            if (from == null) return;
            if (to == null) to = from;
            float t = 0;
            if (state.IsMoving) t = state.IsCameraTravel ? TravelEase(Mathf.InverseLerp(.12f, .94f, state.Progress)) : (state.Progress >= .48f ? 1 : 0);
            camera.transform.SetPositionAndRotation(Vector3.Lerp(from.transform.position, to.transform.position, t), Quaternion.Slerp(from.transform.rotation, to.transform.rotation, t));
            camera.fieldOfView = Mathf.Lerp(from.FieldOfView, to.FieldOfView, t);
            if (state.IsMoving && state.From == ThresholdScreen.Lobby && state.To == ThresholdScreen.Playing
                && state.Progress < PortalCutover && portalEntry != null && !reducedMotion)
                PortalApproach(camera, from.transform.position, from.transform.rotation, from.FieldOfView, portalEntry, state.Progress);
        }

        internal static void PortalApproach(Camera camera, Vector3 fromPosition, Quaternion fromRotation,
            float fromFieldOfView, Transform portalEntry, float progress)
        {
            float approach = Mathf.Clamp01(progress / PortalArrival);
            approach = approach * approach * approach;
            // Stay on the incoming side, outside displacement and the camera near plane.
            // Portal height ranges within +/- .13 local units; reserve extra clearance.
            float clearance = Mathf.Max(.5f, Mathf.Abs(portalEntry.lossyScale.z) * .13f + camera.nearClipPlane + .12f);
            float side = Vector3.Dot(fromPosition - portalEntry.position, portalEntry.forward) >= 0 ? 1 : -1;
            Vector3 destination = portalEntry.position + portalEntry.forward * (side * clearance);
            camera.transform.position = Vector3.Lerp(fromPosition, destination, approach);
            var direction = portalEntry.position - camera.transform.position;
            if (direction.sqrMagnitude > .001f)
                camera.transform.rotation = Quaternion.Slerp(fromRotation, Quaternion.LookRotation(direction), approach);
            camera.fieldOfView = Mathf.Lerp(fromFieldOfView, 68, approach);
        }
        // Quintic ease-in/out: zero velocity and acceleration at either end; the reverse route follows the same path.
        private static float TravelEase(float t) => t * t * t * (t * (t * 6 - 15) + 10);
    }
}
