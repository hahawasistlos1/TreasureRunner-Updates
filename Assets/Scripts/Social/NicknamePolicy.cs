using System.Text.RegularExpressions;
namespace TreasureRunner
{
    public static class NicknamePolicy
    {
        public static bool Valid(string value)=>value!=null && value==value.Trim() && value.Length>=3 && value.Length<=20 && Regex.IsMatch(value,@"\A[A-Za-z0-9ÄÖÜäöüß_. -]+\z") && Regex.IsMatch(value,@"[A-Za-z0-9ÄÖÜäöüß]");
        public const string Hint="3–20 Zeichen: Buchstaben, Zahlen, Leerzeichen, Punkt, _ oder -. Keine E-Mail-Adresse.";
    }
}
