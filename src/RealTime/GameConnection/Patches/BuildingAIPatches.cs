﻿// <copyright file="BuildingAIPatches.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace RealTime.GameConnection.Patches
{
    using System;
    using System.Reflection;
    using ColossalFramework.Math;
    using RealTime.CustomAI;
    using RealTime.Simulation;
    using SkyTools.Patching;
    using UnityEngine;

    /// <summary>
    /// A static class that provides the patch objects for the building AI game methods.
    /// </summary>
    internal static class BuildingAIPatches
    {
        /// <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeAI { get; set; }

        /// <summary>Gets or sets the weather information service.</summary>
        public static IWeatherInfo WeatherInfo { get; set; }

        /// <summary>Gets the patch for the commercial building AI class.</summary>
        public static IPatch CommercialSimulation { get; } = new CommercialBuildingA_SimulationStepActive();

        /// <summary>Gets the patch for the private building AI method 'HandleWorkers'.</summary>
        public static IPatch HandleWorkers { get; } = new PrivateBuildingAI_HandleWorkers();

        /// <summary>Gets the patch for the private building AI method 'GetConstructionTime'.</summary>
        public static IPatch GetConstructionTime { get; } = new PrivateBuildingAI_GetConstructionTime();

        /// <summary>Gets the patch for the private building AI method 'ShowConsumption'.</summary>
        public static IPatch PrivateShowConsumption { get; } = new PrivateBuildingAI_ShowConsumption();

        /// <summary>Gets the patch for the player building AI method 'ShowConsumption'.</summary>
        public static IPatch PlayerShowConsumption { get; } = new PlayerBuildingAI_ShowConsumption();

        /// <summary>Gets the patch for the building AI method 'CalculateUnspawnPosition'.</summary>
        public static IPatch CalculateUnspawnPosition { get; } = new BuildingAI_CalculateUnspawnPosition();

        /// <summary>Gets the patch for the building AI method 'GetUpgradeInfo'.</summary>
        public static IPatch GetUpgradeInfo { get; } = new PrivateBuildingAI_GetUpgradeInfo();

        /// <summary>Gets the patch for the building manager method 'CreateBuilding'.</summary>
        public static IPatch CreateBuilding { get; } = new BuildingManager_CreateBuilding();

        private sealed class CommercialBuildingA_SimulationStepActive : PatchBase
        {
            protected override MethodInfo GetMethod()
            {
                return typeof(CommercialBuildingAI).GetMethod(
                    "SimulationStepActive",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(ushort), typeof(Building).MakeByRefType(), typeof(Building.Frame).MakeByRefType() },
                    new ParameterModifier[0]);
            }

#pragma warning disable SA1313 // Parameter names must begin with lower-case letter
            private static bool Prefix(ref Building buildingData, ref byte __state)
            {
                __state = buildingData.m_outgoingProblemTimer;
                if (buildingData.m_customBuffer2 > 0)
                {
                    // Simulate some goods become spoiled; additionally, this will cause the buildings to never reach the 'stock full' state.
                    // In that state, no visits are possible anymore, so the building gets stuck
                    --buildingData.m_customBuffer2;
                }

                return true;
            }

            private static void Postfix(ushort buildingID, ref Building buildingData, byte __state)
            {
                if (__state != buildingData.m_outgoingProblemTimer)
                {
                    RealTimeAI?.ProcessBuildingProblems(buildingID, __state);
                }
            }
#pragma warning restore SA1313 // Parameter names must begin with lower-case letter
        }

        private sealed class PrivateBuildingAI_HandleWorkers : PatchBase
        {
            protected override MethodInfo GetMethod()
            {
                Type refInt = typeof(int).MakeByRefType();

                return typeof(PrivateBuildingAI).GetMethod(
                    "HandleWorkers",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(ushort), typeof(Building).MakeByRefType(), typeof(Citizen.BehaviourData).MakeByRefType(), refInt, refInt, refInt },
                    new ParameterModifier[0]);
            }

#pragma warning disable SA1313 // Parameter names must begin with lower-case letter
            private static bool Prefix(ref Building buildingData, ref byte __state)
            {
                __state = buildingData.m_workerProblemTimer;
                return true;
            }

            private static void Postfix(ushort buildingID, ref Building buildingData, byte __state)
            {
                if (__state != buildingData.m_workerProblemTimer)
                {
                    RealTimeAI?.ProcessWorkerProblems(buildingID, __state);
                }
            }
#pragma warning restore SA1313 // Parameter names must begin with lower-case letter
        }

        private sealed class PrivateBuildingAI_GetConstructionTime : PatchBase
        {
            protected override MethodInfo GetMethod()
            {
                return typeof(PrivateBuildingAI).GetMethod(
                    "GetConstructionTime",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new Type[0],
                    new ParameterModifier[0]);
            }

#pragma warning disable SA1313 // Parameter names must begin with lower-case letter
            private static bool Prefix(ref int __result)
            {
                __result = RealTimeAI?.GetConstructionTime() ?? 0;
                return false;
            }
#pragma warning restore SA1313 // Parameter names must begin with lower-case letter
        }

        private sealed class PrivateBuildingAI_ShowConsumption : PatchBase
        {
            protected override MethodInfo GetMethod()
            {
                return typeof(PrivateBuildingAI).GetMethod(
                    "ShowConsumption",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(ushort), typeof(Building).MakeByRefType() },
                    new ParameterModifier[0]);
            }

