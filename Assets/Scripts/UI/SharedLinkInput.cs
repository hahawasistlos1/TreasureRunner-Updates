using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public static class SharedLinkInput
    {
        // Sharing apps often copy the whole invitation, including a sentence and line breaks.
        // Extract a single link; each consumer still validates its own exact origin and format.
        public static string Extract(string value)
        {
            if(string.IsNullOrWhiteSpace(value))return "";value=value.Trim();
            if(value.StartsWith("TRL1:",StringComparison.Ordinal))return value;
            if(value.StartsWith("https://",StringComparison.OrdinalIgnoreCase) || value.StartsWith("treasurerunner://",StringComparison.OrdinalIgnoreCase))
                if(value.IndexOfAny(new[]{'\r','\n',' ','<','>'})<0)return value;
            var matches=Regex.Matches(value,@"(?:https://|treasurerunner://)[^\s<>""`]+",RegexOptions.IgnoreCase);
            return matches.Count==1 ? matches[0].Value.TrimEnd('.',',',';',')',']') : value;
        }
        public static bool Paste(InputField input,out string message)
        {
            string value=GUIUtility.systemCopyBuffer;
            if(string.IsNullOrWhiteSpace(value)){message="Die Zwischenablage ist leer. Kopiere zuerst den Link oder halte das Eingabefeld zum Einfügen gedrückt.";return false;}
            value=Extract(value);
            if(input.characterLimit>0 && value.Length>input.characterLimit){message="Der kopierte Inhalt ist zu lang. Kopiere bitte nur den Link oder Level-Code.";return false;}
            input.text=value;input.caretPosition=value.Length;message="Eingefügt. Du kannst den Link jetzt öffnen oder importieren.";return true;
        }
    }
}
