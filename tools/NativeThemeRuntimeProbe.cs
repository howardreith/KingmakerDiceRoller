using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityModManagerNet;
using Object = UnityEngine.Object;

// Temporary lab-only UMM probe. Never included in the six-file mod package.
// Requests are accepted only from the directory/hash/PID pinned by the owned launch.
public static class NativeThemeRuntimeProbe
{
    private const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static string directory, expectedHash;
    private static float elapsed;
    private static UnityModManager.ModEntry entry;
    public static bool Load(UnityModManager.ModEntry value)
    {
        directory = Environment.GetEnvironmentVariable("KDR_THEME_PROBE_DIRECTORY");
        expectedHash = Environment.GetEnvironmentVariable("KDR_THEME_PROBE_SHA256");
        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(expectedHash) || !Directory.Exists(directory)) return false;
        entry = value;
        value.OnToggle = (mod, enabled) => enabled;
        value.OnUpdate = Update;
        value.Logger.Log("Lab-only native theme probe ready; no campaign/save operations.");
        return true;
    }
    private static void Update(UnityModManager.ModEntry mod, float delta)
    {
        elapsed += delta;
        if (elapsed < 0.5f) return;
        elapsed = 0;
        string requestPath = Path.Combine(directory, "request.txt");
        if (!File.Exists(requestPath)) return;
        string[] request = File.ReadAllText(requestPath).Trim().Split('|');
        if (request.Length != 3 || request[0] != Process.GetCurrentProcess().Id.ToString()) return;
        string id = request[1];
        if (id.Length == 0 || id.Any(c => !char.IsLetterOrDigit(c) && c != '-')) return;
        File.Delete(requestPath);
        var output = new StringBuilder();
        try
        {
            Assembly candidate = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "KingmakerDiceRoller");
            string hash;
            using (SHA256 sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(candidate.Location))).Replace("-", "").ToLowerInvariant();
            if (hash != expectedHash) throw new InvalidOperationException("Loaded DLL hash differs from owned launch candidate.");
            output.AppendLine("utc=" + DateTime.UtcNow.ToString("o"));
            output.AppendLine("process=" + Process.GetCurrentProcess().Id + "; dll_sha256=" + hash + "; mvid=" + candidate.ManifestModule.ModuleVersionId);
            object composition = Member(candidate.GetType("KingmakerDiceRoller.Main", true), "root");
            object host = Member(composition, "nativePanel");
            MonoBehaviour allocator = Member(host, "attachedAllocator") as MonoBehaviour;
            if (allocator == null || !allocator.isActiveAndEnabled) throw new InvalidOperationException("No active owned ability allocator; enter genuine new-character Skills first.");
            Type themeType = candidate.GetType("KingmakerDiceRoller.UI.NativeBookTheme", true);
            object resolved = Call(themeType, "Resolve", allocator); // Actual shipped Unity entry point, not a fixture resolver.
            object resources = Member(resolved, "Resources");
            output.AppendLine("production_resolve=" + Member(resources, "Summary"));
            output.AppendLine("attached_theme=" + Member(Member(Member(host, "theme"), "Resources"), "Summary"));
            foreach (string capability in Capabilities(candidate))
            {
                object resource = Resource(candidate, resources, capability);
                output.AppendLine(capability + "=" + (resource == null ? "MISSING: " + Call(resources, "Failure", Capability(candidate, capability)) : Member(resource, "Identity")));
            }
            DumpSession(output, composition, host);
            DumpControls(output, Member(host, "root") as GameObject);
            if (request[2] == "fixtures") RunFixtures(candidate, resources, output);
            else if (request[2] != "inspect") throw new InvalidOperationException("Unknown probe request.");
            output.AppendLine("probe_completed=true");
        }
        catch (Exception exception) { output.AppendLine("PROBE FAILURE: " + exception); }
        File.WriteAllText(Path.Combine(directory, id + ".txt"), output.ToString());
        entry.Logger.Log("Native theme probe report: " + id);
    }
    private static object Member(object instance, string name)
    {
        if (instance == null) return null;
        Type type = instance as Type ?? instance.GetType();
        object target = instance is Type ? null : instance;
        for (; type != null; type = type.BaseType)
        {
            FieldInfo field = type.GetField(name, All | BindingFlags.DeclaredOnly);
            if (field != null) return field.GetValue(target);
            PropertyInfo property = type.GetProperty(name, All | BindingFlags.DeclaredOnly);
            if (property != null) return property.GetValue(target, null);
        }
        return null;
    }
    private static object Call(object instance, string name, params object[] args)
    {
        Type type = instance as Type ?? instance.GetType();
        return type.GetMethods(All).Single(m => m.Name == name && m.GetParameters().Length == args.Length)
            .Invoke(instance is Type ? null : instance, args);
    }
    private static object Capability(Assembly candidate, string name)
    {
        return Enum.Parse(candidate.GetType("KingmakerDiceRoller.UI.NativeThemeCapability", true), name);
    }
    private static string[] Capabilities(Assembly candidate)
    {
        return Enum.GetNames(candidate.GetType("KingmakerDiceRoller.UI.NativeThemeCapability", true));
    }
    private static object Resource(Assembly candidate, object resources, string capability) { return Call(resources, "Get", Capability(candidate, capability)); }
    private static object[] Components(Assembly candidate, object resources, string capability)
    {
        object resource = Resource(candidate, resources, capability);
        if (resource == null) throw new InvalidOperationException("Fixture requires verified live " + capability + " donors.");
        return (object[])Member(resource, "Components");
    }
    private static void Check(bool condition, string name, StringBuilder output)
    {
        if (!condition) throw new InvalidOperationException("Fixture failed: " + name);
        output.AppendLine("FIXTURE PASS " + name);
    }
    private static void DumpSession(StringBuilder output, object composition, object host)
    {
        object coordinator = Member(composition, "coordinator");
        object snapshot = Member(coordinator, "UiSnapshot");
        foreach (string property in new[] { "Mode", "SavedPosition", "SavedCount", "HistoryPosition", "HistoryCount", "Status", "ValidationError" })
            output.AppendLine("session." + property + "=" + Member(snapshot, property));
        int[] assigned = Member(snapshot, "AssignedValues") as int[];
        output.AppendLine("session.AssignedValues=" + (assigned == null ? "<none>" : string.Join(",", assigned)));
        object session = Member(coordinator, "ActiveSession");
        output.AppendLine("session.AssignmentRevision=" + Member(session, "AssignmentRevision"));
        object state = Member(session, "State");
        foreach (string property in new[] { "SkillPointsRemaining", "IntelligenceSkillPoints" })
            output.AppendLine("native." + property + "=" + Member(state, property));
        object panel = Member(host, "panelState");
        output.AppendLine("panel.expanded=" + Member(panel, "IsExpanded") + "; attachments=" + Member(host, "AttachmentCount"));
        var input = Member(host, "customInput") as TMP_InputField;
        if (input != null) output.AppendLine("input.text=" + input.text + "; caret=" + Member(input, "caretPosition"));
    }
    private static void DumpControls(StringBuilder output, GameObject root)
    {
        if (root == null) return;
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            Image image = button.targetGraphic as Image;
            IList listeners = Member(Member(button.onClick, "m_Calls"), "m_RuntimeCalls") as IList;
            output.AppendLine("BUTTON|" + PathOf(button.transform, root.transform) + "|" + Bounds(button.transform as RectTransform) +
                "|active=" + button.gameObject.activeInHierarchy + "|interactable=" + button.interactable +
                "|sprite=" + (image == null || image.overrideSprite == null ? "<none>" : image.overrideSprite.name) +
                "|runtimeListeners=" + (listeners == null ? -1 : listeners.Count));
        }
        foreach (TextMeshProUGUI text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            output.AppendLine("TEXT|" + PathOf(text.transform, root.transform) + "|" + Bounds(text.rectTransform) + "|" + text.text.Replace("\n", " ") +
                "|font=" + (text.font == null ? "<none>" : text.font.name) + "|material=" + (text.fontSharedMaterial == null ? "<none>" : text.fontSharedMaterial.name));
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
            if (image.name == "Paper" || image.name == "PaperShadow") output.AppendLine("PAPER|" + image.name + "|" + Bounds(image.rectTransform) +
                "|enabled=" + image.enabled + "|sprite=" + (image.sprite == null ? "<none>" : image.sprite.name));
    }
    private static string PathOf(Transform node, Transform root)
    {
        var parts = new List<string>();
        for (int depth = 0; node != null && depth < 24; node = node.parent, depth++)
        {
            parts.Add(node.name);
            if (node == root) break;
        }
        parts.Reverse(); return string.Join("/", parts);
    }
    private static string Bounds(RectTransform rect)
    {
        if (rect == null) return "<none>";
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector2 a = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 b = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:F1},{1:F1},{2:F1},{3:F1}", a.x, Screen.height - b.y, b.x - a.x, b.y - a.y);
    }
    private sealed class Fixture : IDisposable
    {
        internal readonly GameObject Root = new GameObject("KingmakerDiceRoller.ThemeProbe.Fixture", typeof(RectTransform));
        internal readonly Assembly Candidate;
        internal readonly object Live;
        internal readonly Type Theme, Resolver;
        internal readonly object Backend;
        internal Transform Action, Label;
        internal Fixture(Assembly candidate, object live, bool populate)
        {
            Root.SetActive(false);
            Candidate = candidate; Live = live;
            Theme = candidate.GetType("KingmakerDiceRoller.UI.NativeBookTheme", true);
            Resolver = candidate.GetType("KingmakerDiceRoller.UI.NativeThemeResolver", true);
            Backend = Activator.CreateInstance(Theme.GetNestedType("UnitySource", All), true);
            if (populate) Populate();
        }
        internal string Locator(string field) { return (string)Resolver.GetField(field, All).GetRawConstantValue(); }
        internal Transform Path(string path)
        {
            Transform node = Root.transform;
            foreach (string name in path.Split('/'))
            {
                Transform child = null;
                for (int i = 0; i < node.childCount; i++) if (node.GetChild(i).name == name) child = node.GetChild(i);
                if (child == null) child = Add(node, name);
                node = child;
            }
            return node;
        }
        internal static Transform Add(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return go.transform;
        }
        private void ImageAt(string path, Image source)
        {
            Image image = Path(path).gameObject.AddComponent<Image>(); Call(Theme, "CopyImage", source, image);
        }
        private void TextAt(Transform node, TextMeshProUGUI source)
        {
            Call(Theme, "CopyText", source, node.gameObject.AddComponent<TextMeshProUGUI>());
        }
        internal void Populate()
        {
            ImageAt(Locator("PaperPath"), (Image)Components(Candidate, Live, "Paper")[0]);
            Action = Path(Locator("ActionPath"));
            var nativeAction = (Button)Components(Candidate, Live, "Buttons")[0];
            var image = Action.gameObject.AddComponent<Image>();
            var button = Action.gameObject.AddComponent<Button>();
            Call(Theme, "ApplyButton", nativeAction, button, image);
            Label = Add(Action, "Next/Complete text"); TextAt(Label, (TextMeshProUGUI)Components(Candidate, Live, "ButtonText")[0]);
            foreach (string role in new[] { "Heading", "Body", "Selector" })
                TextAt(Path(Locator(role + "Path")), (TextMeshProUGUI)Components(Candidate, Live, role)[0]);
            object[] input = Components(Candidate, Live, "Input");
            ImageAt(Locator("InputPath"), (Image)input[0]); ImageAt(Locator("InputPath") + "/Frame", (Image)input[1]);
            Path(Locator("InputPath") + "/InputField").gameObject.AddComponent<TMP_InputField>();
            object[] scroll = Components(Candidate, Live, "Scrollbar");
            ImageAt(Locator("ScrollPath"), (Image)scroll[0]); ImageAt(Locator("ScrollPath") + "/Sliding Area/Handle", (Image)scroll[1]);
            Scrollbar scrollbar = Path(Locator("ScrollPath")).gameObject.AddComponent<Scrollbar>();
            scrollbar.transition = ((Scrollbar)scroll[2]).transition;
            ImageAt(Locator("RulePath"), (Image)Components(Candidate, Live, "Ornament")[0]);
        }
        internal object Resolve() { return Call(Resolver, "Resolve", Root.transform, Backend); }
        public void Dispose() { Object.DestroyImmediate(Root); } // Only this inactive probe-owned root.
    }
    private static void RunFixtures(Assembly candidate, object live, StringBuilder output)
    {
        Check(Member(live, "Status").ToString() == "FullyThemed", "real production resolver fully themed before controlled fixtures", output);
        using (var f = new Fixture(candidate, live, true))
        {
            Check(Member(f.Resolve(), "Status").ToString() == "FullyThemed", "Unity fixture literal slash child resolves", output);
            f.Label.name = "missing label";
            Transform nested = Fixture.Add(Fixture.Add(f.Action, "Next"), "Complete text");
            Call(f.Theme, "CopyText", Components(candidate, live, "ButtonText")[0], nested.gameObject.AddComponent<TextMeshProUGUI>());
            object result = f.Resolve();
            Check(Resource(candidate, result, "ButtonText") == null && Resource(candidate, result, "Buttons") != null,
                "nested path cannot replace literal label or disable valid button artwork", output);
            f.Label.name = "Next/Complete text";
            Fixture.Add(f.Action, "Next/Complete text").gameObject.AddComponent<TextMeshProUGUI>();
            Check(Call(f.Resolve(), "Failure", Capability(candidate, "ButtonText")).ToString().Contains("ambiguous"), "duplicate literal child fails safely", output);
        }
        using (var f = new Fixture(candidate, live, true))
        {
            Object.DestroyImmediate(f.Label.GetComponent<TextMeshProUGUI>());
            Check(Call(f.Resolve(), "Failure", Capability(candidate, "ButtonText")).ToString().Contains("component missing"), "wrong Unity component is diagnosed", output);
            foreach (string field in new[] { "InputPath", "ScrollPath", "RulePath" }) f.Path(f.Locator(field)).name += ".unavailable";
            object partial = f.Resolve();
            Check(Resource(candidate, partial, "Paper") != null && Resource(candidate, partial, "Buttons") != null &&
                Resource(candidate, partial, "Body") != null && Member(partial, "Status").ToString() == "PartiallyThemed",
                "missing optional Unity donors retain paper/button/body styling", output);
        }
        using (var f = new Fixture(candidate, live, false))
        {
            object recovery = Activator.CreateInstance(candidate.GetType("KingmakerDiceRoller.UI.NativeThemeRecovery", true), true);
            object bindings = Activator.CreateInstance(candidate.GetType("KingmakerDiceRoller.UI.NativeThemeBindings", true), true);
            Call(recovery, "Bind", f.Root, f.Root.transform);
            Transform owned = Fixture.Add(f.Root.transform, "OwnedTestButton");
            var image = owned.gameObject.AddComponent<Image>(); var button = owned.gameObject.AddComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent(); int commands = 0;
            button.onClick.AddListener(() => commands++);
            var input = Fixture.Add(f.Root.transform, "OwnedDraft").gameObject.AddComponent<TMP_InputField>();
            input.text = "4d[6]kh";
            int buttonId = button.GetInstanceID(), inputId = input.GetInstanceID();
            Action<string> diagnostic = message => { throw new InvalidOperationException(message); };
            Call(bindings, "Add", Capability(candidate, "Buttons"), (Action<object[]>)(values => Call(f.Theme, "ApplyButton", values[0], button, image)), (Action)(() => image.sprite = null));
            Check((bool)Call(recovery, "TryBegin", true, false), "initial Unity fallback attempt permitted", output);
            Call(bindings, "Apply", f.Resolve(), diagnostic); Call(recovery, "Complete");
            Check(!(bool)Call(recovery, "TryBegin", true, false), "ordinary update cannot retry Unity donor lookup", output);
            f.Populate();
            Check((bool)Call(recovery, "TryBegin", true, true), "existing FillData boundary permits delayed recovery", output);
            object ready = f.Resolve(); Call(bindings, "Apply", ready, diagnostic); Call(recovery, "Complete");
            Check(Member(ready, "Status").ToString() == "FullyThemed" && image.sprite.name == "button_normal" &&
                button.GetInstanceID() == buttonId && input.GetInstanceID() == inputId && input.text == "4d[6]kh" && commands == 0,
                "delayed styling preserves owned controls, draft, and command count", output);
            Call(bindings, "Apply", f.Resolve(), diagnostic); button.onClick.Invoke();
            Check(commands == 1, "recovery leaves one owned listener", output);
            Object.DestroyImmediate(f.Path(f.Locator("PaperPath")).gameObject);
            Check((bool)Call(ready, "DiscardStale") && Resource(candidate, ready, "Paper") == null && Resource(candidate, ready, "Buttons") != null,
                "destroyed Unity donor invalidates only its capability", output);
            Check((bool)Call(recovery, "TryBegin", true, true), "last bounded retry permitted", output); Call(recovery, "Complete");
            Check(!(bool)Call(recovery, "TryBegin", true, true), "repeated FillData stops at three total attempts", output);
            Call(bindings, "Clear");
            Check((int)Member(bindings, "Count") == 0, "owned bindings released on teardown", output);
        }
    }
}
