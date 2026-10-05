using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Genera/arregla los clips de cambio de color de la piedra a partir de las
/// tiras .aseprite ya cortadas en frames, y los cablea al AnimatorController
/// "PiedraTransformator" (parámetro Trigger + transición Any State -> color -> Idle).
///
/// Cada clip anima el Image de la capa "CapaAnimacion":
///   - m_Enabled = 1  -> enciende la capa (por defecto está apagada)
///   - m_Sprite       -> recorre todos los frames de la tira
///
/// Menú: Tools > Piedras > Regenerar clips de color
/// Es idempotente: puedes correrlo las veces que quieras.
/// </summary>
public static class GeneradorClipsColor
{
    const string SpritesDir = "Assets/Sprites";
    const string ControllerPath = "Assets/Sprites/PiedraTransformator.controller";
    const string IdleState = "Idle";
    const int FrameRate = 12; // fps: baja este número si va muy rápido

    // Configuración por color. Para RECORTAR frames ajusta desde/hasta/paso:
    //   desde = primer frame a incluir (0 = el primero)
    //   hasta = último frame a incluir (-1 = hasta el final)
    //   paso  = 1 usa todos; 2 = uno de cada dos (más corto/rápido); 3 = uno de cada tres...
    // Ejemplos:
    //   desde:5, hasta:-1  -> quita los 5 primeros frames
    //   desde:0, hasta:20  -> se queda solo con los frames 0..20 (corta el final)
    //   paso:2             -> usa la mitad de los frames
    static readonly ColorAnim[] Colores =
    {
        new ColorAnim("PINTADOAzul-Sheet.aseprite",           "CambioAzul",     desde: 0, hasta: 32, paso: 1),
        new ColorAnim("PINTADORojo-Sheet.aseprite",           "CambioRojo",     desde: 0, hasta: 32, paso: 1),
        new ColorAnim("PINTADOVerde-Sheet.aseprite",          "CambioVerde",    desde: 0, hasta: 32, paso: 1),
        new ColorAnim("PINTADPintadoAmarillo-Sheet.aseprite", "CambioAmarillo", desde: 0, hasta: 32, paso: 1),
    };

    struct ColorAnim
    {
        public string aseprite, clip;
        public int desde, hasta, paso;
        public ColorAnim(string aseprite, string clip, int desde, int hasta, int paso)
        {
            this.aseprite = aseprite; this.clip = clip;
            this.desde = desde; this.hasta = hasta; this.paso = paso;
        }
    }

