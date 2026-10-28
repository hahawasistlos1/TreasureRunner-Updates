#if UNITY_EDITOR || UNITY_STANDALONE_WIN
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    // UI tests follow the same two menu entry points as a player. No files or gameplay state.
    public static class BuilderTestNavigation
    {
        static Button Find(string label)=>UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.GetComponentInChildren<Text>()?.text==label);
        public static Button Button(string label)
        {
            var button=Find(label);if(button!=null)return button;
            if(UnityEngine.Object.FindFirstObjectByType<LevelEditorScreen>()==null)throw new Exception("No active editor for "+label);
            if(BuilderPalette.Categories.Contains(label) || label=="Bauteil wählen")
            {
                GameObject.Find("Bauteile").GetComponent<Button>().onClick.Invoke();
                if(label=="Bauteil wählen")return GameObject.Find("Bauteile").GetComponent<Button>();
            }
            else
            {
                GameObject.Find("Funktionen").GetComponent<Button>().onClick.Invoke();
                string tab=new[]{"Speichern","Testspielen","Bauplatz","Teilen","Importieren","Einreichen","Menü","Beenden","Neues leeres Level"}.Contains(label) ? "LEVEL" : new[]{"Level einrichten","< Abschnitt","Abschnitt >","− Zoom","+ Zoom","Ansicht","↑ Hoch"}.Contains(label) ? "ANSICHT" : "BAUEN";
                if(tab!="BAUEN")Find(tab).onClick.Invoke();
            }
            button=Find(label);if(button==null)throw new Exception("Editor control missing: "+label);return button;
        }
    }
}
#endif
