using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;
using xiaoye97;
using UnityEngine;
using BepInEx.Configuration;

namespace DragBuildPowerTower
{
    [BepInPlugin("com.GniMaerd.DSP.DragBuildPowerTower", "DragBuildPowerTower", "1.0.0")]
    [BepInDependency(LDBToolPlugin.MODGUID)]

    public class DragBuildPowerTower : BaseUnityPlugin
    {
        public static ConfigEntry<bool> canDragSatellite;
        public static ConfigEntry<float> wirelessPowerTowerInterval;
        public static ConfigEntry<float> satellaiteInterval;

        void Awake()
        {
            canDragSatellite = Config.Bind<bool>("config", "canDragSatellite", true, "If can drag build satellite substation. 是否允许拖动建造卫星配电站。");
            wirelessPowerTowerInterval = Config.Bind<float>("config", "wirelessPowerTowerInterval", 43.96f, "The interval when build the wirelessPowerTowerInterval. If you changed this value (not recommended), you must press shift to activate bevel building.  无线输电塔拖动建造时的距离。如果你更改了这个值，你就必须按住Shift才能斜向建造。");
            satellaiteInterval = Config.Bind<float>("config", "satellaiteInterval", 51.99f, "The interval when build the satellite substation. If you changed this value, you must press shift to activate bevel building. 卫星配电站拖动建造时的距离。如果你更改了这个值，你就必须按住Shift才能斜向建造。");
        }

        void Start()
        {
            Harmony.CreateAndPatchAll(typeof(DragBuildPowerTower));
            LDBTool.PostAddDataAction += ProtoModify;
        }

        public static void ProtoModify()
        {
            float itv = wirelessPowerTowerInterval.Value;
            ItemProto powerPole2 = LDB.items.Select(2202);
            powerPole2.prefabDesc.dragBuild = true;
            powerPole2.prefabDesc.dragBuildDist = new Vector2(itv, itv);

            if (canDragSatellite.Value)
            {
                float itv2 = satellaiteInterval.Value;
                ItemProto powerPole3 = LDB.items.Select(2212);
                powerPole3.prefabDesc.dragBuild = true;
                powerPole3.prefabDesc.dragBuildDist = new Vector2(itv2, itv2);
            }

        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameData), "Import")]
        public static void ForceChangeProtoDataWhenLoad()
        { 
            ProtoModify();
        }


        /// <summary>
        /// 使得可以斜着建造
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="interval"></param>
        /// <param name="yaw"></param>
        /// <param name="planetRadius"></param>
        /// <param name="gap"></param>
        /// <param name="snaps"></param>
        /// <param name="__result"></param>
        /// <returns></returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlanetGrid), "SnapDotsNonAlloc")]
        public static bool PlanetGridSnapDotsNonAllocPrePatch(ref PlanetGrid __instance, Vector3 begin, Vector3 end, Vector2 interval, float yaw, float planetRadius, float gap, Vector3[] snaps, ref int __result)
        {
            if ( !( interval.x == 43.96f && interval.y == 43.96f || interval.x == 51.99f && interval.y == 51.99f || VFInput.shift) ) return true;
            if (VFInput.control) return true;
            
            begin = begin.normalized;
            end = end.normalized;
            float intervalAll = interval.x;
            float radTotal = Mathf.Acos(Vector3.Dot(begin, end)) * 1.5f; // 乘1.5是为了鼠标有时候指得和识别的地表位置有偏差
            float distTotal = radTotal * planetRadius;

            if (distTotal < intervalAll)
            {
                snaps[0] = __instance.SnapTo(begin);
                __result = 1;
                return false;
            }
            int bCount = (int)(distTotal / intervalAll);
            bCount = bCount < snaps.Length ? bCount : snaps.Length;
            int finalCount = 0;
            for (finalCount = 0; finalCount < bCount; finalCount++)
            {
                if (VFInput.shift)
                    snaps[finalCount] = Vector3.Slerp(begin, end, finalCount * intervalAll / (distTotal / 1.5f));
                else
                    snaps[finalCount] = __instance.SnapTo(Vector3.Slerp(begin, end, finalCount * intervalAll / (distTotal / 1.5f)));
            }
            __result = finalCount;
            return false;
        }
    }
}
