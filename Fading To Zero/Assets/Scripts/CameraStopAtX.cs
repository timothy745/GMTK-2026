using UnityEngine;
using Unity.Cinemachine;

public class CameraStopAtX : CinemachineExtension
{
    [SerializeField] private float stopX;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage == CinemachineCore.Stage.Body)
        {
            if (state.RawPosition.x > stopX)
            {
                var pos = state.RawPosition;
                pos.x = stopX;
                state.RawPosition = pos;
            }
        }
    }
}
