using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DisasterReady.Diagnostics
{
    /// <summary>
    /// TEMPORARY, FULLY REVERSIBLE DIAGNOSTIC ONLY.
    /// Not part of the game. Attaches itself to the "EnterButton" GameObject at
    /// runtime only (never saved to the scene). Logs the full pointer-event
    /// pipeline (EventSystem.RaycastAll, IPointer* handlers, runtime object
    /// state, IsPointerOverGameObject, Button.onClick) to the Console so the
    /// exact layer where hover/click delivery stops can be identified.
    ///
    /// Safe to delete: Assets/Scripts/Editor/TEMP_PointerPipelineDiagnostic.cs
    /// (+ .meta). Does not modify UIBuilder, the scene file, or any gameplay
    /// script. Placed under Assets/Scripts/Editor so it is excluded from any
    /// real player build.
    /// </summary>
    public class TEMP_PointerPipelineDiagnostic : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        private const string TargetButtonName = "EnterButton";
        private const float LogInterval = 1.0f;
        private float _nextLogTime;
        private static bool _onClickHooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoAttach()
        {
            var buttonGO = GameObject.Find(TargetButtonName);
            if (buttonGO == null)
            {
                Debug.LogWarning($"[TEMP_PointerPipelineDiagnostic] GameObject '{TargetButtonName}' not found in loaded scene(s) - diagnostic not attached.");
                return;
            }

            if (buttonGO.GetComponent<TEMP_PointerPipelineDiagnostic>() == null)
            {
                buttonGO.AddComponent<TEMP_PointerPipelineDiagnostic>();
                Debug.Log($"[TEMP_PointerPipelineDiagnostic] Attached to '{TargetButtonName}' at runtime (not saved to scene).");
            }

            var btn = buttonGO.GetComponent<Button>();
            if (btn != null && !_onClickHooked)
            {
                _onClickHooked = true;
                btn.onClick.AddListener(() => Debug.Log("[TEMP_PointerPipelineDiagnostic] *** Button.onClick INVOKED ***"));
                Debug.Log("[TEMP_PointerPipelineDiagnostic] Added non-invasive onClick listener (existing listeners untouched).");
            }
        }

        public void OnPointerEnter(PointerEventData eventData) =>
            Debug.Log($"[TEMP_PointerPipelineDiagnostic] *** IPointerEnter *** on {gameObject.name} at screenPos={eventData.position}");

        public void OnPointerExit(PointerEventData eventData) =>
            Debug.Log($"[TEMP_PointerPipelineDiagnostic] *** IPointerExit *** on {gameObject.name}");

        public void OnPointerDown(PointerEventData eventData) =>
            Debug.Log($"[TEMP_PointerPipelineDiagnostic] *** IPointerDown *** on {gameObject.name}");

        public void OnPointerUp(PointerEventData eventData) =>
            Debug.Log($"[TEMP_PointerPipelineDiagnostic] *** IPointerUp *** on {gameObject.name}");

        public void OnPointerClick(PointerEventData eventData) =>
            Debug.Log($"[TEMP_PointerPipelineDiagnostic] *** IPointerClick *** on {gameObject.name}");

        private void Update()
        {
            if (Time.unscaledTime < _nextLogTime) return;
            _nextLogTime = Time.unscaledTime + LogInterval;

            var sb = new StringBuilder();
            sb.AppendLine("========== TEMP_PointerPipelineDiagnostic ==========");

            var es = EventSystem.current;
            if (es == null)
            {
                sb.AppendLine("EventSystem.current: NULL");
                Debug.Log(sb.ToString());
                return;
            }

            Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

            var allEventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            var allCanvasesAll = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var activeCanvases = allCanvasesAll.Where(c => c.isActiveAndEnabled).ToArray();
            var allRaycastersAll = FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None);
            var activeRaycasters = allRaycastersAll.Where(r => r.isActiveAndEnabled).ToArray();

            sb.AppendLine($"Mouse.current.position: {mousePos}");
            sb.AppendLine($"EventSystem.current: {es.name} | enabled={es.enabled} | activeInHierarchy={es.gameObject.activeInHierarchy}");
            sb.AppendLine($"EventSystem count in scene (all, incl. inactive): {allEventSystems.Length}");

            var uiModule = es.currentInputModule as InputSystemUIInputModule;
            sb.AppendLine($"currentInputModule: {(es.currentInputModule != null ? es.currentInputModule.GetType().FullName : "NULL")}");
            if (uiModule != null)
                sb.AppendLine($"  InputSystemUIInputModule.enabled: {uiModule.enabled}");

            sb.AppendLine($"currentSelectedGameObject: {(es.currentSelectedGameObject != null ? GetPath(es.currentSelectedGameObject.transform) : "None")}");
            sb.AppendLine($"IsPointerOverGameObject(): {es.IsPointerOverGameObject()}");
            sb.AppendLine($"IsPointerOverGameObject(-1) [mouse pointerId]: {es.IsPointerOverGameObject(-1)}");

            sb.AppendLine($"Active Canvas count: {activeCanvases.Length} (total incl. inactive/disabled: {allCanvasesAll.Length})");
            foreach (var c in activeCanvases)
                sb.AppendLine($"   Canvas: {GetPath(c.transform)} | renderMode={c.renderMode} | sortingOrder={c.sortingOrder} | enabled={c.enabled}");

            sb.AppendLine($"Active GraphicRaycaster count: {activeRaycasters.Length} (total incl. inactive/disabled: {allRaycastersAll.Length})");
            foreach (var r in activeRaycasters)
                sb.AppendLine($"   GraphicRaycaster on: {GetPath(r.transform)} | enabled={r.enabled}");

            // Diagnostic 1: RaycastAll
            var ped = new PointerEventData(es) { position = mousePos };
            var results = new List<RaycastResult>();
            es.RaycastAll(ped, results);
            sb.AppendLine($"RaycastAll(pointerPosition={mousePos}) hit count: {results.Count}");
            bool hitEnterButton = false;
            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                sb.AppendLine($"   [{i}] name='{r.gameObject.name}' path='{GetPath(r.gameObject.transform)}' module={r.module} depth={r.depth} sortingLayer={r.sortingLayer} sortingOrder={r.sortingOrder} distance={r.distance} index={r.index}");
                if (r.gameObject.name == TargetButtonName) hitEnterButton = true;
            }
            sb.AppendLine($"RaycastAll hit '{TargetButtonName}': {hitEnterButton}");

            // Diagnostic 3: button + graphic state
            var buttonGO = GameObject.Find(TargetButtonName);
            if (buttonGO != null)
            {
                sb.AppendLine($"{TargetButtonName}.activeInHierarchy: {buttonGO.activeInHierarchy}");
                var btn = buttonGO.GetComponent<Button>();
                if (btn != null)
                {
                    sb.AppendLine($"Button.enabled: {btn.enabled}");
                    sb.AppendLine($"Button.interactable: {btn.interactable}");
                }
                else
                {
                    sb.AppendLine("Button component: NOT FOUND on this GameObject");
                }
                var img = buttonGO.GetComponent<Image>();
                if (img != null)
                    sb.AppendLine($"Image.raycastTarget: {img.raycastTarget}");
            }
            else
            {
                sb.AppendLine($"GameObject.Find('{TargetButtonName}') returned NULL (inactive, renamed, or not yet loaded).");
            }

            sb.AppendLine("=====================================================");
            Debug.Log(sb.ToString());
        }

        private static string GetPath(Transform t)
        {
            if (t == null) return "(null)";
            var path = t.name;
            var p = t.parent;
            while (p != null)
            {
                path = p.name + "/" + path;
                p = p.parent;
            }
            return path;
        }
    }
}
