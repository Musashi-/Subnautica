using Harmony;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.VR;

namespace SubMotion
{
    public class VRHandsController : MonoBehaviour
    {
        public GameObject rightController;
        public GameObject leftController;
        public ArmsController armsController;
        public Player player;
        public FullBodyBipedIK ik;
        public PDA pda;

        private static VRHandsController _main;
        public static VRHandsController main {
            get {
                if (_main == null) {
                    _main = new VRHandsController();
                }
                return _main;
            }
        }

        public void Initialize(ArmsController controller)
        {
            armsController = controller;
            player = global::Utils.GetLocalPlayerComp();
            ik = controller.GetComponent<FullBodyBipedIK>();
            pda = player.GetPDA();

            rightController = new GameObject("rightController");
            rightController.transform.parent = player.camRoot.transform;

            leftController = new GameObject("leftController");
            leftController.transform.parent = player.camRoot.transform;
        }

        public void UpdateHandPositions() {
            InventoryItem heldItem = Inventory.main.quickSlots.heldItem;

            rightController.transform.localPosition = InputTracking.GetLocalPosition(VRNode.RightHand) + new Vector3(0f, -0.13f, -0.14f);
            rightController.transform.localRotation = InputTracking.GetLocalRotation(VRNode.RightHand) * Quaternion.Euler(35f, 190f, 270f);

            leftController.transform.localPosition = InputTracking.GetLocalPosition(VRNode.LeftHand) + new Vector3(0f, -0.13f, -0.14f);
            leftController.transform.localRotation = InputTracking.GetLocalRotation(VRNode.LeftHand) * Quaternion.Euler(270f, 90f, 0f);

            if (heldItem.item.GetComponent<PropulsionCannon>()) {
                ik.solver.leftHandEffector.target = null;
                ik.solver.rightHandEffector.target = null;
            } else if (heldItem.item.GetComponent<StasisRifle>()) {
                ik.solver.leftHandEffector.target = null;
                ik.solver.rightHandEffector.target = null;
            } else {
                ik.solver.leftHandEffector.target = leftController.transform;
                ik.solver.rightHandEffector.target = rightController.transform;
            }
        }
    }


    [HarmonyPatch(typeof(ArmsController))]
    [HarmonyPatch("Start")]
    class ArmsController_Start_Patch
    {
        [HarmonyPostfix]
        public static void PostFix(ArmsController __instance)
        {
            if (!VRSettings.enabled) {
                return;
            }

            VRHandsController.main.Initialize(__instance);
        }
    }

    [HarmonyPatch(typeof(ArmsController))]
    [HarmonyPatch("Update")]
    class ArmsController_Update_Patch
    {

        [HarmonyPostfix]
        public static void Postfix(ArmsController __instance)
        {
            if (!VRSettings.enabled) {
                return;
            }

            PDA pda = VRHandsController.main.pda;
            Player player = VRHandsController.main.player;
            if ((Player.main.motorMode != Player.MotorMode.Vehicle && !player.cinematicModeActive) || pda.isActiveAndEnabled)
            {
                VRHandsController.main.UpdateHandPositions();
            }
        }
    }

    [HarmonyPatch(typeof(ArmsController))]
    [HarmonyPatch("Reconfigure")]
    class ArmsController_Reconfigure_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(ArmsController __instance, PlayerTool tool)
        {
            FullBodyBipedIK ik = VRHandsController.main.ik;

            ik.solver.GetBendConstraint(FullBodyBipedChain.LeftArm).bendGoal = __instance.leftHandElbow;
            ik.solver.GetBendConstraint(FullBodyBipedChain.LeftArm).weight = 1f;
            if (tool == null)
            {
                Traverse tInstance = Traverse.Create(__instance);
                tInstance.Field("leftAim").Field("shouldAim").SetValue(false);
                tInstance.Field("rightAim").Field("shouldAim").SetValue(false);

                ik.solver.leftHandEffector.target = null;
                ik.solver.rightHandEffector.target = null;
                if (!VRHandsController.main.pda.isActiveAndEnabled)
                {
                    Transform leftWorldTarget = tInstance.Field<Transform>("leftWorldTarget").Value;
                    if (leftWorldTarget)
                    {
                        ik.solver.leftHandEffector.target = leftWorldTarget;
                        ik.solver.GetBendConstraint(FullBodyBipedChain.LeftArm).bendGoal = null;
                        ik.solver.GetBendConstraint(FullBodyBipedChain.LeftArm).weight = 0f;
                    }

                    Transform rightWorldTarget = tInstance.Field<Transform>("rightWorldTarget").Value;
                    if (rightWorldTarget)
                    {
                        ik.solver.rightHandEffector.target = rightWorldTarget;
                        return;
                    }
                }
            }

        }

    }
}



