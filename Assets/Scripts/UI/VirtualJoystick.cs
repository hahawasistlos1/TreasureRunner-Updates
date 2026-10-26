using UnityEngine;
using UnityEngine.EventSystems;
namespace TreasureRunner
{
    public class VirtualJoystick:MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler
    {
        public PlayerMovement player;public RectTransform knob;public float DeadZone=.14f;int? pointer;
        public float Axis {get;private set;}
        public void OnPointerDown(PointerEventData e){if(pointer.HasValue)return;pointer=e.pointerId;OnDrag(e);}
        public void OnDrag(PointerEventData e)
        {
            if(pointer!=e.pointerId)return;var rect=(RectTransform)transform;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,e.position,e.pressEventCamera,out var point))SetDeflection(point/(rect.rect.width*.34f));
        }
        public void SetDeflection(Vector2 delta)
        {
            delta=Vector2.ClampMagnitude(delta,1);knob.anchoredPosition=delta*((RectTransform)transform).rect.width*.28f;
            float x=Mathf.Abs(delta.x);Axis=x<=DeadZone ? 0 : Mathf.Sign(delta.x)*(x-DeadZone)/(1-DeadZone);player?.SetTouchAxis(Axis);
        }
        public void OnPointerUp(PointerEventData e){if(pointer==e.pointerId)ResetStick();}
        public void ResetStick(){pointer=null;Axis=0;if(knob!=null)knob.anchoredPosition=Vector2.zero;player?.SetTouchAxis(0);}
        void OnDisable()=>ResetStick();
        void OnApplicationFocus(bool focused){if(!focused)ResetStick();}
    }
}
