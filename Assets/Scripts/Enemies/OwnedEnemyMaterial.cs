using UnityEngine;
namespace TreasureRunner { public class OwnedEnemyMaterial:MonoBehaviour { public Material material;private void OnDestroy(){if(material!=null)Destroy(material);} } }
