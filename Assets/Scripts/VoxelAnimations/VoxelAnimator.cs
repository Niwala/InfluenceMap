using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelAnimator : MonoBehaviour
{
    //Bool for testing the script
    public bool walking;


    //Components
    [Header("Components")]
    public MeshFilter body;

    [Header("Animations")]
    public VoxelAnimation idle;
    public VoxelAnimation walk;

    //Runtime
    private float animationTime;
    private VoxelAnimation current;             //The animation that is currently played


    private void Update()
    {
        //Changes the animation according to the boolean walking.
        if (walking)
        {
            if (current != walk)
                OnAnimationChange(walk);
        }
        else
        {
            if (current != idle)
                OnAnimationChange(idle);
        }

        //Play the current animation.
        Animate();
    }

    private void OnAnimationChange(VoxelAnimation newAnimation)
    {
        current = newAnimation;
        animationTime = 0.0f;                                                   //Reset the animation time. The new animation starts on its first frame.
    }

    private void Animate()
    {
        animationTime += Time.deltaTime * current.speed;                        //Advances the animation time at the expected speed
        int frame = Mathf.FloorToInt(animationTime) % current.frames.Length;    //Give the corresponding frame of the animation
        body.sharedMesh = current.frames[frame];                                //Apply the corresponding mesh for this frame
    }
}
