using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(order = 51)]
public class VoxelAnimation : ScriptableObject
{
    [Tooltip("Animation speed. 10.0f = 10 frames per second.")]
    public float speed = 10.0f;

    [Tooltip("Animation frames")]
    public Mesh[] frames;
}