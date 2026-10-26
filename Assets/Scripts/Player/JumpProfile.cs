using UnityEngine;
namespace TreasureRunner
{
    // Shared by the player, campaign authoring and editor reachability checks.
    public static class JumpProfile
    {
        public const float Speed=5.5f, LaunchSpeed=8.4f, Gravity=22f, TerminalSpeed=24f;
        public static float FlightTime(float rise)
        {
            float discriminant=LaunchSpeed*LaunchSpeed-2*Gravity*rise;
            return discriminant<0 ? 0 : (LaunchSpeed+Mathf.Sqrt(discriminant))/Gravity;
        }
        public static bool CanCross(float edgeGap,float rise)=>rise<=1.2f && rise>=-6 && edgeGap<=Speed*FlightTime(rise)-.65f;
    }
}
