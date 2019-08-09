﻿using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Power;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using TweakScale;
using UnityEngine;

namespace FNPlugin.Extensions
{
    public static class VesselExtension
    {
        public static bool PersistHeading(this Vessel vessel, bool forceRotation = false)
        {
            var canPersistDirection = vessel.situation == Vessel.Situations.SUB_ORBITAL || vessel.situation == Vessel.Situations.ESCAPING || vessel.situation == Vessel.Situations.ORBITING;
            var sasIsActive = vessel.ActionGroups[KSPActionGroup.SAS];

            if (canPersistDirection && sasIsActive)
            {
                var requestedDirection = Vector3d.zero;
                var universalTime = Planetarium.GetUniversalTime();
                var vesselPosition = vessel.orbit.getPositionAtUT(universalTime);

                if (vessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.Prograde)
                    requestedDirection = vessel.obt_velocity.normalized;
                else if (vessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.Retrograde)
                    requestedDirection = -vessel.obt_velocity.normalized;
                else if (vessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.Maneuver)
                    requestedDirection = vessel.patchedConicSolver.maneuverNodes.Count > 0 ? vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(vessel.orbit) : vessel.obt_velocity.normalized;
                else if (vessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.Target)
                    requestedDirection = (vessel.targetObject.GetOrbit().getPositionAtUT(universalTime) - vesselPosition).normalized;
                else if (vessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.AntiTarget)
                      requestedDirection = -(vessel.targetObject.GetOrbit().getPositionAtUT(universalTime) - vesselPosition).normalized;
                else if (vessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.Normal)
                    requestedDirection = Vector3.Cross(vessel.obt_velocity, (vesselPosition - vessel.mainBody.position)).normalized;
                else if (vessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.Antinormal)
                    requestedDirection = -Vector3.Cross(vessel.obt_velocity, (vesselPosition - vessel.mainBody.position)).normalized;
                else if (vessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.RadialIn)
                    requestedDirection = -Vector3.Cross(vessel.obt_velocity, Vector3.Cross(vessel.obt_velocity, (vesselPosition - vessel.mainBody.position))).normalized;
                else if (vessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.RadialOut)
                    requestedDirection = Vector3.Cross(vessel.obt_velocity, Vector3.Cross(vessel.obt_velocity, (vesselPosition - vessel.mainBody.position))).normalized;

                if (requestedDirection != Vector3d.zero)
                {
                    var vesselDirection = vessel.transform.up.normalized;

                    if (forceRotation || Vector3d.Dot(vesselDirection, requestedDirection) > 0.9)
                    {
                        vessel.transform.Rotate(Quaternion.FromToRotation(vesselDirection, requestedDirection).eulerAngles, Space.World);
                        vessel.SetRotation(vessel.transform.rotation);
                    }
                    else
                    {
                        var directionName = Enum.GetName(typeof(VesselAutopilot.AutopilotMode), vessel.Autopilot.Mode);
                        var message = "Persistant Thrust stopped - vessel is not facing " + directionName;
                        ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                        Debug.Log("[KSPI]: " + message);
                        TimeWarp.SetRate(0, true);
                        return false;
                    }
                }
            }
            return true;
        }
    }
}