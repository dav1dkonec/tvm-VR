using UnityEngine;

namespace TvmVr2.Client.Sequence
{
    public sealed class SequenceBusyStateController
    {
        private static void SetPinnedMenuBusy(GameObject handObject, bool busy)
        {
            var controller = handObject != null ? handObject.GetComponent<PinnedHandMenuController>() : null;
            if (controller == null)
                return;

            if (busy)
                controller.SuspendForBusy();
            else
                controller.ResumeAfterBusy();
        }

        public void Enter(GameObject leftHand, GameObject rightHand, GameObject waitCanvas)
        {
            SetPinnedMenuBusy(leftHand, true);
            SetPinnedMenuBusy(rightHand, true);

            if (waitCanvas != null)
                waitCanvas.SetActive(true);

            global::Sequence.editing = true;
        }

        public void Exit(GameObject leftHand, GameObject rightHand, GameObject waitCanvas)
        {
            SetPinnedMenuBusy(leftHand, false);
            SetPinnedMenuBusy(rightHand, false);

            if (waitCanvas != null)
                waitCanvas.SetActive(false);

            global::Sequence.editing = false;
        }
    }
}
