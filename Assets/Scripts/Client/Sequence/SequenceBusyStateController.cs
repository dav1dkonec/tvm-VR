using UnityEngine;

namespace TvmVr2.Client.Sequence
{
    public sealed class SequenceBusyStateController
    {
        public void Enter(GameObject leftHand, GameObject rightHand, GameObject waitCanvas)
        {
            if (leftHand != null)
                leftHand.SetActive(false);

            if (rightHand != null)
                rightHand.SetActive(false);

            if (waitCanvas != null)
                waitCanvas.SetActive(true);

            global::Sequence.editing = true;
        }

        public void Exit(GameObject leftHand, GameObject rightHand, GameObject waitCanvas)
        {
            if (leftHand != null)
                leftHand.SetActive(true);

            if (rightHand != null)
                rightHand.SetActive(true);

            if (waitCanvas != null)
                waitCanvas.SetActive(false);

            global::Sequence.editing = false;
        }
    }
}
