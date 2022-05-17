using UnityEngine;

public class FlexAttachTargets : MonoBehaviour
{
    /// <summary>
    /// Targets which objects can attach to.
    /// </summary>
    [Tooltip("Targets which objects can attach to.")]
    [SerializeField]
    private GameObject[] _targets = new GameObject[0];
    
    /// <summary>
    /// Returns a target index to pass into SetAttached on FlexNetworkTransform.
    /// </summary>
    /// <param name="go"></param>
    /// <returns></returns>
    public sbyte ReturnTargetIndex(GameObject go)
    {
        for (int i = 0; i < _targets.Length; i++)
        {
            if (_targets[i] == go)
                return (sbyte)(i);
        }

        //0 is considered unset or not found.
        Debug.LogWarning("TargetIndex not found for " + go.name + ". Be sure the GameObject you are trying to attach to is added to Targets.");
        return -1;
    }

    /// <summary>
    /// Returns the target gameobject for the specified index.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public GameObject ReturnTarget(sbyte index)
    {
        //Invalid indexes.
        if (index < 0)
            return null;
        //Out of bounds.
        if (index >= _targets.Length)
            return null;

        return _targets[index];
    }
}
