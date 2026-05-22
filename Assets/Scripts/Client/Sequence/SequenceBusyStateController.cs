using UnityEngine;

namespace TvmVr2.Client.Sequence
{
    /// <summary>
    /// Controls UI state during sequence editing.
    /// </summary>
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

        /// <summary>
        /// Enters busy editing state.
        /// </summary>
        public void Enter(GameObject leftHand, GameObject rightHand, GameObject waitCanvas)
        {
            SetPinnedMenuBusy(leftHand, true);
            SetPinnedMenuBusy(rightHand, true);

            if (waitCanvas != null)
                waitCanvas.SetActive(true);

            global::Sequence.editing = true;
        }

        /// <summary>
        /// Exits busy editing state.
        /// </summary>
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
