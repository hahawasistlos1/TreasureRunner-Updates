using UnityEngine;
namespace TreasureRunner
{
    public class CoinSpin : MonoBehaviour
    {
        private void Update() => transform.Rotate(Vector3.forward,90 * Time.deltaTime,Space.World);
    }
}
