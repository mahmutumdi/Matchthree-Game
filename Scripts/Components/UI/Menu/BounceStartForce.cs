using UnityEngine;

namespace Components.UI.Menu
{
    public class BounceStartForce : MonoBehaviour
    {
        [SerializeField] private Vector3 _startForce;
        [SerializeField] private Rigidbody _rigidbody;
        
        void Start()
        {
            _rigidbody.AddForce(_startForce, ForceMode.Impulse);
        }
    }
}
