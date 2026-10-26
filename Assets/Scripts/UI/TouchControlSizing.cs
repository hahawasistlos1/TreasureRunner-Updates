using System;
using UnityEngine;
namespace TreasureRunner
{
    public enum TouchControlGroup { Movement,Jump,Attack,Weapon }
    public static class TouchControlSizing
    {
        public const float Minimum=.8f,Maximum=1.8f;
        public static event Action Changed;
        public static float Default(TouchControlGroup group)=>group==TouchControlGroup.Weapon ? 1f : 1.2f;
        public static float Normalize(float value,TouchControlGroup group)
        {
            if(float.IsNaN(value) || float.IsInfinity(value) || value<=0)return Default(group);
            return Mathf.Clamp(Mathf.Round(value*10)/10,Minimum,Maximum);
        }
        public static float Get(TouchControlGroup group)
        {
            var data=ProgressStore.Data;
            return Normalize(group==TouchControlGroup.Movement ? data.touchMovementScale : group==TouchControlGroup.Jump ? data.touchJumpScale : group==TouchControlGroup.Attack ? data.touchAttackScale : data.touchWeaponScale,group);
        }
        public static bool Set(TouchControlGroup group,float value)
        {
            if(ProgressStore.IsReadOnly || !Enum.IsDefined(typeof(TouchControlGroup),group))return false;
            value=Normalize(value,group);var data=ProgressStore.Data;
            switch(group){case TouchControlGroup.Movement:data.touchMovementScale=value;break;case TouchControlGroup.Jump:data.touchJumpScale=value;break;case TouchControlGroup.Attack:data.touchAttackScale=value;break;case TouchControlGroup.Weapon:data.touchWeaponScale=value;break;}
            ProgressStore.Save();Changed?.Invoke();return true;
        }
        public static void Reset()
        {
            if(ProgressStore.IsReadOnly)return;
            var data=ProgressStore.Data;data.touchMovementScale=data.touchJumpScale=data.touchAttackScale=1.2f;data.touchWeaponScale=1f;
            ProgressStore.Save();Changed?.Invoke();
        }
        public static TouchControlLayout Layout(Vector2 area,bool arrows)
            =>Calculate(area,arrows,Get(TouchControlGroup.Movement),Get(TouchControlGroup.Jump),Get(TouchControlGroup.Attack),Get(TouchControlGroup.Weapon));
        public static TouchControlLayout Calculate(Vector2 area,bool arrows,float movement,float jump,float attack,float weapon)
        {
            float move=Normalize(movement,TouchControlGroup.Movement),j=94*Normalize(jump,TouchControlGroup.Jump),a=86*Normalize(attack,TouchControlGroup.Attack),w=72*Normalize(weapon,TouchControlGroup.Weapon);
            float arrow=110*move,stick=168*move,gap=18,padding=24;
            float left=arrows ? 2*arrow+gap : stick,right=j+a+w+2*gap;
            float tallest=Mathf.Max(arrows ? arrow : stick,Mathf.Max(j+30,a+10),w);
            float fit=Mathf.Min(1,Mathf.Max(1,area.x-2*padding-32)/(left+right),Mathf.Max(1,area.y*.78f-padding-48)/tallest);
            j*=fit;a*=fit;w*=fit;arrow*=fit;stick*=fit;gap*=fit;
            float actionsTop=Mathf.Max(padding+j+30*fit,padding+a+10*fit,padding+w);
            return new TouchControlLayout{
                arrowDiameter=arrow,joystickDiameter=stick,knobDiameter=68*move*fit,jumpDiameter=j,attackDiameter=a,weaponDiameter=w,
                left=new Vector2(padding+arrow/2,padding+arrow/2+40*fit),right=new Vector2(padding+arrow*1.5f+gap,padding+arrow/2+40*fit),
                joystick=new Vector2(padding+stick/2,padding+stick/2),
                jump=new Vector2(-padding-j/2,padding+j/2+30*fit),attack=new Vector2(-padding-j-gap-a/2,padding+a/2+10*fit),weapon=new Vector2(-padding-j-a-2*gap-w/2,padding+w/2),
                labelPosition=new Vector2(-padding,actionsTop+8),labelWidth=j+a+w+2*gap,fit=fit
            };
        }
    }
    public struct TouchControlLayout
    {
        public float arrowDiameter,joystickDiameter,knobDiameter,jumpDiameter,attackDiameter,weaponDiameter,labelWidth,fit;
        public Vector2 left,right,joystick,jump,attack,weapon,labelPosition;
    }
}