#pragma warning disable SA1313 // Parameter names must begin with lower-case letter
            private static bool Prefix(ushort buildingID, ref bool __result)
            {
                if (InfoManager.instance.CurrentMode != InfoManager.InfoMode.None)
                {
                    return true;
                }

                if (RealTimeAI != null && RealTimeAI.ShouldSwitchBuildingLightsOff(buildingID))
                {
                    __result = false;
                    return false;
                }

                return true;
            }
#pragma warning restore SA1313 // Parameter names must begin with lower-case letter
        }

        private sealed class PlayerBuildingAI_ShowConsumption : PatchBase
        {
            protected override MethodInfo GetMethod()
            {
                return typeof(PlayerBuildingAI).GetMethod(
                    "ShowConsumption",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(ushort), typeof(Building).MakeByRefType() },
                    new ParameterModifier[0]);
            }

#pragma warning disable SA1313 // Parameter names must begin with lower-case letter
            private static bool Prefix(ushort buildingID, ref bool __result)
            {
                if (InfoManager.instance.CurrentMode != InfoManager.InfoMode.None)
                {
                    return true;
                }

                if (RealTimeAI != null && RealTimeAI.ShouldSwitchBuildingLightsOff(buildingID))
                {
                    __result = false;
                    return false;
                }

                return true;
            }
#pragma warning restore SA1313 // Parameter names must begin with lower-case letter
        }

        private sealed class BuildingAI_CalculateUnspawnPosition : PatchBase
        {
            protected override MethodInfo GetMethod()
            {
                return typeof(BuildingAI).GetMethod(
                    "CalculateUnspawnPosition",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    new[] { typeof(ushort), typeof(Building).MakeByRefType(), typeof(Randomizer).MakeByRefType(), typeof(CitizenInfo), typeof(ushort), typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(Vector2).MakeByRefType(), typeof(CitizenInstance.Flags).MakeByRefType() },
                    new ParameterModifier[0]);
            }

#pragma warning disable SA1313 // Parameter names must begin with lower-case letter
            private static void Postfix(BuildingAI __instance, ushort buildingID, ref Building data, ref Randomizer randomizer, CitizenInfo info, ref Vector3 position, ref Vector3 target, ref CitizenInstance.Flags specialFlags)
            {
                if (WeatherInfo == null || !WeatherInfo.IsBadWeather || data.Info == null || data.Info.m_enterDoors == null)
                {
                    return;
                }

                BuildingInfo.Prop[] enterDoors = data.Info.m_enterDoors;
                bool doorFound = false;
                for (int i = 0; i < enterDoors.Length; ++i)
                {
                    PropInfo prop = enterDoors[i].m_finalProp;
                    if (prop == null)
                    {
                        continue;
                    }

                    if (prop.m_doorType == PropInfo.DoorType.Enter || prop.m_doorType == PropInfo.DoorType.Both)
                    {
                        doorFound = true;
                        break;
                    }
                }

                if (!doorFound)
                {
                    return;
                }

                __instance.CalculateSpawnPosition(buildingID, ref data, ref randomizer, info, out Vector3 spawnPosition, out Vector3 spawnTarget);

                position = spawnPosition;
                target = spawnTarget;
                specialFlags &= ~(CitizenInstance.Flags.HangAround | CitizenInstance.Flags.SittingDown);
            }
#pragma warning restore SA1313 // Parameter names must begin with lower-case letter
        }

        private sealed class PrivateBuildingAI_GetUpgradeInfo : PatchBase
        {
            protected override MethodInfo GetMethod()
            {
                return typeof(PrivateBuildingAI).GetMethod(
                    "GetUpgradeInfo",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    new[] { typeof(ushort), typeof(Building).MakeByRefType() },
                    new ParameterModifier[0]);
            }

#pragma warning disable SA1313 // Parameter names must begin with lower-case letter
            private static bool Prefix(ref BuildingInfo __result, ushort buildingID, ref Building data)
            {
                if (RealTimeAI == null || (data.m_flags & Building.Flags.Upgrading) != 0)
                {
                    return true;
                }

                if (!RealTimeAI.CanBuildOrUpgrade(data.Info.GetService(), buildingID))
                {
                    __result = null;
                    return false;
                }

                return true;
            }
#pragma warning restore SA1313 // Parameter names must begin with lower-case letter
        }

        private sealed class BuildingManager_CreateBuilding : PatchBase
        {
            protected override MethodInfo GetMethod()
            {
                return typeof(BuildingManager).GetMethod(
                    "CreateBuilding",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    new[] { typeof(ushort).MakeByRefType(), typeof(Randomizer).MakeByRefType(), typeof(BuildingInfo), typeof(Vector3), typeof(float), typeof(int), typeof(uint) },
                    new ParameterModifier[0]);
            }

#pragma warning disable SA1313 // Parameter names must begin with lower-case letter
            private static bool Prefix(BuildingInfo info, ref bool __result)
            {
                if (RealTimeAI == null)
                {
                    return true;
                }

                if (!RealTimeAI.CanBuildOrUpgrade(info.GetService()))
                {
                    __result = false;
                    return false;
                }

                return true;
            }

            private static void Postfix(bool __result, ref ushort building, BuildingInfo info)
            {
                if (__result && RealTimeAI != null)
                {
                    RealTimeAI.RegisterConstructingBuilding(building, info.GetService());
                }
            }
#pragma warning restore SA1313 // Parameter names must begin with lower-case letter
        }
    }
}
