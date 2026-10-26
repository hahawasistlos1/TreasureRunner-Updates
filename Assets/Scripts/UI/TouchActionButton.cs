using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace TreasureRunner
{
    public class TouchActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public PlayerMovement player;
        public PlayerCombat combat;
        public int direction;
        public bool jump;
        public bool attack;
        private readonly HashSet<int> pointers = new HashSet<int>();
        public void OnPointerDown(PointerEventData data)
        {
            pointers.Add(data.pointerId);
            Apply();
            if (jump && player != null) player.RequestJump();
            if (attack && combat != null) combat.Attack();
        }
        public void OnPointerUp(PointerEventData data) { pointers.Remove(data.pointerId); Apply(); }
        public void OnPointerExit(PointerEventData data) { pointers.Remove(data.pointerId); Apply(); }
        private void Apply()
        {
            if (player != null && direction != 0) player.SetTouchDirection(direction,pointers.Count > 0);
        }
        private void OnDisable()
        {
            ReleasePointers();
        }
        public void ReleasePointers(){pointers.Clear();Apply();}
        private void OnApplicationFocus(bool focused){if(!focused){pointers.Clear();Apply();}}
    }
}