    [MenuItem("Tools/Piedras/Regenerar clips de color")]
    public static void Regenerar()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError($"[ClipsColor] No se encontró el controller en {ControllerPath}");
            return;
        }

        var sm = controller.layers[0].stateMachine;
        var idle = BuscarEstado(sm, IdleState);
        if (idle == null)
        {
            Debug.LogError($"[ClipsColor] No existe el estado '{IdleState}' en el controller.");
            return;
        }

        int hechos = 0;
        foreach (var cfg in Colores)
        {
            string clipName = cfg.clip;
            string asepritePath = $"{SpritesDir}/{cfg.aseprite}";
            if (AssetImporter.GetAtPath(asepritePath) == null)
            {
                Debug.Log($"[ClipsColor] Salto '{clipName}': no existe {asepritePath}");
                continue;
            }

            // Frames cortados (excluye la tira completa; solo _0, _1, ...)
            var todos = AssetDatabase.LoadAllAssetsAtPath(asepritePath)
                .OfType<Sprite>()
                .Where(s => Regex.IsMatch(s.name, "_\\d+$"))
                .OrderBy(s => NumeroFrame(s.name))
                .ToList();

            if (todos.Count <= 1)
            {
                Debug.LogWarning($"[ClipsColor] '{clipName}': la tira {cfg.aseprite} tiene {todos.Count} frame(s). " +
                                 "Cámbiala a 'Sprite Sheet' y córtala con Slice > Automatic.");
                continue;
            }

            // Aplica recorte desde/hasta/paso
            var frames = Recortar(todos, cfg);
            if (frames.Count == 0)
            {
                Debug.LogWarning($"[ClipsColor] '{clipName}': el recorte dejó 0 frames. Revisa desde/hasta/paso.");
                continue;
            }

            var clip = CrearOActualizarClip(clipName, frames);
            CablearColor(controller, sm, idle, clipName, clip);
            hechos++;
            Debug.Log($"[ClipsColor] '{clipName}' listo ({frames.Count}/{todos.Count} frames) y cableado.");
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ClipsColor] Terminado. {hechos} color(es) procesado(s).");
    }

    // ---------- Clip ----------

    static AnimationClip CrearOActualizarClip(string clipName, List<Sprite> frames)
    {
        string clipPath = $"{SpritesDir}/{clipName}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        bool esNuevo = clip == null;
        if (esNuevo) clip = new AnimationClip();

        clip.frameRate = FrameRate;
        float duracion = frames.Count / (float)FrameRate;

        // 1) m_Enabled = 1 durante todo el clip (enciende la capa overlay)
        var bindingEnabled = EditorCurveBinding.FloatCurve("", typeof(Image), "m_Enabled");
        var curvaEnabled = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(duracion, 1f));
        AnimationUtility.SetEditorCurve(clip, bindingEnabled, curvaEnabled);

        // 2) m_Sprite recorre todos los frames
        var bindingSprite = EditorCurveBinding.PPtrCurve("", typeof(Image), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[frames.Count];
        for (int i = 0; i < frames.Count; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / (float)FrameRate, value = frames[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, bindingSprite, keys);

        // Sin loop: se reproduce una vez y el Animator vuelve a Idle
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (esNuevo) AssetDatabase.CreateAsset(clip, clipPath);
        else EditorUtility.SetDirty(clip);
        return clip;
    }

    // ---------- Controller ----------

    static void CablearColor(AnimatorController controller, AnimatorStateMachine sm,
                             AnimatorState idle, string trigger, AnimationClip clip)
    {
        // Parámetro Trigger
        if (!controller.parameters.Any(p => p.name == trigger))
            controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);

        // Estado del color (reutiliza si ya existe)
        var state = BuscarEstado(sm, trigger) ?? sm.AddState(trigger);
        state.motion = clip;
        state.writeDefaultValues = true;

        // Transición Any State -> color (limpia previas para no duplicar)
        foreach (var t in sm.anyStateTransitions.Where(t => t.destinationState == state).ToArray())
            sm.RemoveAnyStateTransition(t);
        var toColor = sm.AddAnyStateTransition(state);
        toColor.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        toColor.hasExitTime = false;
        toColor.duration = 0f;
        toColor.canTransitionToSelf = false;

        // Transición color -> Idle al terminar (limpia previas)
        foreach (var t in state.transitions.Where(t => t.destinationState == idle).ToArray())
            state.RemoveTransition(t);
        var back = state.AddTransition(idle);
        back.hasExitTime = true;
        back.exitTime = 1f;
        back.duration = 0f;
    }

    // ---------- Helpers ----------

    static List<Sprite> Recortar(List<Sprite> todos, ColorAnim cfg)
    {
        int hasta = cfg.hasta < 0 ? todos.Count - 1 : Mathf.Min(cfg.hasta, todos.Count - 1);
        int desde = Mathf.Clamp(cfg.desde, 0, hasta);
        int paso = Mathf.Max(1, cfg.paso);

        var res = new List<Sprite>();
        for (int i = desde; i <= hasta; i += paso)
            res.Add(todos[i]);
        return res;
    }

    static AnimatorState BuscarEstado(AnimatorStateMachine sm, string name)
        => sm.states.FirstOrDefault(s => s.state.name == name).state;

    static int NumeroFrame(string nombre)
    {
        var m = Regex.Match(nombre, "_(\\d+)$");
        return m.Success ? int.Parse(m.Groups[1].Value) : 0;
    }
}
